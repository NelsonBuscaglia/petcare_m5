# Verificación FEAT-001a: Deberes de comida y paseo: alta, instancias y estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Tier | FEATURE |
| PRD | docs/daw/prd/prd-FEAT-001a.md (17 FR, 3 NFR, 24 AC) |
| Spec | docs/daw/specs/spec-FEAT-001a.md (4 bloques, 81 tests nombrados) |
| Threat model / SAST | docs/daw/security/threat-FEAT-001a.md · docs/daw/security/sast-FEAT-001a.md |
| Código verificado | commits 3a1bf4d, 79d66a9, e7eb9b6, bb979f8 (+ 837c0dc, reporte SAST) |
| Fecha | 2026-10-25 |
| Verificador | `daw-module-verifier` (agente que no escribió el código) |
| Resultado final | **PASSED** — 39 PASS, 0 FAIL, 7 WARN |

## Rondas

| Ronda | Resultado | Motivo |
|---|---|---|
| 1 | BLOCKED (38 PASS, 1 FAIL, 7 WARN) | La evidencia TDD (tests en rojo antes de implementar) no estaba persistida en el repo; no había huecos funcionales. |
| 2 | **PASSED** (39 PASS, 0 FAIL, 7 WARN) | Se aportó la evidencia TDD de los informes del implementador por bloque (resumida abajo); el verificador la contrastó con los tests en disco y coincide. |

No hubo corrective loop a CODE: la ronda 1 no encontró fallas de código, solo evidencia faltante.

## Ejecuciones

- `dotnet build petcare.slnx --no-incremental`: 0 warnings, 0 errores (`TreatWarningsAsErrors` en ambos proyectos).
- `dotnet test petcare.slnx`: **96/96** (86 métodos de test; las theories cuentan 10 casos extra; incluye 3 tests de Playwright en Chromium real).
- `dotnet format style --diagnostics IDE0005 --verify-no-changes`: sin usings sin usar.
- `dotnet test --collect "Code Coverage;Format=cobertura"` (medido dos veces, por el orquestador y por el verificador; coinciden).

## Reglas §5

| Regla | Resultado | Detalle |
|---|---|---|
| F-VER-01 AC con test que pasa | ✅ PASS | AC-01…AC-24 trazados a código y tests (tabla abajo). AC-21 y AC-22 con WARN. |
| F-VER-02 Tareas de la spec | ✅ PASS | Bloques 1–4 completos; mitigaciones M1–M4; greps de la verificación final sin resultados; ningún PageModel usa `PetCareDbContext`. |
| F-VER-03 Cobertura ≥ 80% | ✅ PASS | Líneas 91,7% (1124/1226) · ramas 92,2% (236/256) · funciones 97% sobre funciones escritas a mano (74/76); 71,5% en bruto (ver WARN 3). |
| F-VER-04 Sad paths | ✅ PASS | POST de deberes: sin hora, 25:00, medicación, sin repetición, `<script>`, mascota 999, sin token / token inválido, falla de base; GET deberes 404; 17 casos inválidos de `Validar`; `GET /` con falla de base y nombre con HTML; bordes de `EsVencida`/`ZonaHogar`. |
| F-VER-05 Lint / tipos | ✅ PASS | Build sin warnings con `TreatWarningsAsErrors` (no hay linter separado configurado). |
| F-VER-06 Tests de la spec | ✅ PASS | Los 81 nombres de la spec existen y pasan. 5 tests extra (Resultado ×3, GeneracionLock ×2). |
| W-VER-01 Código muerto | ⚠️ WARN | Ver WARN 4. |
| W-VER-02 Lógica de negocio < 90% | ⚠️ WARN | Ver WARN 5. |
| W-VER-03 Tests frágiles | ⚠️ WARN | Ver WARN 6. |

### Trazabilidad AC → código → tests

| AC | Código | Tests | Estado |
|---|---|---|---|
| AC-01 | `EstadoDelDiaService.ObtenerAsync`, `index.cshtml` | `Index_PrimeraVez_MuestraDosSeccionesVacias`, `Seed_CreaHogarCuidadoresYMascotas` | ✅ |
| AC-02 | `DeberesService.DarDeAltaAsync` / `ListarActivosAsync` | `DarDeAlta_PaseoTodosLosDias_ApareceEnActivos`, `PostAlta_Htmx_DevuelveParcialConDeber` | ✅ |
| AC-03 | `deberes-service.cs:68`, `estado-del-dia-service.cs:28` | `DarDeAlta_ParaFirulais_NoApareceEnMichi`, `Index_DeberDeFirulais_NoApareceEnSeccionDeMichi` | ✅ |
| AC-04…AC-07 | `ValidacionAltaDeber.Validar`, `ConfiguracionModel.OnPostAsync` | `Validar_*`, `PostAlta_*_MuestraMensajeYNoCrea` | ✅ |
| AC-08, AC-19 | `ListarActivosAsync` | `ListarActivos_*` | ✅ |
| AC-09 | `deberes-service.cs:48` | `DarDeAlta_HoraYaPasada_GeneraInstanciaDeHoy`, `Index_AltaConHoraPasada_MuestraPendiente` | ✅ |
| AC-10, AC-11 | `GeneracionInstancias.QueFaltan` | `QueFaltan_*`, `Index_Manana0800_*` | ✅ |
| AC-12, AC-13 | `ZonaHogar.Hoy`, `GenerarDelDiaAsync`, worker | `Hoy_*`, `GenerarDelDia_0005/2359`, `Worker_CruzaMedianoche_*`, `Index_0005/2359_*` | ✅ |
| AC-14 | `GeneracionDiariaWorker` | `Worker_ArrancaA1000TrasCaida_GeneraInstanciaYQuedaPendiente` | ✅ |
| AC-15 | `GenerarDelDiaAsync` | `GenerarDelDia_DosVeces_UnaSolaInstancia`, `Worker_Reinicio_NoDuplica` | ✅ |
| AC-16, AC-17, AC-24 | `EstadoInstancia.EsVencida` | `EsVencida_*`, `Index_0910/0915/0515Local_*` | ✅ |
| AC-18 | `EstadoInstancia.EsVigente` | `EsVigente_*`, `Index_0700_NoMuestraAyerYMuestraHoySinMarca` | ✅ |
| AC-20 | `ArmadoEstadoDelDia.Armar` | `Armar_ComidaYPaseoA1200_*`, `Index_1200_*` | ✅ |
| AC-21, AC-22 | `ArmadoEstadoDelDia.Armar` | `Armar_DosPaseosYComidaHoy_ResumenCorrecto`, `Armar_RegistrosDeAyer_*` | ⚠️ (WARN 1) |
| AC-23 | `site.css`, `_Layout.cshtml` | `SinScrollHorizontal_360px_*` (Chromium real) | ✅ |

## Evidencia TDD (resumen de los informes del implementador por bloque)

| Bloque | Tests de la spec | Rojo antes de implementar | Verde |
|---|---|---|---|
| 1 | 8 | 8/8 con esqueletos que compilaban: `NotImplementedException` en `ZonaHogar` ×3; "no such table: Hogares"; falta `launchSettings.json`; `Assert.Throws` sin excepción; excepción y "no such table: Mascotas" llegando al cliente ×2 | 8/8 |
| 1 (ronda 2) | +5 extra | Pasaban en la primera corrida (comportamiento ya correcto); prueba de mutación: 3/5 fallaron | 13/13 |
| 2 | 26 (36 casos) | 36/36 por aserciones (valores incorrectos de esqueletos: `Expected NotFound Actual OK`, `Expected SeeOther Actual OK`, colecciones vacías, etc.) | 49/49 suite |
| 3 | 21 | 21/21: 11 tests puros por `NotImplementedException`, 10 por aserción; ronda 2 con pruebas de mutación (código 2067→2068 y `EsVencida` solo con `TimeOnly` → fallan los asserts esperados) | 70/70 suite |
| 4 | 26 | 26/26 por aserciones (`Expected OK Actual NotFound`, `Expected 360 Actual 980` sin viewport, `598/783/791` sin CSS) | 96/96 suite |

El verificador contrastó cada referencia con los tests en disco y coincide. Nota: el rojo por
`NotImplementedException` de los 11 tests puros del Bloque 3 es más débil que un rojo por aserción; lo
compensan las pruebas de mutación de esa ronda.

## Advertencias (no bloquean)

1. **AC-21 / AC-22 solo probados sobre la función pura.** Ningún test inserta un `RegistroCumplimiento` y pide `GET /`; no se ejecutan con datos el join de registros (`estado-del-dia-service.cs:41-46`), el subquery `TieneRegistro` (`:38`) ni la rama con registro de `textos-estado-del-dia.cs:29,32`. → Agregar un `Index_*` con registros de hoy y de ayer en FEAT-001b.
2. **Desvío de contrato:** `src/shared/resultados/resultado.cs:8` es `sealed record` (clase); la spec pedía `readonly record struct`. Cambio deliberado en la corrección del Bloque 1 (evita un `default` inválido). → Mencionarlo en el PR.
3. **Cobertura de funciones en bruto 71,5%:** 74 de los 76 métodos sin cubrir los genera el compilador (48 `set_`, 13 `get_`, 13 constructores de copia de records). → Agregar un `.runsettings` que excluya `CompilerGeneratedAttribute`.
4. **W-VER-01 código muerto:** `MascotasService.ListarAsync` (`mascotas-service.cs:15-19`) no tiene llamadores; rama inalcanzable en `configuracion.cshtml.cs:59-62`. → Decidir en FEAT-001b si se usa o se quita.
5. **W-VER-02:** `textos-estado-del-dia.cs` 5/8 ramas (62,5%) y `mascotas-service.cs` 5/9 líneas, por los puntos 1 y 4.
6. **W-VER-03 tests frágiles:** `generacion-diaria-worker-tests.cs:30,217-231` esperan en tiempo real (hasta 5 s por condición); `playwright-fixture.cs` descarga Chromium la primera vez (excluibles con `--filter "Categoria!=Navegador"`).
7. **Ancho 100% (NFR-02):** aplicado al input de hora y al botón, no a los radios (aceptado en CODE; Playwright confirma sin scroll horizontal).

Informativos: `instancias-service.cs:18` usa `SqliteExtendedErrorCode 2067` (más estricto que el 19 de la spec); el flujo manual en navegador de la verificación final no se ejecutó en VERIFY para no crear `petcare.db` (cubierto por Playwright y por la prueba manual con base temporal del Bloque 4).

## Pendientes para FEAT-001b

- Test de `GET /` con registros de cumplimiento (WARN 1).
- Acotar la consulta de registros para "último de cualquier día" (auditoría del Bloque 4).
- Decidir `MascotasService.ListarAsync` (WARN 4).
- Filtrar por `HogarId` si llega un ticket multi-hogar.
