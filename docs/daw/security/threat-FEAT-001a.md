# Threat model FEAT-001a: Deberes de comida y paseo: alta, instancias y estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Spec | docs/daw/specs/spec-FEAT-001a.md |
| PRD | docs/daw/prd/prd-FEAT-001a.md |
| Fecha | 2026-10-25 |
| Método | STRIDE por componente |

## Componentes y flujos analizados

| Id | Componente (spec) | Flujo |
|---|---|---|
| C1 | Página `GET /` — estado del día (Bloque 4, `index.cshtml` + `EstadoDelDiaService`) | Navegador → Kestrel → servicio → SQLite → HTML |
| C2 | Página `GET/POST /mascotas/{mascotaId:int}/deberes` — configuración y alta de deberes (Bloque 2, `configuracion.cshtml`, `_configuracion-deberes.cshtml`, `DeberesService`, `ValidacionAltaDeber`) | Navegador (form / HTMX) → Kestrel → validación pura → servicio → SQLite |
| C3 | Página `/error` y manejador global de excepciones (Bloque 1) | Excepción → `UseExceptionHandler` → HTML genérico |
| C4 | `GeneracionDiariaWorker` + `InstanciasService` (Bloque 3) | Timer (TimeProvider) → servicio → SQLite; sin entrada externa |
| C5 | Base SQLite `petcare.db` y migración `Inicial` con datos precargados (Bloque 1) | Archivo local en disco |
| C6 | Archivos estáticos: `site.css`, `htmx.min.js` vendorizado (Bloque 4) | Kestrel → navegador |
| C7 | Cadena de suministro: paquetes NuGet, `dotnet-ef`, HTMX, Chromium de Playwright (solo tests) | Registro de paquetes / CDN → máquina de desarrollo |

## Fronteras de confianza

| Id | Frontera | Lado confiable / no confiable |
|---|---|---|
| TB1 | Navegador ↔ servidor ASP.NET Core (HTTP) | Todo lo que llega en ruta, formulario y headers (`mascotaId`, `tipo`, `hora`, `repeticion`, `HX-Request`, token antiforgery) es no confiable |
| TB2 | Proceso de la app ↔ archivo SQLite en disco | El archivo es modificable por cualquier usuario del sistema operativo con acceso a la carpeta |
| TB3 | Repositorios externos ↔ máquina de desarrollo (NuGet, unpkg/GitHub para HTMX, CDN de Playwright) | Los artefactos descargados son no confiables hasta fijar versión |
| TB4 | Servidor ↔ logs (consola) | Lo que se escribe en el log puede terminar en archivos o terminales compartidos |

## Clasificación de datos

| Dato | Clasificación | Dónde | Expuesto en UI en este ticket |
|---|---|---|---|
| Email de cuidador (`Cuidador.Email`) | PII | SQLite (seed: `ana@ejemplo.com`, `beto@ejemplo.com`, dominio de ejemplo) | No |
| Nombre de mascota | Datos del hogar (no sensibles) | SQLite | Sí |
| Deberes, instancias, registros de cumplimiento (tipo, horas, fechas) | Datos del hogar (no sensibles) | SQLite | Sí |
| Credenciales / tokens | No hay en este ticket (sin sesión) | — | — |
| Token antiforgery | Credencial de request (efímera) | Cookie + campo oculto | Solo en el HTML del formulario |

**Cifrado de la PII (F-TM-07):**
- **En tránsito:** HTTP sin TLS, solo sobre `localhost` (mitigación M1). Ver riesgo aceptado RA-2.
- **En reposo:** archivo SQLite sin cifrar en la máquina de desarrollo. Ver riesgo aceptado RA-2.

## STRIDE por componente

### C1 — `GET /` estado del día
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin autenticación: cualquiera que alcance la URL ve el estado del día | R1 |
| T | Solo lectura; no modifica datos | — |
| R | Lectura sin registro de quién consulta; fuera de alcance (PRD) | R6 |
| I | Muestra nombres de mascotas y horarios; no muestra emails. Nombre con HTML → encoding de Razor (test `Index_NombreConHtml_SeMuestraCodificado`) | R3 |
| D | Una consulta por request sobre un hogar con 2 mascotas; sin paginación necesaria | R8 |
| E | No hay roles que escalar | — |

### C2 — `GET/POST /mascotas/{mascotaId}/deberes`
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin autenticación: cualquiera que alcance la URL da de alta deberes | R1 |
| T | CSRF: un sitio externo podría forzar un POST → antiforgery por defecto de Razor Pages, test `PostAlta_SinTokenAntiforgery_400` | R2 |
| T | Inyección SQL vía `tipo`/`hora`/`repeticion` → se validan contra listas cerradas y regex antes de persistir; EF Core parametriza; sin SQL crudo (M2) | R4 |
| T | Overposting: solo se bindean 3 strings y `mascotaId` de la ruta; ninguna entidad se bindea desde el formulario | R4 |
| R | No se registra qué cuidador dio de alta un deber; fuera de alcance (PRD) | R6 |
| I | Valores rechazados se re-muestran en el formulario → encoding de Razor (test `PostAlta_TipoConHtml_SeMuestraCodificado`); `mascotaId` inexistente → 404 sin datos | R3 |
| D | Altas masivas por POST hacen crecer la tabla `Deber` y el trabajo del worker; longitudes de campo acotadas | R8 |
| E | No hay roles; `mascotaId` de otra mascota es válido por diseño (todos los cuidadores ven todo, PRD-001) | — |

### C3 — `/error` y manejador global
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | No aplica (página estática) | — |
| T | No recibe entrada | — |
| R | Las excepciones quedan en el log | — |
| I | Fuera de Development: mensaje genérico, sin stack trace (tests `Error_*_Devuelve500Generico`). En Development se muestra la página de excepción del desarrollador, solo en localhost | R5 |
| D | No aplica | — |
| E | No aplica | — |

### C4 — Worker de generación diaria
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin entrada externa; lo dispara el timer | — |
| T | Escribe instancias; el índice único `(DeberId, Fecha)` impide duplicados | — |
| R | Cada pasada fallida se registra con `EventId` 3002 y la excepción; `InstanciaYaExistia` con 3001 | — |
| I | Los logs del worker no incluyen emails (M3) | R7 |
| D | Una pasada por minuto sobre pocos deberes; una excepción no detiene el host (test `Worker_PasadaQueLanza_RegistraErrorYSigue`) | R8 |
| E | Corre con los permisos del proceso de la app; no ejecuta nada externo | — |

### C5 — Base SQLite y migración
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | No aplica | — |
| T | Cualquier usuario del SO con acceso a la carpeta puede editar `petcare.db` | RA-2 |
| R | No aplica | — |
| I | El archivo contiene emails de cuidadores sin cifrar; `.gitignore` excluye `*.db` para que no se commitee (Bloque 1) | R7, RA-2 |
| D | Borrado o corrupción del archivo → la app devuelve 500 genérico; se recrea con `dotnet ef database update` | — |
| E | No aplica | — |

### C6 — Archivos estáticos
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | No aplica | — |
| T | `htmx.min.js` se sirve desde el propio servidor (sin CDN en runtime) | R9 |
| R | No aplica | — |
| I | Solo CSS/JS públicos | — |
| D | No aplica | — |
| E | No aplica | — |

### C7 — Cadena de suministro (W-TM-01)
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Paquete con nombre parecido (typosquatting) | R9 |
| T | Artefacto alterado en origen | R9 |
| R | No aplica | — |
| I | No aplica | — |
| D | No aplica | — |
| E | Un paquete comprometido ejecuta código con los permisos del desarrollador | R9 |

## Riesgos

| Id | Riesgo | STRIDE | Probabilidad | Impacto | Tratamiento |
|---|---|---|---|---|---|
| R1 | Sin autenticación: cualquiera que alcance la URL ve y modifica datos (C1, C2) | S, E | Media si se expone en red; baja en localhost | Alto | M1 + riesgo aceptado RA-1 |
| R2 | CSRF en el alta de deberes | T | Baja | Medio | Antiforgery por defecto (spec Bloque 2) + test |
| R3 | XSS al mostrar valores rechazados o nombres de mascota | T, I | Baja | Alto | Encoding de Razor, prohibido `Html.Raw` con datos de usuario (spec) + 2 tests |
| R4 | Inyección SQL / overposting | T | Baja | Alto | Validación cerrada + EF parametrizado + M2 (sin SQL crudo) |
| R5 | Fuga de detalles internos en errores | I | Baja | Medio | `UseExceptionHandler` fuera de Development + tests; página de desarrollador solo en Development |
| R6 | No se puede saber quién hizo cada acción | R | Alta (por diseño) | Bajo | Fuera de alcance por PRD; se aborda con el ticket de sesión |
| R7 | PII (emails) en logs | I | Baja | Medio | M3 |
| R8 | Crecimiento de datos por POST masivos / carga del worker | D | Baja | Bajo | Acotado a localhost (M1); un hogar; aceptado como bajo (W-TM-02) |
| R9 | Dependencia comprometida o alterada | T, E | Baja | Alto | M4 |

Resumen: C:0 · H (impacto alto): R1, R3, R4, R9 — todos con mitigación; R1 además con riesgo aceptado.

## Mitigaciones a incorporar en la spec

- **M1 — Solo localhost:** `src/Properties/launchSettings.json` usa `applicationUrl: "http://localhost:5080"`; ninguna configuración del repo (`appsettings*.json`, `Kestrel:Endpoints`) enlaza `0.0.0.0`, `*` ni `+`. Test `Arranque_LaunchSettings_SoloLocalhost` que lee el archivo y verifica que todas las URLs son `localhost`.
- **M2 — Sin SQL crudo:** prohibido `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated` y `SqlQueryRaw` en `src/` (verificable con `grep` en la verificación final).
- **M3 — Sin PII en logs:** ningún `Log*` de `src/` incluye `Cuidador.Email`; los eventos del worker registran solo `DeberId`, fechas y cantidades.
- **M4 — Dependencias fijadas:** versiones exactas en los `.csproj` y en `.config/dotnet-tools.json` (sin rangos flotantes); HTMX descargado de la release oficial de GitHub (`bigskysoftware/htmx`) en una versión 2.0.x exacta, con la versión y el SHA-256 del archivo anotados en `src/wwwroot/lib/htmx/VERSION.txt`.

## Riesgos aceptados

### RA-1 — Sin control de acceso
| Campo | Valor |
|---|---|
| Quién lo aceptó | El usuario (dueño del proyecto), en DEFINE de FEAT-001, 2026-10-25 |
| Justificación | Registro y sesión quedan para un ticket propio; esta entrega arranca con datos precargados |
| Condiciones de revisión | La app se usa solo en desarrollo local (M1). Se revisa obligatoriamente en el ticket de registro y sesión, o antes si la app se va a exponer fuera de `localhost` |

### RA-2 — PII sin cifrar en tránsito (HTTP) y en reposo (SQLite)
| Campo | Valor |
|---|---|
| Quién lo aceptó | El usuario (dueño del proyecto), en PLAN de FEAT-001a, 2026-10-25 |
| Justificación | Solo desarrollo local sobre `localhost` (M1); los únicos emails son de dominio de ejemplo (`ejemplo.com`); este ticket no muestra emails en la UI |
| Condiciones de revisión | Antes de cargar emails reales o de exponer la app fuera de `localhost`, y como más tarde en el ticket de registro y sesión (HTTPS + evaluar cifrado del archivo) |

## Resultado

```
/daw-threat-modeling — PASSED
Superficies de ataque: 7 · Fronteras de confianza: 4
Riesgos: C:0 H:4 M:3 L:2 — todos los de impacto alto con mitigación
Mitigaciones a incorporar en la spec: M1, M2, M3, M4
Report: docs/daw/security/threat-FEAT-001a.md
```
