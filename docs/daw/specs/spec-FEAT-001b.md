# Spec FEAT-001b: Cumplimiento, email de vencimiento y actualización automática del estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| PRD | docs/daw/prd/prd-FEAT-001b.md |
| Tier | FEATURE |
| Fecha | 2026-10-25 |
| Spec loops | 2 |
| Base | código de FEAT-001a en `main` (spec `docs/daw/specs/spec-FEAT-001a.md`, cuyas decisiones transversales siguen vigentes salvo lo que se amplía acá) |

## Resumen

Sobre el código de FEAT-001a: el cuidador registra el cumplimiento de una instancia vigente desde el
estado del día (botón "Cumplido" + "Confirmar", hora real editable) y elimina registros del día; ambas
escrituras las hace un `RegistrosService` nuevo en la feature `instancias`. Un segundo
`BackgroundService` (`VencimientosWorker`) revisa cada minuto qué instancias vencieron y manda un
único email de vencimiento a todos los cuidadores, con reintentos dentro de una ventana de 5 minutos;
el envío es a la consola por defecto y por SMTP (MailKit) solo fuera de Development. El estado del
día se refresca solo con polling HTMX cada 10 s, pausado mientras hay un formulario de cumplimiento
abierto. Una migración nueva agrega lo que el email necesita (`VencidaAlGenerarse`,
`ReactivadaEnUtc`, tabla `EmailsVencimiento`).

## Decisiones transversales (además de las de FEAT-001a)

- **Escrituras por feature:** `RegistroCumplimiento` e `Instancia` pertenecen a `instancias`; solo
  `RegistrosService` e `InstanciasService` (`instancias/data`) las escriben. **Excepción declarada:**
  `DeberesService` crea la instancia del día en el alta (FR-07 de FEAT-001a) y ahora también setea su
  `VencidaAlGenerarse`. `EmailsVencimientoService` escribe solo `EmailVencimiento` (feature `emails`).
- **Handlers en la página del estado del día:** registrar, eliminar, abrir el formulario y refrescar
  son handlers del `IndexModel` de `estado-del-dia/ui`, porque todas sus respuestas son el parcial del
  estado del día. El `IndexModel` inyecta solo servicios (`EstadoDelDiaService`, `RegistrosService`,
  `IOptions<OpcionesRefresco>`) y sus handlers no hacen más que traducir errores tipados a mensaje o
  status. Las features `emails` e `instancias` no tienen `ui/` propia en este ticket.
- **Regla de los 15 minutos en un solo lugar:** `EstadoInstancia.MomentoDeVencimiento(fecha, hora)`
  (= `fecha.ToDateTime(hora) + 15 min`); `EsVencida` y la regla del email la usan.
- **`DateTimeOffset` en SQLite:** EF Core SQLite lo guarda como TEXT. Toda fecha-hora absoluta se
  persiste en UTC (offset `+00:00`) y **nunca se compara ni se ordena en SQL** (`Where`/`OrderBy`):
  se trae a memoria y se evalúa con funciones puras. Test que verifica el offset cero.
- **Email en desarrollo:** AGENTS.md: los emails se escriben en la consola. `ConsolaEnvioEmail`
  escribe a un `TextWriter` (por defecto `Console.Out`), **no** a `ILogger`. **M3 reformulada:**
  ningún `ILogger` de `src/` recibe direcciones de email ni `Exception.Message` de MailKit; el único
  lugar donde aparecen destinatarios es la salida del envío a consola, con las direcciones completas
  (decisión del usuario en PLAN; son direcciones de `ejemplo.com`).
- **Modo de email:** `Email:Modo` = `Consola` (default en `appsettings.json`) | `Smtp`. `Smtp` solo se
  configura por variables de entorno (`Email__Modo`, `Email__Smtp__Host`, `__Puerto`, `__Usuario`,
  `__Clave`, `__Remitente`), nunca en `appsettings*.json` ni en `launchSettings.json`. **En
  Development la app no arranca si el modo no es `Consola`.**
- **Respuestas HTMX:** los rechazos de negocio devuelven **200** con el parcial y el mensaje (HTMX no
  reemplaza contenido con 4xx); 404 solo cuando el recurso no existe; 400 por antiforgery o parámetro
  faltante. Sin HTMX: éxito → 303 a `/`; rechazo → 200 con la página completa y el mensaje.
- **Seguridad (threat model `docs/daw/security/threat-FEAT-001b.md`):** M3 reformulada (arriba), M3-bis (sin `Email:Smtp:*` en archivos versionados), M5 (asunto sin `\r`/`\n`), M6 (TLS con `StartTls`/`SslOnConnect`, sin reemplazar la validación de certificados), M7 (destinatarios en Bcc, remitente en To).
- **Riesgo aceptado (usuario, PLAN 2026-10-25):** si el servidor SMTP acepta el email y falla el
  guardado de `EmailVencimiento`, la pasada siguiente lo reenvía (duplicado posible).

### Excepciones de estructura y nombres (además de las de FEAT-001a)

| Ruta / nombre | Motivo |
|---|---|
| `src/migrations/{timestamp}_CumplimientoYEmails.cs` y `.Designer.cs` | Generados por `dotnet ef`; no se renombran |
| `src/shared/hosting/vencimientos-worker.cs` | Hosting transversal, igual que `generacion-diaria-worker.cs` |
| `tests/coverage.runsettings` | Archivo de configuración del runner |

### Dependencias nuevas

| Paquete | Proyecto | Para qué | Alternativa descartada |
|---|---|---|---|
| `MailKit` 4.18.2 (trae `MimeKit` 4.18.2) | src | Envío SMTP (en el stack de AGENTS.md) | `System.Net.Mail.SmtpClient`: obsoleto según Microsoft |

Sin paquetes de test nuevos: el servidor SMTP de prueba se escribe con `TcpListener`.

## Cobertura: PRD → bloques

| Requerimiento | Cubierto por |
|---|---|
| FR-01 | Bloque 5 (polling), Bloque 2 (handler `Estado`) |
| FR-02 | Bloque 2 |
| FR-03 | Bloque 2 |
| FR-04 | Bloque 2 |
| FR-05 | Bloque 2 |
| FR-06 | Bloque 2 |
| FR-07 | Bloque 2 |
| FR-08 | Bloque 3 |
| FR-09 | Bloque 3 |
| FR-10 | Bloque 3 |
| FR-11 | Bloque 3 |
| FR-12 | Bloque 3 |
| FR-13 | Bloque 4 |
| FR-14 | Bloque 1 (`VencidaAlGenerarse`), Bloque 4 (regla) |
| FR-15 | Bloque 4 |
| FR-16 | Bloque 4 |
| FR-17 | Bloque 1 (tabla), Bloque 4 (inserción solo al aceptar) |
| FR-18 | Bloque 4 |
| NFR-01 | Estrategia: `VencimientosWorker` hace una pasada al arrancar y otra cada 1 min (`PeriodicTimer` con `TimeProvider`); la regla `CondicionEmail` intenta el envío en toda pasada con `ahora < desde + 5 min`, con `desde` = momento de vencimiento o de reactivación. Primer intento ≤ ~1 min después de la condición; reintentos en las pasadas siguientes dentro de la ventana; la ventana se reevalúa antes de cada intento con la hora actual. Verificado con `FakeTimeProvider` (AC-18, AC-22, AC-26, AC-28, AC-29). |
| NFR-02 | Estrategia: contenedor `#estado-del-dia` con `hx-trigger="every 10s [...]"` (10 s + render < 15 s; configurable en 1..15 s), `hx-sync` para que un poll no pise una acción. Verificado con Playwright con intervalo de 1 s y `FakeTimeProvider` (AC-01..AC-04). |
| NFR-03 | Estrategia: "Cumplido" abre el formulario con la hora actual precargada y "Confirmar" lo registra: 2 toques. Verificado con Playwright (AC-31). |
| NFR-04 | Estrategia: CSS mobile-first de FEAT-001a, más formulario y lista de registros a 100% de ancho, botones apilables. Verificado con Playwright a 360 px (AC-30). |

| Criterio | Test(s) |
|---|---|
| AC-01 | B5 `Refresco_OtroCuidadorRegistra_DesapareceYSumaPaseo` |
| AC-02 | B5 `Refresco_SinInteraccion_MarcaPendienteA1215` |
| AC-03 | B5 `Refresco_OtroCuidadorDaDeAlta_ApareceInstancia` |
| AC-04 | B5 `Refresco_Medianoche_DesapareceInstancia` |
| AC-05 | B2 `Registrar_InstanciaVigente_CreaRegistro`; B2 `Cumplir_Htmx_InstanciaDesapareceYSumaPaseo` |
| AC-06 | B2 `FormularioCumplimiento_A1030_ProponeHoraActual`; B2 `Cumplir_AceptandoHoraPropuesta_UltimaComida1030` |
| AC-07 | B2 `Cumplir_Hora1000_UltimaComida1000` |
| AC-08 | B2 `Validar_HoraPosteriorALaActual`; B2 `Cumplir_Hora1100_RechazaYMantieneInstancia` |
| AC-09 | B2 `Registrar_Concurrente_SegundoRecibeYaCumplida`; B2 `Cumplir_DosVeces_SegundoMuestraYaCumplida` |
| AC-10 | B2 `Registrar_InstanciaDeAyer_InstanciaExpirada`; B2 `Cumplir_TrasMedianoche_MuestraExpiro` |
| AC-11 | B2 `Cumplir_SoloHoy_DejaDeAparecerEnConfiguracion` |
| AC-12 | B2 `Cumplir_UnicoDeber_MuestraSinTareasYUnPaseo` |
| AC-13 | B3 `Index_ConRegistroDeHoy_MuestraRegistroConEliminar` |
| AC-14 | B3 `EliminarRegistro_UnoDeDos_MuestraUnPaseo` |
| AC-15 | B3 `Eliminar_RegistroDeAyer_RegistroDeOtroDia`; B3 `EliminarRegistro_DeAyer_MuestraMensaje` |
| AC-16 | B3 `EliminarRegistro_ComidaCumplidaPorError_VuelvePendiente` |
| AC-17 | B3 `Eliminar_Concurrente_SoloUnoElimina`; B3 `EliminarRegistro_YaEliminado_MuestraMensaje` |
| AC-18 | B4 `Procesar_0915SinCumplir_EnviaAAnaYBeto`; B4 `VencimientosWorker_SinAppAbierta_EnviaAntesDe0920` |
| AC-19 | B4 `Procesar_Cumplida0905_NoEnvia` |
| AC-20 | B4 `Procesar_TodoElDia_ExactamenteUnEmail` |
| AC-21 | B4 `Armar_ComidaFirulais0900_IncluyeDatos` |
| AC-22 | B4 `DebeIntentarse_Reactivada1000_VentanaDesde1000`; B4 `Procesar_EliminarRegistroA1000_EnviaAntesDe1005` |
| AC-23 | B4 `Procesar_EmailYaAceptado_NoReenviaTrasEliminar` |
| AC-24 | B4 `Procesar_AltaConHoraVencida_NoEnvia` |
| AC-25 | B4 `Procesar_GeneradaTrasCaida_NoEnvia` |
| AC-26 | B4 `Procesar_RechazaHasta0917_AceptadoAntesDe0920` |
| AC-27 | B4 `Procesar_RechazaHasta0930_SinIntentosDespuesDe0920` |
| AC-28 | B4 `Procesar_VentanaPerdidaYReactivada1030_EnviaAntesDe1035` |
| AC-29 | B4 `Procesar_SinRespuesta_FallaA30sYAceptaAntesDe0920`; B4 `Smtp_SinRespuesta_SinRespuesta` |
| AC-30 | B5 `SinScrollHorizontal_360px_RegistrarCumplimientoEditandoHora`; B5 `SinScrollHorizontal_360px_EliminarRegistro` |
| AC-31 | B5 `RegistrarCumplimiento_HoraPorDefecto_DosClics` |

## Dependencias entre bloques

- Bloque 1: ninguna (sobre FEAT-001a). Va primero.
- Bloque 2 depende de 1 (`FormatoHora`, `VencidaAlGenerarse` en las instancias creadas).
- Bloque 3 depende de 1 y 2 (registros creados con `RegistrarAsync`, parcial del estado del día).
- Bloque 4 depende de 1 (esquema, `MomentoDeVencimiento`, `AUtc`) y de 3 (`EliminarAsync` en los tests de AC-22, AC-23, AC-28).
- Bloque 5 depende de 2 y 3 (formulario, parcial y handler `Estado`).
- Orden: 1 → 2 → 3 → 4 → 5.

---

## Bloque 1 — Esquema y piezas compartidas

**Archivos**
- `src/features/instancias/domain/instancia.cs` (modificado) — `+ bool VencidaAlGenerarse`, `+ DateTimeOffset? ReactivadaEnUtc`.
- `src/features/instancias/data/instancia-configuration.cs` (modificado) — mapeo de las dos columnas.
- `src/features/emails/domain/email-vencimiento.cs` (nuevo) — entidad `EmailVencimiento`.
- `src/features/emails/data/email-vencimiento-configuration.cs` (nuevo).
- `src/shared/data/petcare-db-context.cs` (modificado) — `DbSet<EmailVencimiento> EmailsVencimiento`.
- `src/migrations/{timestamp}_CumplimientoYEmails.cs`, `{timestamp}_CumplimientoYEmails.Designer.cs` (nuevos, generados con `dotnet ef migrations add CumplimientoYEmails --project src --output-dir migrations`), `src/migrations/PetCareDbContextModelSnapshot.cs` (modificado, generado).
- `src/features/instancias/domain/estado-instancia.cs` (modificado) — `+ DateTime MomentoDeVencimiento(DateOnly fecha, TimeOnly hora)`; `EsVencida` lo usa.
- `src/shared/tiempo/zona-hogar.cs` (modificado) — `+ DateTimeOffset AUtc(DateTime local)` (lanza `ArgumentException` si `local.Kind != Unspecified`; devuelve offset cero); actualizar el comentario "única conversión".
- `src/shared/tiempo/formato-hora.cs` (nuevo) — `static partial class FormatoHora` con `bool TryParse(string? texto, out TimeOnly hora)` sobre la regex `^([01][0-9]|2[0-3]):[0-5][0-9]\z`.
- `src/features/deberes/domain/validacion-alta-deber.cs` (modificado) — usa `FormatoHora`; se **borra** el método privado `FormatoHora()` (choque de nombres).
- `src/features/deberes/data/deberes-service.cs` (modificado) — la instancia del alta lleva `VencidaAlGenerarse = EstadoInstancia.EsVencida(hoy, hora, ahoraLocal)`.
- `src/features/instancias/data/instancias-service.cs` (modificado) — ídem en `GenerarDelDiaAsync`; la constante `SqliteConstraintUnique` (2067) pasa a `src/shared/data/sqlite-errores.cs`.
- `src/shared/data/sqlite-errores.cs` (nuevo) — `const int ConstraintUnique = 2067` y helper `EsViolacionUnica(DbUpdateException)`.
- `src/features/hogar/data/mascotas-service.cs` (modificado) — se quita `ListarAsync` (sin llamadores; pendiente W-VER-01 de FEAT-001a).
- `tests/coverage.runsettings` (nuevo) — Microsoft Code Coverage, excluye `^System\.Runtime\.CompilerServices\.CompilerGeneratedAttribute$` y `src/migrations/`.
- `tests/petcare-tests.csproj` (modificado) — `<RunSettingsFilePath>$(MSBuildProjectDirectory)/coverage.runsettings</RunSettingsFilePath>`.
- `tests/infra/base-de-test.cs` (modificado) — `InsertarInstancia(..., bool vencidaAlGenerarse = false, DateTimeOffset? reactivadaEnUtc = null)` (las 10 llamadas existentes no cambian) y `+ InsertarEmailVencimiento(int instanciaId, DateTimeOffset aceptadoEnUtc)`.
- `tests/shared/zona-hogar-tests.cs`, `tests/features/instancias/estado-instancia-tests.cs`, `tests/features/deberes/deberes-service-tests.cs`, `tests/features/instancias/instancias-service-tests.cs` (modificados) — tests nuevos del bloque.
- `tests/shared/formato-hora-tests.cs`, `tests/features/emails/email-vencimiento-tests.cs`, `tests/shared/migracion-tests.cs` (nuevos).

**Lógica**
Esquema y helpers que usan los bloques 2–4. `VencidaAlGenerarse` se calcula al crear cada instancia
(alta o generación diaria) contra la hora local de ese momento.

**Modelo de datos**
- `Instancia` (modificada): `+ VencidaAlGenerarse` INTEGER (bool) not null — la migración usa el valor constante `false` para filas existentes (no es un default calculado); `+ ReactivadaEnUtc` TEXT (`DateTimeOffset`, siempre offset cero) null.
- `EmailVencimiento` (nueva, tabla `EmailsVencimiento`): `Id` int PK; `InstanciaId` int FK → `Instancia.Id` not null, `OnDelete Restrict`, **índice único**; `AceptadoEnUtc` TEXT (`DateTimeOffset`, offset cero) not null. Sin defaults calculados por la base.

**Validación de entrada**
- No aplica: no recibe entrada de usuario.

**Manejo de errores**
- `ZonaHogar.AUtc` con `DateTimeKind` distinto de `Unspecified` → `ArgumentException` (error de programación, no se captura).
- Segundo `EmailVencimiento` para la misma instancia → `DbUpdateException` con `SqliteExtendedErrorCode` 2067 (lo trata el Bloque 4).

**Tests requeridos**
- [ ] `Migracion_AgregaColumnasYTablaEmails` — tras `Migrate()` existen `Instancias.VencidaAlGenerarse`, `Instancias.ReactivadaEnUtc` y la tabla `EmailsVencimiento` con índice único en `InstanciaId` — valida FR-17, FR-14.
- [ ] `MomentoDeVencimiento_Suma15Minutos` — 25/10 09:00 → 25/10 09:15; 25/10 23:50 → 26/10 00:05 — valida FR-13.
- [ ] `AUtc_ConvierteConDesfaseFijo` — 25/10 09:15 local → 25/10 12:15 +00:00 — valida FR-13.
- [ ] `AUtc_KindUtc_Lanza` — `DateTimeKind.Utc` → `ArgumentException` — error documentado.
- [ ] `FormatoHora_TryParse` — válidos `00:00`, `23:59`; inválidos `24:00`, `9:00`, `12:60`, `18:00\n`, vacío, nulo — valida FR-03.
- [ ] `DarDeAlta_HoraYaVencida_MarcaVencidaAlGenerarse` — a las 10:30 alta "paseo 10:00 solo hoy" → `VencidaAlGenerarse = true` — valida FR-14.
- [ ] `DarDeAlta_HoraFutura_NoMarcaVencidaAlGenerarse` — a las 09:00 alta 18:00 → `false` — valida FR-14.
- [ ] `GenerarDelDia_TrasCaida_MarcaVencidaAlGenerarse` — generación a las 10:00 del deber 08:00 → `true` — valida FR-14.
- [ ] `GenerarDelDia_ALas0005_NoMarcaVencidaAlGenerarse` — valida FR-14.
- [ ] `EmailVencimiento_AceptadoEnUtc_SePersisteConOffsetCero` — guardar y releer: `Offset == TimeSpan.Zero` — decisión transversal.
- [ ] `EmailVencimiento_DosParaLaMismaInstancia_ViolaIndiceUnico` — segundo insert → `DbUpdateException` con 2067 — error documentado.

**Criterio de completitud**
Los 11 tests del bloque y toda la suite de FEAT-001a pasan; `dotnet build petcare.slnx --no-incremental`
sin warnings; `dotnet ef migrations list --project src --no-connect` muestra `Inicial` y
`CumplimientoYEmails`; `grep -rn "ListarAsync" src/features/hogar` sin resultados; `dotnet test`
genera cobertura con `coverage.runsettings` sin pasar `--settings`.

---

## Bloque 2 — Registro de cumplimiento

**Archivos**
- `src/features/instancias/domain/cumplimiento-error.cs` (nuevo) — `enum CumplimientoError { InstanciaNoEncontrada, HoraObligatoria, HoraInvalida, HoraPosteriorALaActual, InstanciaYaCumplida, InstanciaExpirada, RegistroYaEliminado, RegistroDeOtroDia }` + `static class CumplimientoErrores { string Mensaje(CumplimientoError) }`.
- `src/features/instancias/domain/validacion-cumplimiento.cs` (nuevo) — `Resultado<TimeOnly, CumplimientoError> Validar(string? horaReal, DateOnly fechaInstancia, bool tieneRegistro, DateTime ahoraLocal)`. Puro.
- `src/features/instancias/data/registros-service.cs` (nuevo) — `RegistrosService` (scoped): `RegistrarAsync`, `PrepararFormularioAsync` (en este bloque); `EliminarAsync` (Bloque 3).
- `src/features/estado-del-dia/domain/estado-del-dia-vista.cs` (nuevo) — `record EstadoDelDiaVista(EstadoDelDia Estado, string? Mensaje, FormularioCumplimientoVista? Formulario, int RefrescoSegundos)` y `record FormularioCumplimientoVista(int InstanciaId, string HoraPropuesta, string? Error)`.
- `src/features/estado-del-dia/ui/_estado-del-dia.cshtml` (nuevo) — contenedor `<div id="estado-del-dia" data-testid="estado-del-dia">` con el mensaje (`data-testid="mensaje"`) y las secciones (se mueven desde `index.cshtml`, **mismos `data-testid`**); cada instancia vigente con enlace "Cumplido" (`data-testid="cumplido"`) y, si `Formulario.InstanciaId` coincide, el formulario.
- `src/features/estado-del-dia/ui/_formulario-cumplimiento.cshtml` (nuevo) — `<form method="post" action="/?handler=Cumplir" data-formulario-cumplimiento hx-post="/?handler=Cumplir" hx-target="#estado-del-dia" hx-swap="outerHTML" hx-sync="closest #estado-del-dia:replace">` con `instanciaId` oculto, `<input type="time" name="horaReal" value="{HoraPropuesta}">` (`data-testid="hora-real"`), error (`data-testid="error-cumplimiento"`), botón "Confirmar" (`data-testid="confirmar"`) y enlace "Cancelar" (`href="/"`, `hx-get="/?handler=Estado"`, `data-testid="cancelar"`). Token antiforgery por el tag helper.
- `src/features/estado-del-dia/ui/index.cshtml` (modificado) — renderiza `_estado-del-dia` con `Model.Vista`.
- `src/features/estado-del-dia/ui/index.cshtml.cs` (modificado) — handlers `OnGetAsync`, `OnGetEstadoAsync`, `OnGetFormularioCumplimientoAsync`, `OnPostCumplirAsync`; inyecta `EstadoDelDiaService`, `RegistrosService`.
- `src/program.cs` (modificado) — `AddScoped<RegistrosService>()`.
- `src/wwwroot/css/site.css` (modificado) — formulario de cumplimiento a 100% de ancho, mensaje.
- `tests/features/instancias/validacion-cumplimiento-tests.cs`, `tests/features/instancias/registros-service-tests.cs`, `tests/features/estado-del-dia/cumplimiento-page-tests.cs` (nuevos).

**Lógica**
- `Validar` (puro), en este orden: `fechaInstancia < DateOnly.FromDateTime(ahoraLocal)` → `InstanciaExpirada` (FR-07); `tieneRegistro` → `InstanciaYaCumplida` (FR-06); hora nula/espacios → `HoraObligatoria`; `!FormatoHora.TryParse` → `HoraInvalida`; `hoy.ToDateTime(hora) > ahoraLocal` → `HoraPosteriorALaActual` (FR-04; a las 10:30:25 se acepta 10:30 y se rechaza 10:31). Devuelve un solo error (el primero).
- `RegistrarAsync(int instanciaId, string? horaReal, CancellationToken)` → `Resultado<int, CumplimientoError>`: lee la instancia (`Fecha`, si tiene registro); si no existe → `InstanciaNoEncontrada`; `Validar` con `ahoraLocal = ZonaHogar.AhoraLocal(time.GetUtcNow())`; inserta `RegistroCumplimiento { InstanciaId, Fecha = hoy (FR-05), HoraReal }`; `SaveChangesAsync`; si `SqliteErrores.EsViolacionUnica` → `ChangeTracker.Clear()` y `InstanciaYaCumplida` (FR-06 concurrente). Devuelve `Ok(registro.Id)`.
- `PrepararFormularioAsync(int instanciaId, CancellationToken)` → `Resultado<FormularioCumplimientoVista, CumplimientoError>`: instancia inexistente → `InstanciaNoEncontrada`; no vigente → `InstanciaExpirada` / `InstanciaYaCumplida`; si no, `HoraPropuesta = ahoraLocal` en `HH:mm` (FR-03).
- Handlers: arman `EstadoDelDiaVista` con `EstadoDelDiaService.ObtenerAsync` + mensaje/formulario. Con `HX-Request: true` devuelven `Partial("_estado-del-dia", vista)`; sin HTMX, la página completa (o 303 tras un registro exitoso).

**Contrato HTTP**
- `GET /?handler=Estado` — Request: sin parámetros. Response 200: parcial `_estado-del-dia` sin layout. Errores: 500 genérico (manejador global). Auth: ninguna (RA-1).
- `GET /?handler=FormularioCumplimiento&instanciaId={int}` — Response 200: parcial (con `HX-Request`) o página completa, con el formulario abierto en esa instancia y la hora propuesta; si la instancia ya no es vigente, 200 sin formulario y con el mensaje. Errores: 404 si la instancia no existe; 400 si falta `instanciaId` o no es entero. Auth: ninguna.
- `POST /?handler=Cumplir` — Request `application/x-www-form-urlencoded`: `instanciaId` (int), `horaReal` (string HH:MM), `__RequestVerificationToken`; header opcional `HX-Request`. Response con HTMX: 200 parcial actualizado (sin formulario si se registró; con el formulario y su error si la hora es inválida; sin formulario y con el mensaje si ya fue cumplida o expiró). Sin HTMX: éxito → 303 a `/`; rechazo → 200 página completa con el mensaje. Errores: 404 instancia inexistente; 400 sin `instanciaId`, `instanciaId` no entero o antiforgery inválido. Auth: ninguna.

**Validación de entrada**
- `instanciaId`: entero (`int?` bindeado; nulo → 400).
- `horaReal`: `string?`, formato `HH:MM` 24 h, longitud exacta 5 (regex de `FormatoHora`), no posterior a la hora actual; nunca se bindea como `TimeOnly`.
- La salida se codifica con Razor (sin `Html.Raw`).

**Manejo de errores**
- `InstanciaNoEncontrada` → 404.
- `HoraObligatoria` → "La hora es obligatoria." (formulario abierto).
- `HoraInvalida` → "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59." (formulario abierto).
- `HoraPosteriorALaActual` → "La hora no puede ser posterior a la actual." (formulario abierto, FR-04).
- `InstanciaYaCumplida` → "La instancia ya fue cumplida." (FR-06).
- `InstanciaExpirada` → "La instancia expiró." (FR-07).
- Antiforgery → 400 del framework. Otras excepciones de base → no se capturan (500 genérico).

**Tests requeridos**
- [ ] `Validar_HoraValida_Ok` — a las 10:30, `"10:30"` → Ok 10:30 — valida FR-03.
- [ ] `Validar_HoraObligatoria` — nulo y `"  "` — error documentado.
- [ ] `Validar_HoraInvalida` — `"25:00"`, `"9:30"`, `"10:30\n"` — error documentado.
- [ ] `Validar_HoraPosteriorALaActual` — a las 10:30:25 `"10:31"` rechazado, `"10:30"` aceptado — valida AC-08, FR-04.
- [ ] `Validar_InstanciaExpirada` — instancia de ayer — valida FR-07.
- [ ] `Validar_InstanciaYaCumplida` — con registro — valida FR-06.
- [ ] `Registrar_InstanciaVigente_CreaRegistro` — registro con `Fecha` de hoy y la hora real — valida AC-05, FR-02, FR-05.
- [ ] `Registrar_InstanciaInexistente_InstanciaNoEncontrada` — error documentado.
- [ ] `Registrar_InstanciaDeAyer_InstanciaExpirada` — valida AC-10.
- [ ] `Registrar_Concurrente_SegundoRecibeYaCumplida` — un interceptor de EF inserta el registro de otro cuidador entre la lectura y el guardado → `InstanciaYaCumplida`, un solo registro — valida AC-09, FR-06.
- [ ] `PrepararFormulario_PropuestaEsHoraActual` — a las 10:30:40 → `"10:30"` — valida FR-03.
- [ ] `FormularioCumplimiento_A1030_ProponeHoraActual` — `GET` con HTMX → parcial con `hora-real` = `10:30` — valida AC-06.
- [ ] `FormularioCumplimiento_InstanciaInexistente_404` — error documentado.
- [ ] `FormularioCumplimiento_InstanciaCumplida_MuestraMensaje` — 200 sin formulario con "La instancia ya fue cumplida." — error documentado.
- [ ] `FormularioCumplimiento_SinInstanciaId_400` — error documentado.
- [ ] `Cumplir_Htmx_InstanciaDesapareceYSumaPaseo` — la instancia de paseo de Firulais deja de aparecer y "1 paseo" — valida AC-05.
- [ ] `Cumplir_SinHtmx_Redirige303` — valida FR-02.
- [ ] `Cumplir_AceptandoHoraPropuesta_UltimaComida1030` — valida AC-06, FR-05.
- [ ] `Cumplir_Hora1000_UltimaComida1000` — valida AC-07.
- [ ] `Cumplir_Hora1100_RechazaYMantieneInstancia` — mensaje exacto, formulario abierto, instancia visible, 0 registros — valida AC-08.
- [ ] `Cumplir_DosVeces_SegundoMuestraYaCumplida` — segundo POST con el mismo `instanciaId` → mensaje, "1 paseo" — valida AC-09.
- [ ] `Cumplir_TrasMedianoche_MuestraExpiro` — formulario abierto a las 23:59, reloj a 00:01, POST → "La instancia expiró." y 0 registros — valida AC-10.
- [ ] `Cumplir_SoloHoy_DejaDeAparecerEnConfiguracion` — tras cumplir "paseo 09:00 solo hoy", `/mascotas/1/deberes` no lo lista — valida AC-11.
- [ ] `Cumplir_UnicoDeber_MuestraSinTareasYUnPaseo` — a las 10:00, tras cumplir a las 09:05 → "1 paseo", último paseo hoy 09:05, "No hay tareas pendientes." — valida AC-12.
- [ ] `Cumplir_InstanciaInexistente_404` — error documentado.
- [ ] `Cumplir_SinInstanciaId_400` — error documentado.
- [ ] `Cumplir_SinToken_400` — error documentado.
- [ ] `Cumplir_FallaDeBase_500Generico` — Production + interceptor que lanza → 500 sin detalles — error documentado.
- [ ] `Cumplir_HoraConHtml_SeMuestraCodificado` — `horaReal = "<b>"` → `HoraInvalida`, sin `<b>` crudo en la respuesta — validación de salida.
- [ ] `Estado_DevuelveParcialSinLayout` — `GET /?handler=Estado` → contiene `#estado-del-dia`, no contiene `<html` — valida FR-01.
- [ ] `Index_TestIdsDeFeat001a_SeMantienen` — `GET /` sigue exponiendo `seccion-{id}`, `instancia`, `marca-pendiente`, `paseos-hoy`, `ultima-comida`, `ultimo-paseo` dentro de `#estado-del-dia` — regresión.

**Criterio de completitud**
Los 31 tests del bloque y la suite previa pasan; `index.cshtml.cs` no referencia `PetCareDbContext`;
`grep -rn "RegistroCumplimiento" src/features --include=*.cs` solo encuentra escrituras en
`instancias/data`.

---

## Bloque 3 — Registros del día y eliminación

**Archivos**
- `src/features/estado-del-dia/domain/estado-del-dia.cs` (modificado) — `SeccionMascota` + `IReadOnlyList<RegistroVista> RegistrosHoy`; `+ record RegistroVista(int Id, TipoDeber Tipo, TimeOnly HoraReal)`.
- `src/features/estado-del-dia/domain/armado-estado-del-dia.cs` (modificado) — `RegistroLeido` + `int Id` (primer parámetro); `Armar` deduplica por `Id` y arma `RegistrosHoy` (fecha de hoy, por `HoraReal`).
- `src/features/estado-del-dia/domain/textos-estado-del-dia.cs` (modificado) — `Registro(RegistroVista)` → "Paseo · 09:05".
- `src/features/estado-del-dia/data/estado-del-dia-service.cs` (modificado) — consulta de registros acotada.
- `src/features/instancias/data/registros-service.cs` (modificado) — `EliminarAsync`.
- `src/features/estado-del-dia/ui/_estado-del-dia.cshtml` (modificado) — lista "Registrado hoy" (`data-testid="registro"`, `data-registro-id`) con `<form method="post" action="/?handler=EliminarRegistro" hx-post=... hx-target="#estado-del-dia" hx-swap="outerHTML" hx-sync="closest #estado-del-dia:replace">` y botón "Eliminar" (`data-testid="eliminar-registro"`).
- `src/features/estado-del-dia/ui/index.cshtml.cs` (modificado) — `OnPostEliminarRegistroAsync`.
- `src/wwwroot/css/site.css` (modificado) — lista de registros.
- `tests/features/estado-del-dia/armado-estado-del-dia-tests.cs` (modificado) — las construcciones de `RegistroLeido` (líneas 43-48, 66-68, 136) agregan `Id`; tests nuevos.
- `tests/features/estado-del-dia/textos-estado-del-dia-tests.cs`, `tests/features/instancias/registros-service-tests.cs`, `tests/features/estado-del-dia/index-page-tests.cs` (modificados).
- `tests/features/estado-del-dia/estado-del-dia-service-tests.cs`, `tests/features/estado-del-dia/eliminacion-page-tests.cs` (nuevos).

**Lógica**
- Consulta acotada (pendiente de FEAT-001a): dos lecturas con `join` a `Instancias` y `Deberes` — (1) registros con `Fecha == hoy`; (2) el último por (`MascotaId`, `Tipo`): `GroupBy(x => new { x.MascotaId, x.Tipo }).Select(g => g.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.HoraReal).First())` (EF Core 10 SQLite lo traduce con `ROW_NUMBER`; `DateOnly`/`TimeOnly` se ordenan como TEXT ISO). `Armar` recibe la unión y deduplica por `Id`.
- `EliminarAsync(int registroId, CancellationToken)` → `Resultado<int, CumplimientoError>`:
  1. Lee el registro (`Fecha`, `InstanciaId`); no existe → `RegistroYaEliminado` (FR-12; un id que nunca existió da el mismo mensaje).
  2. `Fecha != hoy` → `RegistroDeOtroDia` (FR-10).
  3. En una transacción: `RegistrosCumplimiento.Where(r => r.Id == registroId).ExecuteDeleteAsync()`; si borra 0 filas → rollback y `RegistroYaEliminado`; si no, `Instancias.Where(i => i.Id == instanciaId).ExecuteUpdateAsync(set ReactivadaEnUtc = time.GetUtcNow().ToUniversalTime())` y commit (FR-11). Devuelve `Ok(instanciaId)`.
- La instancia vuelve a ser vigente porque ya no tiene registro; su marca "pendiente" sale de `EsVencida` (AC-16). El deber "solo para hoy" vuelve a la configuración (deber activo).

**Contrato HTTP**
- `POST /?handler=EliminarRegistro` — Request `application/x-www-form-urlencoded`: `registroId` (int), `__RequestVerificationToken`; header opcional `HX-Request`. Response con HTMX: 200 parcial actualizado (con mensaje si fue rechazada). Sin HTMX: éxito → 303 a `/`; rechazo → 200 página completa con el mensaje. Errores: 400 sin `registroId`, no entero o antiforgery inválido. Auth: ninguna (RA-1).

**Validación de entrada**
- `registroId`: entero (`int?`; nulo → 400). La regla "solo registros del día" se valida en el servicio contra la base.

**Manejo de errores**
- `RegistroYaEliminado` → "El registro ya fue eliminado." (200).
- `RegistroDeOtroDia` → "Solo se pueden eliminar registros del día." (200, FR-10).
- Carrera de dos eliminaciones → la segunda borra 0 filas → `RegistroYaEliminado`, sin `DbUpdateConcurrencyException`.
- Antiforgery → 400. Otras excepciones → 500 genérico.

**Tests requeridos**
- [ ] `Armar_RegistrosHoy_OrdenadosPorHora` — valida FR-08.
- [ ] `Armar_UltimoQueEsDeHoy_NoSeDuplica` — el mismo `Id` en las dos listas cuenta una vez para paseos y registros — valida FR-08.
- [ ] `Registro_Texto` — "Paseo · 09:05" — valida FR-08.
- [ ] `EstadoDelDiaService_ConsultaAcotada_TraeHoyYUltimos` — con registros de 3 días: trae los de hoy y el último de comida y paseo por mascota, ninguno más (la consulta se ejecuta en SQLite) — valida FR-08.
- [ ] `Index_ConRegistroDeHoy_MuestraRegistroConEliminar` — tras registrar a las 09:10 un paseo con 09:05: `registro` "Paseo · 09:05" con botón eliminar — valida AC-13.
- [ ] `Index_ConRegistrosDeHoyYAyer_MuestraResumen` — "Última comida: 25/10/2026 13:00", "Último paseo: 24/10/2026 21:00" renderizados (pendiente de FEAT-001a) — valida FR-08.
- [ ] `Eliminar_RegistroDeHoy_BorraYReactiva` — 0 registros, `ReactivadaEnUtc` = ahora con offset cero — valida FR-09, FR-11.
- [ ] `Eliminar_RegistroDeAyer_RegistroDeOtroDia` — el registro sigue — valida AC-15, FR-10.
- [ ] `Eliminar_YaEliminado_RegistroYaEliminado` — dos llamadas seguidas — valida FR-12.
- [ ] `Eliminar_Concurrente_SoloUnoElimina` — un interceptor borra la fila justo antes del `ExecuteDelete` → `RegistroYaEliminado`, sin excepción, sin reactivar — valida AC-17, FR-12.
- [ ] `EliminarRegistro_UnoDeDos_MuestraUnPaseo` — valida AC-14.
- [ ] `EliminarRegistro_ComidaCumplidaPorError_VuelvePendiente` — comida 08:00 registrada a las 08:10, eliminada a las 10:00 → instancia con marca pendiente — valida AC-16, FR-11.
- [ ] `EliminarRegistro_DeAyer_MuestraMensaje` — POST directo con el id de un registro de ayer → mensaje, último paseo sigue ayer 18:00 — valida AC-15.
- [ ] `EliminarRegistro_YaEliminado_MuestraMensaje` — dos POST → "El registro ya fue eliminado.", la instancia vigente una sola vez — valida AC-17.
- [ ] `EliminarRegistro_SinHtmx_Redirige303` — valida FR-09.
- [ ] `EliminarRegistro_SinRegistroId_400` — error documentado.
- [ ] `EliminarRegistro_SinToken_400` — error documentado.
- [ ] `EliminarRegistro_FallaDeBase_500Generico` — error documentado.
- [ ] `EliminarRegistro_DeberSoloHoy_VuelveAConfiguracion` — regresión de `DeberActivo` — valida FR-11.

**Criterio de completitud**
Los 19 tests del bloque y la suite previa pasan; la cobertura de ramas de `textos-estado-del-dia.cs`
supera el 80%.

---

## Bloque 4 — Email de vencimiento

**Archivos**
- `src/features/emails/domain/envio-email.cs` (nuevo) — `interface IEnvioEmail { Task<Resultado<bool, EnvioEmailError>> EnviarAsync(MensajeEmail mensaje, CancellationToken ct); }`, `record MensajeEmail(IReadOnlyList<string> Para, string Asunto, string Cuerpo)`, `enum EnvioEmailError { Rechazado, SinRespuesta }`.
- `src/features/emails/domain/condicion-email.cs` (nuevo) — `bool DebeIntentarse(CandidataEmail c, DateTimeOffset ahoraUtc)`; `record CandidataEmail(int InstanciaId, DateOnly Fecha, TimeOnly Hora, bool TieneRegistro, bool TieneEmailAceptado, bool VencidaAlGenerarse, DateTimeOffset? ReactivadaEnUtc)`. Puro.
- `src/features/emails/domain/mensaje-vencimiento.cs` (nuevo) — `MensajeEmail Armar(string nombreMascota, TipoDeber tipo, DateOnly fecha, TimeOnly hora, IReadOnlyList<string> destinatarios)`. Puro.
- `src/features/emails/domain/opciones-email.cs` (nuevo) — `OpcionesEmail { Modo, Smtp { Host, Puerto = 587, Usuario, Clave, Remitente } }` y `ValidacionOpcionesEmail.Validar(OpcionesEmail, bool esDevelopment)` → lista de errores. Puro.
- `src/features/emails/data/consola-envio-email.cs` (nuevo) — escribe el email en un `TextWriter` (por defecto `Console.Out`) y devuelve `Ok`.
- `src/features/emails/data/smtp-envio-email.cs` (nuevo) — MailKit: `ConnectAsync(host, puerto, puerto == 465 ? SslOnConnect : StartTls)` (M6, nunca `ServerCertificateValidationCallback`), remitente en To y destinatarios en Bcc (M7), `AuthenticateAsync` si hay usuario, `SendAsync`, `DisconnectAsync`; `SmtpClient.Timeout = 30000`; `internal static EnvioEmailError? Clasificar(Exception)`; constructor `internal` para tests con `SecureSocketOptions.None` y timeout corto.
- `src/features/emails/data/emails-vencimiento-service.cs` (nuevo) — `EmailsVencimientoService` (scoped): `Task<ResumenEnvios> ProcesarAsync(CancellationToken)`.
- `src/shared/hosting/vencimientos-worker.cs` (nuevo) — `VencimientosWorker : BackgroundService`, mismo patrón que `GeneracionDiariaWorker`; EventIds 4000–4003.
- `src/program.cs` (modificado) — bind y validación de `Email` **después** de la de `ConnectionStrings` (lanza `InvalidOperationException` con los errores); `IEnvioEmail` según el modo; `AddScoped<EmailsVencimientoService>()`; `AddHostedService<VencimientosWorker>()`.
- `src/appsettings.json` (modificado) — `"Email": { "Modo": "Consola" }`.
- `src/petcare-web.csproj` (modificado) — `MailKit` 4.18.2.
- `tests/infra/envio-email-falso.cs` (nuevo) — `IEnvioEmail` falso: registra cada intento (instante, mensaje) y se programa para aceptar, rechazar hasta un instante o no responder hasta un instante (espera la cancelación).
- `tests/infra/servidor-smtp-falso.cs` (nuevo) — servidor SMTP mínimo con `TcpListener` en loopback: acepta, responde 550 o no responde.
- `tests/infra/petcare-app-factory.cs` (modificado) — quita `VencimientosWorker` con `Single(...)`; reemplaza `IEnvioEmail` por un `EnvioEmailFalso` expuesto como propiedad.
- `tests/features/emails/condicion-email-tests.cs`, `mensaje-vencimiento-tests.cs`, `opciones-email-tests.cs`, `consola-envio-email-tests.cs`, `smtp-envio-email-tests.cs`, `emails-vencimiento-service-tests.cs`, `vencimientos-worker-tests.cs` (nuevos); `tests/shared/arranque-tests.cs` (modificado).

**Lógica**
- `DebeIntentarse` (puro): `false` si `TieneRegistro`, `TieneEmailAceptado` (FR-17), `VencidaAlGenerarse` (FR-14) o `Fecha != ZonaHogar.Hoy(ahoraUtc)`. `vencUtc = ZonaHogar.AUtc(EstadoInstancia.MomentoDeVencimiento(Fecha, Hora))`; `false` si `ahoraUtc < vencUtc`. `desde = max(vencUtc, ReactivadaEnUtc ?? vencUtc)`; `true` si `ahoraUtc < desde + 5 min` (FR-16, NFR-01).
- `MensajeVencimiento.Armar` (FR-15): reemplaza `\r` y `\n` por espacio en el asunto (M5); asunto `"Vencida: {Tipo} de {Mascota} — {dd/MM/yyyy HH:mm}"`; cuerpo con nombre de la mascota, tipo ("comida"/"paseo") y fecha y hora de la instancia; `Para` = todos los cuidadores del hogar (FR-13). Formato con cultura invariante.
- `ProcesarAsync`: trae a memoria las instancias de hoy con su deber, mascota, si tienen registro y si tienen email aceptado (sin filtros de `DateTimeOffset` en SQL); por cada una con `DebeIntentarse(c, time.GetUtcNow())` — reevaluado justo antes de cada intento —: arma el mensaje con los emails de los cuidadores del hogar de la mascota; `using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30), time)` enlazado con `ct`; `EnviarAsync`; `OperationCanceledException` con `timeout` cancelado y `ct` no → `SinRespuesta` (FR-18); aceptado → inserta `EmailVencimiento { InstanciaId, AceptadoEnUtc = time.GetUtcNow().ToUniversalTime() }` (FR-17); fallido → no persiste nada (se reintenta en la próxima pasada si sigue en ventana). Devuelve `ResumenEnvios(Aceptados, Fallidos)` con los ids y errores.
- `VencimientosWorker`: pasada al arrancar y cada 1 min; delega en `ProcesarAsync`; logs sin direcciones ni `ex.Message`.
- `SmtpEnvioEmail.Clasificar`: `SmtpCommandException`, `SmtpProtocolException`, `AuthenticationException`, `SslHandshakeException`, `IOException`/`SocketException` → `Rechazado`; `OperationCanceledException`/`TimeoutException` por el timeout del cliente → `SinRespuesta`; otras → `null` (se propagan).
- Validación de `OpcionesEmail` al arrancar: `Modo` exactamente `Consola` o `Smtp` (sensible a mayúsculas); en Development, `Modo` debe ser `Consola`; con `Smtp`: `Host` no vacío, `Remitente` con formato `x@y`, `Puerto` 1..65535, `Usuario` sin `Clave` es error.

**Modelo de datos**
- Sin cambios (usa `EmailVencimiento` del Bloque 1).

**Validación de entrada**
- No hay entrada de usuario; la configuración (`Email:*`) se valida al arrancar como se describe arriba.

**Manejo de errores**
- `EnvioEmailError.Rechazado` / `SinRespuesta` → no se persiste; `LogWarning` EventId 4002 `EnvioFallido` con `InstanciaId` y el error tipado (sin direcciones); reintento en la próxima pasada si sigue en ventana.
- Choque del índice único al guardar `EmailVencimiento` (2067) → `ChangeTracker.Clear()`, `LogInformation` EventId 4001 con `InstanciaId` (ya registrado); no se reintenta.
- SMTP aceptó y falla el guardado → la excepción llega al worker (4003); posible reenvío en la pasada siguiente (riesgo aceptado).
- Excepción no clasificada en una pasada → el worker la captura (salvo cancelación del host) y registra `LogError` EventId 4003 `PasadaDeVencimientosFallida` con el **tipo** y el `HResult` de la excepción, nunca su mensaje; sigue con el próximo tick.
- Configuración inválida → `InvalidOperationException` al arrancar con la lista de errores (no se muestran claves).
- Cancelación del host → fin normal del loop.

**Tests requeridos**
- [ ] `DebeIntentarse_AntesDeVencer_False` — 09:14 para instancia 09:00 — valida FR-13.
- [ ] `DebeIntentarse_Vencida0915_True` — valida FR-13.
- [ ] `DebeIntentarse_FinDeVentana0920_False` — valida FR-16.
- [ ] `DebeIntentarse_ConRegistro_False` — valida FR-13.
- [ ] `DebeIntentarse_ConEmailAceptado_False` — valida FR-17.
- [ ] `DebeIntentarse_VencidaAlGenerarse_False` — valida FR-14.
- [ ] `DebeIntentarse_InstanciaDeAyer_False` — valida FR-13.
- [ ] `DebeIntentarse_Reactivada1000_VentanaDesde1000` — 10:04 true, 10:05 false — valida AC-22.
- [ ] `DebeIntentarse_ReactivadaAntesDeVencer_VentanaDesdeVencimiento` — reactivada 08:10, ventana 08:15–08:20 — valida FR-16.
- [ ] `Armar_ComidaFirulais0900_IncluyeDatos` — "Firulais", "comida", "25/10/2026 09:00" — valida AC-21, FR-15.
- [ ] `Armar_DestinatariosTodosLosCuidadores` — valida FR-13.
- [ ] `Armar_NombreConSaltoDeLinea_AsuntoSinCrLf` — nombre `"Fi\r\nBcc: x@y"` → asunto sin `\r`/`\n` — mitigación M5.
- [ ] `ValidarOpciones_ConsolaPorDefecto_Ok` — config.
- [ ] `ValidarOpciones_ModoDesconocido_Error` — `"smtp"` y `"Otro"` — error documentado.
- [ ] `ValidarOpciones_DevelopmentConSmtp_Error` — error documentado.
- [ ] `ValidarOpciones_SmtpSinHostORemitente_Error` — error documentado.
- [ ] `ValidarOpciones_PuertoInvalido_Error` — 0 y 70000 — error documentado.
- [ ] `ValidarOpciones_UsuarioSinClave_Error` — error documentado.
- [ ] `ValidarOpciones_SmtpCompletoFueraDeDevelopment_Ok` — config.
- [ ] `Consola_EscribeDestinatariosAsuntoYCuerpo` — en el `TextWriter`; devuelve Ok — valida FR-17 (aceptado).
- [ ] `Smtp_ServidorAcepta_Ok` — servidor falso con 250; el servidor recibe `RCPT TO` de ana y beto y la cabecera `To` solo tiene al remitente (Bcc) — valida FR-17, mitigación M7.
- [ ] `Smtp_ServidorRechaza550_Rechazado` — valida FR-16.
- [ ] `Smtp_SinRespuesta_SinRespuesta` — servidor que no contesta, timeout corto del constructor interno — valida AC-29, FR-18.
- [ ] `Clasificar_ExcepcionesMailKit` — cada excepción listada → su error; `InvalidOperationException` → `null` — error documentado.
- [ ] `Procesar_0915SinCumplir_EnviaAAnaYBeto` — a las 09:15:30 un intento aceptado dirigido a `ana@ejemplo.com` y `beto@ejemplo.com` y un `EmailVencimiento` — valida AC-18, FR-13.
- [ ] `Procesar_Cumplida0905_NoEnvia` — valida AC-19.
- [ ] `Procesar_TodoElDia_ExactamenteUnEmail` — pasadas cada minuto de 09:00 a 23:59 → 1 email — valida AC-20.
- [ ] `Procesar_EliminarRegistroA1000_EnviaAntesDe1005` — registro 08:10, `EliminarAsync` a las 10:00 → email aceptado a las 10:01 — valida AC-22.
- [ ] `Procesar_EmailYaAceptado_NoReenviaTrasEliminar` — email a las 08:15, registro 09:00 eliminado 10:00 → ningún intento más — valida AC-23.
- [ ] `Procesar_AltaConHoraVencida_NoEnvia` — alta 10:30 de "paseo 10:00 solo hoy" vía `DeberesService`, pasadas hasta 23:59 → 0 intentos — valida AC-24, FR-14.
- [ ] `Procesar_GeneradaTrasCaida_NoEnvia` — generación a las 10:00 del deber 08:00 → 0 intentos — valida AC-25, FR-14.
- [ ] `Procesar_RechazaHasta0917_AceptadoAntesDe0920` — valida AC-26, FR-16.
- [ ] `Procesar_RechazaHasta0930_SinIntentosDespuesDe0920` — ningún intento con instante ≥ 09:20 — valida AC-27, FR-16.
- [ ] `Procesar_VentanaPerdidaYReactivada1030_EnviaAntesDe1035` — valida AC-28, FR-17.
- [ ] `Procesar_SinRespuesta_FallaA30sYAceptaAntesDe0920` — el falso no responde hasta 09:17; cada intento falla con `SinRespuesta` al avanzar 30 s; aceptado ≤ 09:20 — valida AC-29, FR-18.
- [ ] `Procesar_ChoqueAlGuardarEmail_ErrorTipadoSinReintento` — interceptor inserta el `EmailVencimiento` antes del guardado → sin excepción, un solo registro — error documentado.
- [ ] `Procesar_ExcepcionNoClasificada_Propaga` — el falso lanza `InvalidOperationException` — error documentado.
- [ ] `VencimientosWorker_SinAppAbierta_EnviaAntesDe0920` — worker con `FakeTimeProvider` desde 09:00, avanzando de a 1 min, sin ninguna request → aceptado entre 09:15 y 09:20 — valida AC-18, NFR-01.
- [ ] `VencimientosWorker_EnvioFallido_RegistraWarningSinDirecciones` — EventId 4002, el texto del log no contiene `@` — error documentado, M3.
- [ ] `VencimientosWorker_PasadaQueLanza_RegistraTipoYSigue` — EventId 4003 con el tipo, sin el mensaje de la excepción; la pasada siguiente corre — error documentado.
- [ ] `VencimientosWorker_Cancelacion_TerminaSinError` — error documentado.
- [ ] `Arranque_DevelopmentConSmtp_Falla` — `Email:Modo=Smtp` en Development → `InvalidOperationException` — error documentado.
- [ ] `Arranque_SmtpSinHost_Falla` — Production, `Email:Modo=Smtp` sin host — error documentado.
- [ ] `Arranque_AppSettingsSinCredencialesSmtp` — ningún `appsettings*.json` ni `launchSettings.json` define `Email:Smtp:Usuario`, `Clave` ni `Host` — mitigación.

**Criterio de completitud**
Los 44 tests del bloque y la suite previa pasan; `grep -rn ServerCertificateValidationCallback src/` sin resultados (M6); `dotnet list petcare.slnx package --vulnerable
--include-transitive` sin vulnerables; con `dotnet run --project src` y un deber vencido, la consola
muestra el email (destinatarios, asunto, cuerpo) y el log solo el EventId 4001 con el `InstanciaId`.

---

## Bloque 5 — Actualización automática del estado del día

**Archivos**
- `src/features/estado-del-dia/ui/opciones-refresco.cs` (nuevo) — `OpcionesRefresco { RefrescoSegundos = 10 }` y validación pura 1..15.
- `src/features/estado-del-dia/ui/_estado-del-dia.cshtml` (modificado) — atributos del contenedor: `hx-get="/?handler=Estado" hx-trigger="every {N}s [!document.querySelector('[data-formulario-cumplimiento]')]" hx-swap="outerHTML" hx-sync="this:replace"`.
- `src/features/estado-del-dia/ui/index.cshtml.cs` (modificado) — inyecta `IOptions<OpcionesRefresco>` y pasa `RefrescoSegundos` a la vista.
- `src/program.cs` (modificado) — bind de `EstadoDelDia` y validación al arrancar (fuera de rango → `InvalidOperationException`).
- `src/appsettings.json` (modificado) — `"EstadoDelDia": { "RefrescoSegundos": 10 }`.
- `src/wwwroot/css/site.css` (modificado) — ajustes a 360 px de formulario y registros.
- `tests/infra/petcare-app-factory.cs` (modificado) — `EstadoDelDia:RefrescoSegundos = 1`.
- `tests/infra/playwright-fixture.cs` (modificado) — expone el `FakeTimeProvider` de la fábrica y permite abrir varios contextos de navegador.
- `tests/features/estado-del-dia/opciones-refresco-tests.cs`, `tests/features/estado-del-dia/refresco-navegador-tests.cs` (nuevos; `[Trait("Categoria", "Navegador")]`); `tests/features/estado-del-dia/sin-scroll-horizontal-tests.cs`, `tests/features/estado-del-dia/index-page-tests.cs`, `tests/shared/arranque-tests.cs` (modificados).

**Lógica**
- El contenedor se reemplaza entero cada N s con el estado actual (FR-01): cambios de otros cuidadores (alta, registro, eliminación), instancias que pasan a "pendiente" y que expiran a medianoche.
- El polling se pausa mientras hay un formulario de cumplimiento abierto (filtro del trigger); "Cancelar" o "Confirmar" lo cierran y el polling sigue. `hx-sync` hace que la acción del usuario reemplace a un poll en vuelo. Un mensaje de rechazo se ve hasta el poll siguiente (≤ N s) — decisión de diseño.
- El filtro usa `htmx.config.allowEval` (true por defecto); si en el futuro se agrega una CSP, hay que revisarlo.

**Validación de entrada**
- `EstadoDelDia:RefrescoSegundos`: entero 1..15 (NFR-02); fuera de rango la app no arranca.

**Manejo de errores**
- Configuración fuera de rango → `InvalidOperationException` al arrancar.
- Falla en `GET /?handler=Estado` → 500 genérico; HTMX no reemplaza el contenido y reintenta en el siguiente tick.

**Tests requeridos**
- [ ] `OpcionesRefresco_FueraDeRango_Error` — 0 y 16 — error documentado.
- [ ] `OpcionesRefresco_Diez_Ok` — valida NFR-02.
- [ ] `Arranque_RefrescoInvalido_Falla` — error documentado.
- [ ] `Estado_FallaDeBase_500Generico` — Production + interceptor que lanza → `GET /?handler=Estado` devuelve 500 sin detalles — error documentado.
- [ ] `Index_ContenedorConPollingPausableYSync` — el HTML de `#estado-del-dia` tiene `hx-trigger` con `every 1s` y el filtro de formulario, y `hx-sync` — valida FR-01.
- [ ] `Refresco_OtroCuidadorRegistra_DesapareceYSumaPaseo` — dos contextos; B registra un paseo de Firulais por la UI; en A, dentro de los 15 s y sin recargar, la instancia desaparece y suma 1 paseo — valida AC-01, NFR-02.
- [ ] `Refresco_SinInteraccion_MarcaPendienteA1215` — reloj 12:14 con "paseo 12:00", luego 12:15 → marca visible en ≤ 15 s — valida AC-02.
- [ ] `Refresco_OtroCuidadorDaDeAlta_ApareceInstancia` — valida AC-03.
- [ ] `Refresco_Medianoche_DesapareceInstancia` — reloj 23:59 → 00:00 → la instancia de las 20:00 desaparece en ≤ 15 s — valida AC-04.
- [ ] `Refresco_FormularioAbierto_NoSeCierraPorPolling` — con el formulario abierto, 3 s después sigue abierto y conserva la hora editada — valida FR-01.
- [ ] `Cancelar_CierraFormularioYReanudaRefresco` — valida FR-01.
- [ ] `RegistrarCumplimiento_HoraPorDefecto_DosClics` — "Cumplido" + "Confirmar" (2 clics contados) → la instancia desaparece — valida AC-31, NFR-03.
- [ ] `SinScrollHorizontal_360px_RegistrarCumplimientoEditandoHora` — formulario abierto, con error de hora y confirmado — valida AC-30, NFR-04.
- [ ] `SinScrollHorizontal_360px_EliminarRegistro` — lista de registros con nombre de 50 caracteres y eliminación — valida AC-30, NFR-04.

**Criterio de completitud**
Los 14 tests del bloque y toda la suite pasan (con y sin `--filter "Categoria!=Navegador"`); con la
app levantada, dos pestañas ven los cambios de la otra en ≤ 15 s.

---

## Verificación final

- `dotnet build petcare.slnx --no-incremental` sin warnings.
- `dotnet test petcare.slnx`: los 119 tests nuevos de los 5 bloques y los de FEAT-001a pasan; con `--filter "Categoria!=Navegador"` pasan todos los demás.
- Cobertura con `tests/coverage.runsettings`: líneas, ramas y funciones ≥ 80% medidas por la herramienta, sin ajuste manual.
- Cada AC-01…AC-31 tiene al menos un test que pasa (tabla "Cobertura").
- `grep -rE "DateTime(Offset)?\.(Now|UtcNow)|TimeZoneInfo\.Local|EnsureCreated|\.Migrate\(|FromSql|ExecuteSqlRaw|SqlQueryRaw|Html\.Raw" src/` sin resultados.
- Ningún `Log*` de `src/` recibe direcciones de email ni `ex.Message`; ningún `appsettings*.json` ni `launchSettings.json` tiene `Email:Smtp:*`.
- Ningún PageModel referencia `PetCareDbContext`; las escrituras de `RegistroCumplimiento` e `Instancia` están solo en `instancias/data` (más la excepción declarada de `DeberesService`).
- `dotnet list petcare.slnx package --vulnerable --include-transitive` sin vulnerables.

## Rollback

Sin datos productivos (RA-1): revertir los commits de la rama. La migración se deshace con
`dotnet ef database update Inicial --project src` (las columnas `VencidaAlGenerarse` y
`ReactivadaEnUtc` y la tabla `EmailsVencimiento` se eliminan; los registros de cumplimiento se
conservan porque su tabla viene de `Inicial`).
