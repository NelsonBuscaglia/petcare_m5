# Verificación FEAT-001b: Cumplimiento, email de vencimiento y actualización automática del estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| Tier | FEATURE |
| PRD | docs/daw/prd/prd-FEAT-001b.md (18 FR, 4 NFR, 31 AC) |
| Spec | docs/daw/specs/spec-FEAT-001b.md (5 bloques, 119 tests nombrados) |
| Threat model / SAST | docs/daw/security/threat-FEAT-001b.md · docs/daw/security/sast-FEAT-001b.md |
| Código verificado | commits db11a7b, 3efe176, 6e9cce3, 09f8b62, fdca73b (+ ce58a05, reporte SAST) |
| Fecha | 2026-10-26 |
| Verificador | `daw-module-verifier` (agente que no escribió el código) |
| Resultado final | **PASSED** (ronda 2): 54 PASS, 0 FAIL, 2 WARN |

## Rondas

| Ronda | Resultado | Motivo |
|---|---|---|
| 2 | **PASSED** (54 PASS, 0 FAIL, 2 WARN) | Tras el corrective loop (commits 79186c8 y 76fd7d2): W-1, W-2 y W-3 cerrados; W-4 sigue igual; W-5 nuevo y conocido (ticket aparte). Detalle en "Ronda 2" al final. |
| 1 | **PASSED** (50 PASS, 0 FAIL, 4 WARN) | El verificador no tenía los reportes TDD de los implementadores. Se le aportó la evidencia de cada bloque (ver abajo), que ya se había contrastado bloque por bloque en la etapa (a) de CODE. |

**Decisión del usuario tras la ronda 1:** aunque la ronda pasó, se abre un corrective loop a CODE para cubrir con tests W-1 (reevaluación de la ventana antes de cada intento, NFR-01), W-2 (elección de TLS por puerto, M6, y autenticación SMTP) y W-3 (`horaReal` vacía por HTTP). No se cambia lógica de producción.

## Ejecuciones

| Comando | Resultado |
|---|---|
| `dotnet test petcare.slnx --settings tests/coverage.runsettings` | 227/227 |
| `dotnet test petcare.slnx --filter "Categoria!=Navegador"` | 215/215 |
| `dotnet build petcare.slnx --no-incremental` | 0 warnings, 0 errores |
| `dotnet list petcare.slnx package --vulnerable --include-transitive` | Sin paquetes vulnerables |
| `dotnet format --verify-no-changes --diagnostics IDE0005 IDE0051 IDE0052 IDE0060` | Sin hallazgos |

## F-VER-01: trazabilidad PRD → código → test (31/31 ✅)

Todos los tests verifican comportamiento real (contenido del parcial, filas en la base o intentos de envío), ninguno solo el status code.

| AC | Código | Test(s) |
|---|---|---|
| AC-01 | `_estado-del-dia.cshtml:10` (polling) + `index.cshtml.cs:OnGetEstadoAsync` | `Refresco_OtroCuidadorRegistra_DesapareceYSumaPaseo` |
| AC-02 | ídem + `EstadoInstancia.EsVencida` | `Refresco_SinInteraccion_MarcaPendienteA1215` |
| AC-03 | ídem | `Refresco_OtroCuidadorDaDeAlta_ApareceInstancia` |
| AC-04 | ídem | `Refresco_Medianoche_DesapareceInstancia`. Verifica que desaparece la instancia de ayer, no que aparece la del día nuevo, porque `GeneracionDiariaWorker` está desactivado en los tests. Limitación aprobada por el usuario en CODE. |
| AC-05 | `registros-service.cs:RegistrarAsync`, `index.cshtml.cs:OnPostCumplirAsync` | `Registrar_InstanciaVigente_CreaRegistro`; `Cumplir_Htmx_InstanciaDesapareceYSumaPaseo` |
| AC-06 | `PrepararFormularioAsync` | `FormularioCumplimiento_A1030_ProponeHoraActual`; `Cumplir_AceptandoHoraPropuesta_UltimaComida1030` |
| AC-07 | `validacion-cumplimiento.cs:Validar` | `Cumplir_Hora1000_UltimaComida1000` |
| AC-08 | `Validar` (HoraPosteriorALaActual) | `Validar_HoraPosteriorALaActual`; `Cumplir_Hora1100_RechazaYMantieneInstancia` |
| AC-09 | `RegistrarAsync` (violación única) + `Validar` | `Registrar_Concurrente_SegundoRecibeYaCumplida`; `Cumplir_DosVeces_SegundoMuestraYaCumplida` |
| AC-10 | `Validar` (InstanciaExpirada) | `Registrar_InstanciaDeAyer_InstanciaExpirada`; `Cumplir_TrasMedianoche_MuestraExpiro` |
| AC-11 | `RegistrarAsync` | `Cumplir_SoloHoy_DejaDeAparecerEnConfiguracion` |
| AC-12 | `RegistrarAsync` + `ArmadoEstadoDelDia.Armar` | `Cumplir_UnicoDeber_MuestraSinTareasYUnPaseo` |
| AC-13 | `armado-estado-del-dia.cs:Armar` + parcial | `Index_ConRegistroDeHoy_MuestraRegistroConEliminar` |
| AC-14 | `registros-service.cs:EliminarAsync`, `OnPostEliminarRegistroAsync` | `EliminarRegistro_UnoDeDos_MuestraUnPaseo` |
| AC-15 | `EliminarAsync` (RegistroDeOtroDia) | `Eliminar_RegistroDeAyer_RegistroDeOtroDia`; `EliminarRegistro_DeAyer_MuestraMensaje` |
| AC-16 | `EliminarAsync` (ReactivadaEnUtc) | `EliminarRegistro_ComidaCumplidaPorError_VuelvePendiente` |
| AC-17 | `EliminarAsync` | `Eliminar_Concurrente_SoloUnoElimina`; `EliminarRegistro_YaEliminado_MuestraMensaje` |
| AC-18 | `emails-vencimiento-service.cs:ProcesarAsync`, `vencimientos-worker.cs` | `Procesar_0915SinCumplir_EnviaAAnaYBeto`; `VencimientosWorker_SinAppAbierta_EnviaAntesDe0920` |
| AC-19 | `condicion-email.cs:DebeIntentarse` | `Procesar_Cumplida0905_NoEnvia` |
| AC-20 | `DebeIntentarse` | `Procesar_TodoElDia_ExactamenteUnEmail` |
| AC-21 | `mensaje-vencimiento.cs:Armar` | `Armar_ComidaFirulais0900_IncluyeDatos` |
| AC-22 | `DebeIntentarse` (ventana desde la reactivación) | `DebeIntentarse_Reactivada1000_VentanaDesde1000`; `Procesar_EliminarRegistroA1000_EnviaAntesDe1005` |
| AC-23 | `DebeIntentarse` | `Procesar_EmailYaAceptado_NoReenviaTrasEliminar` |
| AC-24 | `deberes-service.cs` (VencidaAlGenerarse) + `DebeIntentarse` | `Procesar_AltaConHoraVencida_NoEnvia` |
| AC-25 | `instancias-service.cs:GenerarDelDiaAsync` + `DebeIntentarse` | `Procesar_GeneradaTrasCaida_NoEnvia` |
| AC-26 | `ProcesarAsync` | `Procesar_RechazaHasta0917_AceptadoAntesDe0920` |
| AC-27 | `DebeIntentarse` (ventana de 5 min) | `Procesar_RechazaHasta0930_SinIntentosDespuesDe0920` |
| AC-28 | `DebeIntentarse` + `EliminarAsync` | `Procesar_VentanaPerdidaYReactivada1030_EnviaAntesDe1035` |
| AC-29 | `ProcesarAsync` (timeout de 30 s), `SmtpEnvioEmail` | `Procesar_SinRespuesta_FallaA30sYAceptaAntesDe0920`; `Smtp_SinRespuesta_SinRespuesta` |
| AC-30 | `site.css` (sin cambios en el Bloque 5, aprobado) + parciales | `SinScrollHorizontal_360px_RegistrarCumplimientoEditandoHora`; `SinScrollHorizontal_360px_EliminarRegistro` |
| AC-31 | parciales: "Cumplido" + "Confirmar" | `RegistrarCumplimiento_HoraPorDefecto_DosClics` |

## F-VER-02 / F-VER-06: tareas y tests del spec ✅

| Bloque | Tests del spec |
|---|---|
| 1. Esquema y piezas compartidas | 11/11 |
| 2. Registro de cumplimiento | 31/31 |
| 3. Registros del día y eliminación | 19/19 |
| 4. Email de vencimiento | 44/44 |
| 5. Actualización automática | 14/14 |
| **Total** | **119/119**: existen con el nombre exacto y pasan. El diff agrega 119 tests nuevos (129 métodos públicos, menos 7 `Dispose` y 3 helpers), que coincide con la "Verificación final". |

Desvíos aprobados durante CODE:
- `PrepararFormularioAsync` devuelve `TimeOnly`.
- `coverage.runsettings` excluye los miembros sintéticos por nombre.
- `site.css` no se tocó en el Bloque 5.

## Evidencia TDD (tests en rojo antes de implementar) ✅

| Bloque | En rojo antes | Fuente |
|---|---|---|
| 1 | 11/11 | Informe del implementador, contrastado en la etapa (a) |
| 2 | 31/31 (34 casos), por aserción, con esqueletos que compilaban | Informe del implementador + etapa (a): "34 tests en rojo antes de implementar" |
| 3 | 19/19 | Informe del implementador + etapa (a), ronda R1/R2 |
| 4 | 44/44 (49 casos) | Informe del implementador + etapa (a) |
| 5 | 11/14 por aserción o compilación | Los otros 3 (`Estado_FallaDeBase_500Generico` y los dos de 360 px) son de caracterización: prueban comportamiento entregado en bloques anteriores. Se demostró que pueden fallar rompiendo el código a propósito: sin `UseExceptionHandler` y con `min-width: 420px` fallan (`Expected: 360 Actual: 453`). La etapa (a) lo aceptó. Los tests de AC-03 y AC-04, después de alinearlos con el PRD, fallan con el polling desactivado. |

## F-VER-03: cobertura (29 archivos `.cs` de `src` del diff, sin migraciones) ✅

| Métrica | Valor |
|---|---|
| Líneas | 97,4% (789/810) |
| Ramas | 91,9% (251/273) |
| Funciones | 98,5% (129/131) |

Los únicos métodos sin cubrir son el constructor público de `SmtpEnvioEmail` y la lambda de `program.cs:48-50` que lo crea.

## F-VER-04: sad paths ✅

Todos los handlers y funciones con entrada tienen al menos un test con entrada inválida:
- `GET Estado`: 500.
- `GET FormularioCumplimiento`: 404, 400 y ya cumplida.
- `POST Cumplir`: 404, 400 sin id, 400 sin token, 500, hora posterior, ya cumplida, expirada y HTML codificado.
- `POST EliminarRegistro`: 400 sin id, 400 sin token, 500, de otro día y ya eliminado.
- Funciones: `Validar`, `FormatoHora`, `ZonaHogar.AUtc`, `DebeIntentarse`, `ValidacionOpcionesEmail`, `OpcionesRefresco`, el arranque y SMTP (550 y sin respuesta).

## F-VER-05: build ✅

0 warnings, 0 errores.

## Lista "Verificación final" del spec ✅

- ✅ Build sin warnings; suite completa 227/227 y sin navegador 215/215; cobertura ≥ 80% en las tres métricas.
- ✅ AC-01…AC-31 tienen cada uno al menos un test que pasa.
- ✅ El grep de patrones prohibidos no encuentra nada en `src/`.
- ✅ Los logs no llevan direcciones de email ni `ex.Message`.
- ✅ Ningún `appsettings*.json` ni `launchSettings.json` tiene `Email:Smtp:*`.
- ✅ Ningún PageModel usa `PetCareDbContext`.
- ✅ `RegistroCumplimiento` e `Instancia` se escriben solo en `instancias/data`, más la excepción declarada de `DeberesService`.
- ✅ Sin paquetes vulnerables.
- No ejecutado: la prueba manual con `dotnet run` (dos pestañas y el email en la consola). La cubren los tests Playwright de AC-01..AC-04 y los del worker.

## Warnings (no bloquean)

| Id | Regla | Archivo | Hallazgo |
|---|---|---|---|
| W-1 | W-VER-02 | `src/features/emails/data/emails-vencimiento-service.cs:73-75`, `:101` | 85,7% de ramas. Ningún test ejerce la reevaluación de la ventana antes de cada intento, cuando un envío anterior lento deja a otra candidata fuera de ventana. El spec lo pide en NFR-01. |
| W-2 | W-VER-02 | `src/features/emails/data/smtp-envio-email.cs:28-33`, `:45-49`, `:63-66` | 80% de líneas y 75% de ramas. Quedan sin test automatizado el constructor público, que elige `SslOnConnect` en el puerto 465 y `StartTls` en los demás (mitigación M6), y la autenticación SMTP. |
| W-3 | F-VER-04 (parcial) | `src/features/estado-del-dia/ui/index.cshtml.cs:92`, `src/features/instancias/domain/cumplimiento-error.cs:21` | Ningún test HTTP manda `horaReal` vacío; el error está probado solo en el dominio (`Validar_HoraObligatoria`). |
| W-4 | W-VER-03 | `tests/features/estado-del-dia/refresco-navegador-tests.cs`, `tests/features/emails/emails-vencimiento-service-tests.cs:222`, `tests/features/estado-del-dia/sin-scroll-horizontal-tests.cs:89` | Hay estado compartido entre tests: el `FakeTimeProvider` y la base de la fixture de Playwright, mitigados con `IrAProxima` y una mascota por test. Hay esperas fijas reales (3 s y 1,5 s, pedidas por el spec) y una aserción negativa con `Task.Delay(50)`. 3 corridas extra sin intermitencias. |

Nota fuera del diff: `src/shared/hosting/generacion-diaria-worker.cs:53` (de FEAT-001a) pasa la excepción completa a `LogGeneracionFallida`. No viola M3, porque ahí no interviene MailKit, pero no sigue el criterio de "solo tipo y HResult" del worker nuevo.

---

## Ronda 2: después del corrective loop (2026-10-26, HEAD 76fd7d2)

**Resultado: PASSED.** 54 PASS, 0 FAIL, 2 WARN. Verificador: `daw-module-verifier`, que no escribió los tests de la corrección.

### Ejecuciones

| Comando | Resultado |
|---|---|
| `dotnet build petcare.slnx --no-incremental` | 0 warnings, 0 errores |
| `dotnet test petcare.slnx --settings tests/coverage.runsettings` | 239/239 (227 + 12 casos nuevos) |
| `--filter "Categoria!=Navegador"` | 227/227 |
| 3 corridas extra de las clases tocadas | 49/49 en cada una |
| `dotnet format --verify-no-changes --diagnostics IDE0005 IDE0051 IDE0052 IDE0060` | Sin hallazgos |
| `dotnet list package --vulnerable --include-transitive` | Sin vulnerables |

### Reglas

- **F-VER-01 ✅:** AC-01..AC-31 siguen trazados. Los 42 tests de la tabla de la ronda 1 existen y pasan. El cambio en `src` (`smtp-envio-email.cs:28-34`) no toca ningún camino de los AC.
- **F-VER-02 / F-VER-06 ✅:** 119/119 tests del spec, con el nombre exacto.
- **F-VER-03 ✅:**

  | Métrica | Ronda 2 | Ronda 1 |
  |---|---|---|
  | Líneas | 99,4% (803/808) | 97,4% |
  | Ramas | 96,0% (262/273) | 91,9% |
  | Funciones | 99,2% (131/132) | 98,5% |

  La única función sin cubrir es la lambda de `program.cs:48-50`.
- **F-VER-04 ✅:** ahora también hay tests para la hora vacía o ausente por HTTP, las credenciales SMTP incorrectas, un usuario sin clave y un servidor sin STARTTLS.
- **F-VER-05 ✅**, **W-VER-01 ✅**.
- **Evidencia de que los tests nuevos pueden fallar ✅:** como no hay lógica nueva, el verificador repitió las mutaciones en una copia aislada. Al quitar la reevaluación de la ventana, usar `StartTlsWhenAvailable`, saltear `AuthenticateAsync`, sacar el filtro `when` de `:101` y cambiar el texto de HoraObligatoria, fallaron los tests correspondientes.

### Estado de los warnings

| Id | Estado | Detalle |
|---|---|---|
| W-1 | ✅ Cerrado | Ramas de `emails-vencimiento-service.cs`: de 85,7% a 100%. Tests en `emails-vencimiento-service-tests.cs:244` y `:268`. |
| W-2 | ✅ Cerrado | `smtp-envio-email.cs`: líneas de 80% a 100% y ramas de 75% a 96,9%. Solo queda sin cubrir la rama `opciones == null` de `:28`. M6 queda fijada en `smtp-envio-email-tests.cs:134` y `:140`; la autenticación en `:92`, `:106` y `:120`. |
| W-3 | ✅ Cerrado | `cumplimiento-page-tests.cs:235`, Theory con `""` y `null`, con y sin HTMX. |
| W-4 | ⚠️ Sin cambios (W-VER-03) | Las mismas esperas y la misma fixture compartida que en la ronda 1. Los tests nuevos no agregan esperas fijas. Matiz: `smtp-envio-email-tests.cs:140` usa el timeout real de 30 s. |
| W-5 | ⚠️ Nuevo, conocido | Viola la convención "errores tipados" de AGENTS.md. En `src/features/emails/data/smtp-envio-email.cs:102-108`, `Clasificar` no traduce `NotSupportedException` (servidor sin STARTTLS) ni `ServiceNotAuthenticatedException`. `smtp-envio-email-tests.cs:146` fija el comportamiento actual. La excepción corta el `foreach` de `emails-vencimiento-service.cs:70`, así que esa pasada no intenta las demás candidatas, y termina en `LogPasadaFallida` (solo tipo y HResult). No hay fuga ni envío en claro. **Decisión del usuario: ticket aparte.** |

La prueba manual con `dotnet run` no se ejecutó, igual que en la ronda 1.
