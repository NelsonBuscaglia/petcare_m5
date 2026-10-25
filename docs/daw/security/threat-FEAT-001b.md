# Threat model FEAT-001b: Cumplimiento, email de vencimiento y actualización automática del estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| Spec | docs/daw/specs/spec-FEAT-001b.md |
| PRD | docs/daw/prd/prd-FEAT-001b.md |
| Base | docs/daw/security/threat-FEAT-001a.md (M1–M4, RA-1, RA-2 siguen vigentes salvo lo que se amplía acá) |
| Fecha | 2026-10-25 |
| Método | STRIDE por componente |

## Componentes y flujos analizados

| Id | Componente (spec) | Flujo |
|---|---|---|
| C1 | `POST /?handler=Cumplir` + `RegistrosService.RegistrarAsync` (Bloque 2) | Navegador (HTMX/form) → validación pura → SQLite |
| C2 | `POST /?handler=EliminarRegistro` + `RegistrosService.EliminarAsync` (Bloque 3) | Navegador → transacción (ExecuteDelete + ExecuteUpdate) → SQLite |
| C3 | `GET /?handler=FormularioCumplimiento`, `GET /?handler=Estado` y polling HTMX (Bloques 2 y 5) | Navegador cada N s → servicio → HTML |
| C4 | `VencimientosWorker` + `EmailsVencimientoService` (Bloque 4) | Timer → SQLite → `IEnvioEmail` → `EmailsVencimiento` |
| C5 | `SmtpEnvioEmail` (MailKit) (Bloque 4) | Proceso → servidor SMTP externo (red) |
| C6 | `ConsolaEnvioEmail` (Bloque 4) | Proceso → stdout |
| C7 | Configuración `Email:*` y `EstadoDelDia:*` (Bloques 4 y 5) | `appsettings.json` + variables de entorno → validación al arrancar |
| C8 | Migración `CumplimientoYEmails` (Bloque 1) | Esquema SQLite |
| C9 | Cadena de suministro: `MailKit`/`MimeKit` 4.18.2 | NuGet → máquina de desarrollo |

## Fronteras de confianza

| Id | Frontera | Lado no confiable |
|---|---|---|
| TB1 | Navegador ↔ servidor (HTTP) | `instanciaId`, `registroId`, `horaReal`, `HX-Request`, token antiforgery |
| TB2 | Proceso ↔ archivo SQLite | El archivo es editable por usuarios del SO (heredado) |
| TB3 | Proceso ↔ servidor SMTP externo | Red y servidor SMTP: respuestas, demoras, certificados |
| TB4 | Proceso ↔ consola / logs | Lo escrito puede quedar en terminales o archivos compartidos |
| TB5 | Entorno del SO ↔ proceso (variables `Email__*`) | Cualquier proceso o persona con acceso al entorno |
| TB6 | Repositorio de paquetes ↔ máquina de desarrollo | Paquetes descargados |

## Clasificación de datos

| Dato | Clasificación | Dónde | Novedad en este ticket |
|---|---|---|---|
| Email de cuidador | PII | SQLite; destinatarios del email; salida de `ConsolaEnvioEmail` | Ahora se **envía** (SMTP) y se **imprime** (consola) |
| Credenciales SMTP (`Usuario`, `Clave`) | Credenciales | Solo variables de entorno; memoria del proceso | Nuevo |
| Nombre de mascota, tipo y horario | Datos del hogar | SQLite; cuerpo del email | Ahora salen del sistema por email |
| Registros de cumplimiento (hora real) | Datos del hogar | SQLite | Ahora los crea y borra la UI |
| `AceptadoEnUtc`, `ReactivadaEnUtc` | Operacional | SQLite | Nuevo |

**Cifrado de PII y credenciales (F-TM-07):**
- **Email a SMTP (tránsito):** TLS obligatorio con `SecureSocketOptions.StartTls`; MailKit falla si el servidor no ofrece STARTTLS; validación de certificado por defecto (M6).
- **Credenciales SMTP (reposo):** nunca en archivos del repo (`appsettings*.json`, `launchSettings.json`), con test (M3-bis); viven solo en el entorno de la máquina que ejecuta con `Modo=Smtp`, que no puede ser Development.
- **Navegador ↔ servidor y SQLite:** sin cambios respecto de RA-2 (HTTP en `localhost`, archivo sin cifrar).

## STRIDE por componente

### C1 — Registrar cumplimiento
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin autenticación: cualquiera con la URL registra cumplimientos | R1 |
| T | CSRF → antiforgery (`Cumplir_SinToken_400`). `instanciaId` manipulado → 404 o error tipado; `horaReal` validada con regex cerrada y contra la hora actual | R2, R4 |
| R | No se registra quién cumplió (fuera de alcance del PRD) | R14 |
| I | Mensajes de error genéricos de negocio; 500 sin detalles; `horaReal` re-mostrada codificada (`Cumplir_HoraConHtml_SeMuestraCodificado`) | R3 |
| D | Un POST por clic; índice único evita registros duplicados | — |
| E | Sin roles; todo cuidador puede cumplir cualquier instancia (diseño del PRD) | — |

### C2 — Eliminar registro
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin autenticación | R1 |
| T | CSRF → antiforgery. `registroId` arbitrario → solo registros del día; borrado atómico sin carrera | R2, R4 |
| R | No se registra quién eliminó | R14 |
| I | "Ya fue eliminado" no distingue un id inexistente: no revela ids | — |
| D | Eliminar y volver a registrar en bucle podría abrir nuevas ventanas de email: solo si el email anterior **no** fue aceptado (AC-23 impide reenvíos) | R8 |
| E | — | — |

### C3 — Formulario y polling
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Sin autenticación | R1 |
| T | GET sin efectos (no cambian estado) | — |
| R | — | — |
| I | El parcial muestra lo mismo que `/` | — |
| D | 1 request cada 10 s por pestaña; para un hogar es despreciable | R9 |
| E | El filtro de `hx-trigger` se evalúa como JS (`allowEval`): no contiene datos de usuario | R12 |

### C4 — Worker y servicio de vencimientos
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Destinatarios salen de la base (cuidadores del hogar), nunca de una request | — |
| T | Una sola fila `EmailVencimiento` por instancia (índice único) | — |
| R | EventIds 4000–4003 con `InstanciaId` y error tipado | — |
| I | Logs sin direcciones ni `ex.Message` (M3 reformulada, test que busca `@`) | R6 |
| D | Altas masivas de deberes futuros generarían muchos emails; acotado a `localhost` (M1) y un email por instancia | R8 |
| E | Corre con los permisos del proceso | — |

### C5 — Envío SMTP
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S | Servidor SMTP falso por DNS/MITM → TLS con validación de certificado (M6) | R10 |
| T | Inyección de cabeceras vía nombre de mascota (CR/LF en el asunto) → M5 | R5 |
| R | Aceptación registrada en `EmailsVencimiento` | — |
| I | Cada cuidador vería las direcciones de los demás → destinatarios en **Bcc** (M7); credenciales nunca logueadas | R11, R6 |
| D | Servidor que no responde → timeout de 30 s y ventana de 5 min | — |
| E | — | — |

### C6 — Envío a consola
| STRIDE | Análisis | Riesgo |
|---|---|---|
| I | Imprime destinatarios completos en stdout (decisión del usuario; solo `Modo=Consola`, forzado en Development) | RA-2 ampliado |
| (resto) | No aplica | — |

### C7 — Configuración
| STRIDE | Análisis | Riesgo |
|---|---|---|
| S/T | Variables de entorno manipuladas por quien controla la máquina: fuera del modelo | — |
| I | Credenciales en archivos versionados → prohibido y testeado (`Arranque_AppSettingsSinCredencialesSmtp`) | R6 |
| E | `Modo=Smtp` en Development mandaría emails reales → la app no arranca (`Arranque_DevelopmentConSmtp_Falla`) | R13 |

### C8 — Migración
| STRIDE | Análisis | Riesgo |
|---|---|---|
| T | Columna nueva con valor constante `false`; sin defaults calculados | — |
| (resto) | No aplica | — |

### C9 — Cadena de suministro (W-TM-01)
| STRIDE | Análisis | Riesgo |
|---|---|---|
| T/E | Paquete comprometido → versión exacta 4.18.2, `dotnet list package --vulnerable` en CODE (SAST) | R15 |

## Riesgos

| Id | Riesgo | STRIDE | Probabilidad | Impacto | Tratamiento |
|---|---|---|---|---|---|
| R1 | Sin autenticación: cualquiera con la URL registra y elimina cumplimientos | S, E | Baja en `localhost` | Alto | M1 + RA-1 |
| R2 | CSRF en Cumplir / EliminarRegistro | T | Baja | Medio | Antiforgery + tests |
| R3 | XSS al re-mostrar `horaReal` o nombres | T, I | Baja | Alto | Encoding de Razor, sin `Html.Raw`, test |
| R4 | IDs manipulados (`instanciaId`, `registroId`) | T | Media | Bajo | Validación en servicio (404 / "ya eliminado" / "solo del día"); todos los datos son del mismo hogar |
| R5 | Inyección de cabeceras en el email | T | Baja | Alto | M5 + codificación de MimeKit |
| R6 | Fuga de PII o credenciales por logs o archivos | I | Media | Alto | M3 reformulada + test de appsettings + `Clasificar` sin `ex.Message` |
| R7 | Email duplicado si SMTP acepta y falla el guardado | — | Baja | Bajo | RA-3 |
| R8 | Flood de emails por altas masivas | D | Baja | Medio | M1 + un email por instancia + `VencidaAlGenerarse` |
| R9 | Carga por polling | D | Baja | Bajo | Intervalo ≥ 1 s validado; un hogar |
| R10 | MITM / downgrade con el servidor SMTP | S, I | Baja | Alto | M6 |
| R11 | Destinatarios ven las direcciones de los demás | I | Alta (por diseño) | Bajo | M7 |
| R12 | `allowEval` del filtro de HTMX | E | Baja | Medio | Atributo sin datos de usuario; revisar si se agrega CSP |
| R13 | Emails reales en desarrollo | E | Media | Medio | Guarda de arranque + test |
| R14 | No se sabe quién registró o eliminó | R | Alta (por diseño) | Bajo | Fuera de alcance (PRD); ticket de sesión |
| R15 | Dependencia comprometida | T, E | Baja | Alto | Versión exacta + auditoría de vulnerables |

Resumen: C:0 · H (impacto alto): R1, R3, R5, R6, R10, R15 — todos con mitigación; R1 además con RA-1.

## Mitigaciones a incorporar en la spec

- **M3 (reformulada):** ningún `ILogger` de `src/` recibe direcciones de email, credenciales ni `Exception.Message` de MailKit; los destinatarios solo aparecen en la salida de `ConsolaEnvioEmail`. Test que verifica que los logs del worker no contienen `@`.
- **M3-bis:** `Email:Smtp:*` nunca en `appsettings*.json` ni `launchSettings.json` (test).
- **M5 — Asunto sin saltos de línea:** `MensajeVencimiento.Armar` reemplaza `\r` y `\n` por espacio en el asunto (y el nombre de la mascota en el cuerpo se escribe como texto plano). Test con un nombre que contiene `\r\nBcc: x@y`.
- **M6 — TLS estricto:** `SmtpEnvioEmail` usa `SecureSocketOptions.StartTls` (o `SslOnConnect` si el puerto es 465) y **nunca** reemplaza `ServerCertificateValidationCallback`; el constructor interno con `SecureSocketOptions.None` existe solo para tests (`InternalsVisibleTo`). Verificable con `grep ServerCertificateValidationCallback src/` sin resultados.
- **M7 — Bcc:** `SmtpEnvioEmail` pone a los destinatarios en **Bcc** y al remitente en **To**, para que ningún cuidador vea las direcciones de los demás.

## Riesgos aceptados

### RA-1 — Sin control de acceso (heredado de FEAT-001a, ampliado)
| Campo | Valor |
|---|---|
| Quién lo aceptó | El usuario, en DEFINE de FEAT-001, 2026-10-25 |
| Justificación | Registro y sesión quedan para su propio ticket; ahora también cubre registrar y eliminar cumplimientos sin autenticación |
| Condiciones de revisión | Solo desarrollo local (M1); obligatoriamente en el ticket de registro y sesión, o antes de exponer la app fuera de `localhost` |

### RA-2 — PII sin cifrar en desarrollo (heredado, ampliado a la consola)
| Campo | Valor |
|---|---|
| Quién lo aceptó | El usuario: en PLAN de FEAT-001a (HTTP y SQLite) y en PLAN de FEAT-001b, 2026-10-25 (salida de consola con destinatarios completos) |
| Justificación | Solo `localhost`; direcciones de `ejemplo.com`; la consola es el canal de "email" en desarrollo que pide AGENTS.md |
| Condiciones de revisión | Antes de cargar emails reales o exponer la app; como más tarde en el ticket de registro y sesión |

### RA-3 — Email de vencimiento duplicado
| Campo | Valor |
|---|---|
| Quién lo aceptó | El usuario, en PLAN de FEAT-001b, 2026-10-25 |
| Justificación | Si el servidor SMTP acepta el email y falla el guardado de `EmailVencimiento`, la pasada siguiente lo reenvía; la alternativa (guardar antes de enviar) puede perder emails, que es peor para el objetivo del PRD |
| Condiciones de revisión | Si se observan duplicados en uso real, o al pasar a un servicio de email con idempotencia (Message-ID / API transaccional) |

## Resultado

```
/daw-threat-modeling — PASSED
Superficies de ataque: 9 · Fronteras de confianza: 6
Riesgos: C:0 H:6 M:4 L:5 — todos los de impacto alto con mitigación
Mitigaciones a incorporar en la spec: M3 (reformulada), M3-bis, M5, M6, M7
Riesgos aceptados: RA-1, RA-2 (ampliados), RA-3 (nuevo) — los tres aprobados por el usuario
Report: docs/daw/security/threat-FEAT-001b.md
```
