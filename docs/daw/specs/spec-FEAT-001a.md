# Spec FEAT-001a: Deberes de comida y paseo: alta, instancias y estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| PRD | docs/daw/prd/prd-FEAT-001a.md |
| Tier | FEATURE |
| Fecha | 2026-10-25 |
| Spec loops | 2 |

## Resumen

Aplicación ASP.NET Core Razor Pages (.NET 10) + HTMX, con SQLite y EF Core 10. Un único proyecto
web (`src/`) organizado en features (`hogar`, `deberes`, `instancias`, `estado-del-dia`) y un
proyecto de tests xUnit (`tests/`). El esquema y los datos precargados llegan por una migración
`Inicial`. Las reglas de dominio (validación del alta, vencimiento, qué instancias le tocan a un día,
armado del estado del día, conversión a UTC−3) son funciones puras en `domain/`; los servicios en
`data/` leen y escriben la base y las invocan; las páginas Razor solo hablan con servicios. Un
`BackgroundService` sin lógica propia genera las instancias del día al arrancar y cada 1 minuto. El
vencimiento ("pendiente") y la expiración se calculan al leer, contra la hora del `TimeProvider`.

## Decisiones transversales

- **Hora:** toda hora sale de `TimeProvider` inyectado (registrado como `TimeProvider.System`; en
  tests, `FakeTimeProvider`). Prohibido `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`,
  `DateTimeOffset.UtcNow`, `TimeZoneInfo.Local` y defaults de base como
  `HasDefaultValueSql("CURRENT_TIMESTAMP")`. Las funciones puras reciben el instante
  (`DateTimeOffset`) o la hora local (`DateTime`) como parámetro; solo los servicios y el worker
  llaman a `GetUtcNow()`.
- **Zona horaria (NFR-03):** desfase fijo de −3 h respecto de UTC, sin horario de verano, en
  `src/shared/tiempo/zona-hogar.cs`. Fechas y horas de dominio se guardan como hora local UTC−3
  (`DateOnly` + `TimeOnly`).
- **Capas:** `domain/` contiene entidades, enums, records y funciones puras, sin I/O. `data/`
  contiene configuraciones de EF y los servicios que hacen I/O. `ui/` contiene las páginas; sus
  PageModels inyectan **solo servicios**, nunca `PetCareDbContext`.
- **Lecturas entre features:** `estado-del-dia-service` y `deberes-service` consultan el
  `PetCareDbContext` compartido directamente (tablas de otras features), sin pasar por los
  servicios de esas features. Decisión pragmática para una app de un solo hogar; las escrituras
  sobre una entidad las hace solo el servicio de su feature.
- **Errores tipados:** las operaciones que pueden fallar por reglas de negocio devuelven
  `Resultado<T, TError>` (`src/shared/resultados/resultado.cs`) con un error de tipo enum por
  feature. Nada de `catch` silencioso: toda excepción capturada se transforma en error tipado o se
  registra en el log con su `EventId`.
- **Esquema:** solo por migraciones (`dotnet ef migrations add <Nombre> --project src --output-dir
  migrations`). La app **no** llama a `Migrate()` ni `EnsureCreated()` al arrancar; la base de
  desarrollo se crea con `dotnet ef database update --project src`. Los tests llaman a `Migrate()`
  sobre SQLite en memoria (solo en tests).
- **Sin control de acceso:** riesgo aceptado en el PRD (solo desarrollo local). Ningún endpoint
  requiere autenticación en este ticket.
- **Antiforgery:** los POST validan el token antiforgery de Razor Pages (comportamiento por
  defecto). Los formularios HTMX lo envían como campo oculto generado por el tag helper `<form>`.
- **Seguridad (threat model `docs/daw/security/threat-FEAT-001a.md`):**
  - M1: la app escucha solo en `localhost` (`launchSettings.json` con `http://localhost:5080`); ninguna configuración enlaza `0.0.0.0`, `*` ni `+`.
  - M2: prohibido SQL crudo en `src/` (`FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated`, `SqlQueryRaw`).
  - M3: ningún log incluye emails de cuidadores.
  - M4: versiones exactas de paquetes y herramientas (sin rangos flotantes); HTMX con versión y SHA-256 anotados en `src/wwwroot/lib/htmx/VERSION.txt`.
- **Rutas:** todas las páginas declaran ruta absoluta en `@page` (`"/"`,
  `"/mascotas/{mascotaId:int}/deberes"`), porque `RootDirectory = "/features"` derivaría rutas con
  `/ui/`.

### Excepciones a la estructura `src/features/<feature>/{ui,domain,data}` y a kebab-case

| Ruta / nombre | Motivo |
|---|---|
| `src/program.cs`, `src/appsettings*.json`, `src/Properties/launchSettings.json`, `src/wwwroot/` | Impuestos por ASP.NET Core |
| `src/shared/` (data, tiempo, resultados, hosting) | Transversal a todas las features |
| `src/migrations/` y sus archivos generados (`{timestamp}_Inicial.cs`, `{timestamp}_Inicial.Designer.cs`, `PetCareDbContextModelSnapshot.cs`) | Generados por `dotnet ef`; **no se renombran a mano** |
| `src/features/_ViewImports.cshtml`, `src/features/_ViewStart.cshtml`, `src/features/Shared/_Layout.cshtml` | Nombres y carpeta que busca Razor |
| `.config/dotnet-tools.json` | Manifiesto de herramientas locales de .NET |

### Dependencias nuevas

| Paquete / herramienta | Proyecto | Para qué | Alternativa descartada |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` 10.x | src | Proveedor SQLite (en el stack) | — |
| `Microsoft.EntityFrameworkCore.Design` 10.x | src | Requerido por `dotnet ef` para generar migraciones (regla del proyecto) | Escribir migraciones a mano: frágil |
| `dotnet-ef` 10.x (herramienta local) | repo | Generar y aplicar migraciones con versión fijada en el repo | Herramienta global: versión no reproducible |
| HTMX 2.0.x (`htmx.min.js` vendorizado en `wwwroot/lib/htmx/`) | src | Interacciones sin recarga (en el stack) | CDN: dependencia de red en desarrollo |
| `xunit` + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` | tests | Runner de tests (en el stack) | — |
| `Microsoft.AspNetCore.Mvc.Testing` 10.x | tests | `WebApplicationFactory` para tests HTTP de las páginas | Levantar el servidor a mano: más código, menos aislamiento |
| `Microsoft.Extensions.TimeProvider.Testing` 10.x | tests | `FakeTimeProvider` para mover el reloj (vencimientos, cambio de día) | Fake propio: reinventar lo que Microsoft ya mantiene |
| `AngleSharp` 1.x | tests | Parsear el HTML y consultar por `data-testid` en vez de comparar strings | Regex sobre HTML: tests frágiles |
| `Microsoft.Playwright` 1.x | tests | AC-23: medir scroll horizontal en un navegador real a 360 px (decisión del usuario en PLAN) | Checklist manual: no automatizable |

## Cobertura: PRD → bloques

| Requerimiento | Cubierto por |
|---|---|
| FR-01 | Bloque 1 |
| FR-02 | Bloque 2 |
| FR-03 | Bloque 2 |
| FR-04 | Bloque 2 (alta), Bloque 3 (generación según repetición) |
| FR-05 | Bloque 2 |
| FR-06 | Bloque 2 |
| FR-07 | Bloque 2 |
| FR-08 | Bloque 3 |
| FR-09 | Bloque 3 |
| FR-10 | Bloque 3 (cálculo), Bloque 4 (marca en pantalla) |
| FR-11 | Bloque 3 (cálculo), Bloque 2 y Bloque 4 (efecto en las vistas) |
| FR-12 | Bloque 4 |
| FR-13 | Bloque 4 |
| FR-14 | Bloque 4 |
| FR-15 | Bloque 4 |
| FR-16 | Bloque 4 |
| FR-17 | Bloque 4 |
| NFR-01 | Estrategia: el worker (Bloque 3) ejecuta una pasada al arrancar y luego cada 1 minuto con `PeriodicTimer(TimeSpan.FromMinutes(1), TimeProvider)`; la primera pasada posterior a las 00:00 (UTC−3) genera las instancias del nuevo día, a más tardar ~1 min después del inicio del día (< 5 min). Verificado con `FakeTimeProvider`. |
| NFR-02 | Estrategia: CSS propio mobile-first (Bloque 4), sin anchos fijos mayores a 360 px, `box-sizing: border-box`, `overflow-wrap: anywhere` en textos y formularios a 100% de ancho; `<meta name="viewport" content="width=device-width, initial-scale=1">`. Verificado con Playwright a 360 px. |
| NFR-03 | Estrategia: `ZonaHogar` (Bloque 1) con desfase fijo −3 h; toda conversión UTC→local pasa por ahí. Verificado con tests de borde (02:59 UTC, 03:05 UTC, 08:15 UTC). |

| Criterio | Test(s) |
|---|---|
| AC-01 | B4 `Index_PrimeraVez_MuestraDosSeccionesVacias`; B1 `Seed_CreaHogarCuidadoresYMascotas` |
| AC-02 | B2 `DarDeAlta_PaseoTodosLosDias_ApareceEnActivos`; B2 `PostAlta_Htmx_DevuelveParcialConDeber` |
| AC-03 | B2 `DarDeAlta_ParaFirulais_NoApareceEnMichi`; B4 `Index_DeberDeFirulais_NoApareceEnSeccionDeMichi` |
| AC-04 | B2 `Validar_SinHora_HoraObligatoria`; B2 `PostAlta_SinHora_MuestraMensajeYNoCrea` |
| AC-05 | B2 `Validar_Hora2500_HoraInvalida`; B2 `PostAlta_Hora2500_MuestraMensajeYNoCrea` |
| AC-06 | B2 `Validar_TipoMedicacionOVacio_TipoInvalido`; B2 `PostAlta_TipoMedicacion_MuestraMensajeYNoCrea` |
| AC-07 | B2 `Validar_SinRepeticion_RepeticionObligatoria`; B2 `PostAlta_SinRepeticion_MuestraMensajeYNoCrea` |
| AC-08 | B2 `ListarActivos_SoloHoyDeAyerNoCumplido_NoAparece` |
| AC-09 | B2 `DarDeAlta_HoraYaPasada_GeneraInstanciaDeHoy`; B4 `Index_AltaConHoraPasada_MuestraPendiente` |
| AC-10 | B3 `QueFaltan_TodosLosDias_IncluyeManana`; B4 `Index_Manana0800_MuestraInstanciaDeTodosLosDias` |
| AC-11 | B3 `QueFaltan_SoloHoy_NoGeneraManana`; B4 `Index_Manana0800_NoMuestraSoloHoyDeAyer` |
| AC-12 | B3 `GenerarDelDia_0005_CreaInstanciaDelNuevoDia`; B3 `Worker_CruzaMedianoche_GeneraEnMenosDe5Minutos`; B4 `Index_0005_MuestraInstanciaDelNuevoDia` |
| AC-13 | B1 `Hoy_0259UtcDelDiaSiguiente_EsElDiaAnterior`; B3 `GenerarDelDia_2359_NoCreaInstanciaDeManana` |
| AC-14 | B3 `Worker_ArrancaA1000TrasCaida_GeneraInstanciaYQuedaPendiente` |
| AC-15 | B3 `GenerarDelDia_DosVeces_UnaSolaInstancia`; B3 `Worker_Reinicio_NoDuplica` |
| AC-16 | B3 `EsVencida_0910_False`; B4 `Index_0910_SinMarcaPendiente` |
| AC-17 | B3 `EsVencida_0915_True`; B4 `Index_0915_MarcaPendiente` |
| AC-18 | B3 `EsVigente_InstanciaDeAyer_False`; B4 `Index_0700_NoMuestraAyerYMuestraHoySinMarca` |
| AC-19 | B2 `ListarActivos_TodosLosDiasConAyerExpirada_SigueApareciendo` |
| AC-20 | B4 `Armar_ComidaYPaseoA1200_ComidaPendientePaseoSinMarca`; B4 `Index_1200_ComidaPendientePaseoSinMarca` |
| AC-21 | B4 `Armar_DosPaseosYComidaHoy_ResumenCorrecto` |
| AC-22 | B4 `Armar_RegistrosDeAyer_MuestraFechasDeAyerYCeroPaseos` |
| AC-23 | B4 `SinScrollHorizontal_360px_EstadoDelDia`, `SinScrollHorizontal_360px_ConfiguracionDeDeberes`, `SinScrollHorizontal_360px_AltaDeDeberConErrores` |
| AC-24 | B1 `AhoraLocal_0815Utc_Es0515`; B3 `EsVencida_0515Para0800_False`; B4 `Index_0515Local_NoMarcaPendiente` |

## Dependencias entre bloques

- Bloque 1: ninguna. Va primero.
- Bloque 2 depende de 1 (entidades, DbContext, `ZonaHogar`, `Resultado`, infraestructura de tests).
- Bloque 3 depende de 1; usa el `GeneracionLock` de 1. Sus tests de servicio crean deberes
  directamente en la base, así que no depende de 2 para compilar; el alta del Bloque 2 también toma
  el `GeneracionLock`.
- Bloque 4 depende de 1, 2 y 3 (usa `EstadoInstancia` del 3 y el alta del 2 en sus tests de página).
- Orden de ejecución: 1 → 2 → 3 → 4.

---

## Bloque 1 — Esqueleto, esquema y datos precargados

**Archivos**
- `.gitignore` (modificado) — agregar `bin/`, `obj/`, `*.db`, `*.db-shm`, `*.db-wal`, `*.db-journal`, `TestResults/`, `.vs/`, `*.user`, `__pycache__/`, `.playwright/`, sin tocar el bloque DAW.
- `petcare.slnx` (nuevo) — solución con `src/petcare-web.csproj` y `tests/petcare-tests.csproj`.
- `.config/dotnet-tools.json` (nuevo) — `dotnet-ef` 10.x.
- `src/petcare-web.csproj` (nuevo) — `net10.0`, `Nullable` y `ImplicitUsings` habilitados, `TreatWarningsAsErrors=true`, `RootNamespace=PetCare`, `AssemblyName=PetCare.Web`; paquetes EF Core Sqlite y Design.
- `src/program.cs` (nuevo) — host: `AddRazorPages(o => o.RootDirectory = "/features")`; `AddDbContext<PetCareDbContext>` con `ConnectionStrings:PetCare` (falla al arrancar si falta); `AddSingleton(TimeProvider.System)`; `AddSingleton<GeneracionLock>()`; registro de servicios (bloques 2–4); `AddHostedService<GeneracionDiariaWorker>()` (bloque 3); `UseExceptionHandler("/error")` fuera de Development; `UseStaticFiles`; `MapRazorPages`. Termina con `public partial class Program;` para `WebApplicationFactory`.
- `src/appsettings.json` (nuevo) — `ConnectionStrings:PetCare = "Data Source=petcare.db"`, logging. Sin secretos.
- `src/appsettings.Development.json` (nuevo) — logging de desarrollo.
- `src/Properties/launchSettings.json` (nuevo) — perfil `http` con `applicationUrl: "http://localhost:5080"` y `ASPNETCORE_ENVIRONMENT=Development` (M1).
- `src/shared/data/petcare-db-context.cs` (nuevo) — `PetCareDbContext` con `DbSet` de las 6 entidades y `ApplyConfigurationsFromAssembly`.
- `src/shared/tiempo/zona-hogar.cs` (nuevo) — `static class ZonaHogar`: `Desfase = TimeSpan.FromHours(-3)`; `DateTime AhoraLocal(DateTimeOffset instanteUtc)`; `DateOnly Hoy(DateTimeOffset instanteUtc)`. Puro.
- `src/shared/resultados/resultado.cs` (nuevo) — `readonly record struct Resultado<T, TError>` con `Ok(T)` / `Falla(IReadOnlyList<TError>)`, `EsOk`, `Valor`, `Errores`.
- `src/shared/hosting/generacion-lock.cs` (nuevo) — singleton que envuelve un `SemaphoreSlim(1,1)`; lo usan el alta (B2) y la generación diaria (B3).
- `src/features/Shared/error.cshtml` + `error.cshtml.cs` (nuevos) — `@page "/error"`: mensaje genérico "Ocurrió un error inesperado.", sin detalles. Requiere `_ViewImports`/`_ViewStart`/`_Layout`, que se crean en este bloque (ver abajo) aunque el layout se completa en el Bloque 4.
- `src/features/_ViewImports.cshtml`, `src/features/_ViewStart.cshtml`, `src/features/Shared/_Layout.cshtml` (nuevos) — versión mínima; el Bloque 4 completa el layout.
- `src/features/hogar/domain/hogar.cs`, `cuidador.cs`, `mascota.cs` (nuevos) — entidades.
- `src/features/deberes/domain/deber.cs`, `tipo-deber.cs`, `repeticion.cs` (nuevos) — entidad y enums.
- `src/features/instancias/domain/instancia.cs`, `registro-cumplimiento.cs` (nuevos) — entidades.
- `src/features/hogar/data/hogar-configuration.cs`, `cuidador-configuration.cs`, `mascota-configuration.cs` (nuevos) — mapeo + `HasData` (FR-01).
- `src/features/deberes/data/deber-configuration.cs` (nuevo).
- `src/features/instancias/data/instancia-configuration.cs`, `registro-cumplimiento-configuration.cs` (nuevos).
- `src/migrations/{timestamp}_Inicial.cs`, `src/migrations/{timestamp}_Inicial.Designer.cs`, `src/migrations/PetCareDbContextModelSnapshot.cs` (nuevos, generados con `dotnet ef migrations add Inicial --project src --output-dir migrations`).
- `tests/petcare-tests.csproj` (nuevo) — `net10.0`, `ProjectReference` a `src/petcare-web.csproj`; paquetes de test de la tabla de dependencias.
- `tests/infra/petcare-app-factory.cs` (nuevo) — `WebApplicationFactory<Program>`: reemplaza el `DbContext` por SQLite en memoria (`Data Source=:memory:`, conexión abierta durante la vida de la fábrica) y llama a `Migrate()`; reemplaza `TimeProvider` por un `FakeTimeProvider` expuesto como propiedad; quita el `GeneracionDiariaWorker` de los hosted services (los tests del worker lo instancian aparte).
- `tests/infra/base-de-test.cs` (nuevo) — helper para crear un `PetCareDbContext` sobre SQLite en memoria con `Migrate()`, y helpers para insertar deberes, instancias y registros como datos de partida.
- `tests/features/hogar/datos-precargados-tests.cs` (nuevo).
- `tests/shared/zona-hogar-tests.cs` (nuevo).
- `tests/shared/arranque-tests.cs` (nuevo).

**Contrato HTTP**
- `GET /error` (y re-ejecución interna de `UseExceptionHandler`)
  - Request: sin parámetros.
  - Response: HTML con "Ocurrió un error inesperado."; status 500 cuando lo invoca el manejador de excepciones, 200 si se pide directamente.
  - Errores: ninguno propio.
  - Auth: ninguna (riesgo aceptado del PRD).

**Lógica**
Proyecto, host y esquema completo de este ticket en una sola migración `Inicial`, con los datos
precargados por `HasData`. `ZonaHogar` es la única conversión UTC → UTC−3.

**Modelo de datos**
- `Hogar`: `Id` int PK (identidad); `Nombre` TEXT(100) not null. Seed: `{Id=1, Nombre="Hogar"}`.
- `Cuidador`: `Id` int PK; `HogarId` int FK → `Hogar.Id` not null, `OnDelete Restrict`; `Email` TEXT(254) not null, índice único con collation `NOCASE`. Seed: `{1, 1, "ana@ejemplo.com"}`, `{2, 1, "beto@ejemplo.com"}`.
- `Mascota`: `Id` int PK; `HogarId` int FK → `Hogar.Id` not null, `Restrict`; `Nombre` TEXT(50) not null; índice único (`HogarId`, `Nombre`) con `NOCASE`. Seed: `{1, 1, "Firulais"}`, `{2, 1, "Michi"}`.
- `Deber`: `Id` int PK; `MascotaId` int FK → `Mascota.Id` not null, `Restrict`, indexado; `Tipo` int not null (`TipoDeber`: `Comida=1`, `Paseo=2`) con check constraint `Tipo IN (1,2)`; `Hora` TEXT (`TimeOnly`, HH:mm:ss) not null; `Repeticion` int not null (`Repeticion`: `SoloHoy=1`, `TodosLosDias=2`) con check `Repeticion IN (1,2)`; `FechaAlta` TEXT (`DateOnly`) not null. Sin seed.
- `Instancia`: `Id` int PK; `DeberId` int FK → `Deber.Id` not null, `Restrict`; `Fecha` TEXT (`DateOnly`) not null; `Hora` TEXT (`TimeOnly`) not null (copia de `Deber.Hora` al generarse); índice único (`DeberId`, `Fecha`); índice en `Fecha`.
- `RegistroCumplimiento`: `Id` int PK; `InstanciaId` int FK → `Instancia.Id` not null, `Restrict`, índice único; `Fecha` TEXT (`DateOnly`) not null, indexado; `HoraReal` TEXT (`TimeOnly`) not null. En este ticket no hay UI que lo cree.
- Ninguna columna tiene default calculado por la base.

**Validación de entrada**
- No aplica: este bloque no recibe entrada de usuario.

**Manejo de errores**
- Falta `ConnectionStrings:PetCare` → `program.cs` lanza `InvalidOperationException("Falta ConnectionStrings:PetCare en la configuración.")` al arrancar; el host no levanta.
- Excepción no manejada en una request fuera de Development → `UseExceptionHandler("/error")` devuelve 500 con la página genérica, sin stack trace ni detalles internos.
- Base no creada (no se corrió `dotnet ef database update`) → `SqliteException` en la primera consulta, que cae en el caso anterior (500 genérico) y queda en el log; la solución documentada es correr el comando, no crear la base al arrancar.

**Tests requeridos**
- [ ] `Seed_CreaHogarCuidadoresYMascotas` — tras `Migrate()` existen exactamente 1 hogar, los cuidadores `ana@ejemplo.com` y `beto@ejemplo.com`, las mascotas Firulais y Michi y 0 deberes — valida FR-01, AC-01.
- [ ] `Hoy_0305Utc_EsElNuevoDia` — 03:05 UTC del día D+1 → `Hoy` = D+1 — valida NFR-03, AC-12.
- [ ] `Hoy_0259UtcDelDiaSiguiente_EsElDiaAnterior` — 02:59 UTC de D+1 → `Hoy` = D — valida NFR-03, AC-13.
- [ ] `AhoraLocal_0815Utc_Es0515` — valida NFR-03, AC-24.
- [ ] `Arranque_LaunchSettings_SoloLocalhost` — todas las `applicationUrl` de `launchSettings.json` son `localhost` y ningún `appsettings*.json` define `Kestrel:Endpoints` con `0.0.0.0`, `*` o `+` — mitigación M1.
- [ ] `Arranque_SinCadenaDeConexion_Falla` — sin `ConnectionStrings:PetCare` el host lanza `InvalidOperationException` con el mensaje documentado — error documentado.
- [ ] `Error_ExcepcionEnProduccion_Devuelve500Generico` — con entorno Production y un `IStartupFilter` de test que lanza en `/_test/lanza`, la respuesta es 500 con "Ocurrió un error inesperado." y sin el texto de la excepción — error documentado.
- [ ] `Error_BaseSinMigrar_Devuelve500Generico` — en Production con una base SQLite vacía (sin migrar) y un `IStartupFilter` de test que consulta `Mascotas` en `/_test/consulta`, la respuesta es 500 genérico — error documentado.

**Criterio de completitud**
`dotnet build petcare.slnx` sin warnings; `dotnet ef migrations list --project src` muestra
`Inicial`; los 8 tests del bloque pasan; `git status` no muestra `bin/`, `obj/` ni `*.db` después de
compilar y correr tests.

---

## Bloque 2 — Alta y configuración de deberes

**Archivos**
- `src/features/deberes/domain/validacion-alta-deber.cs` (nuevo) — `static class ValidacionAltaDeber` con `Resultado<AltaDeberValida, AltaDeberError> Validar(string? tipo, string? hora, string? repeticion)`; `record AltaDeberValida(TipoDeber Tipo, TimeOnly Hora, Repeticion Repeticion)`.
- `src/features/deberes/domain/alta-deber-error.cs` (nuevo) — `enum AltaDeberError { MascotaNoEncontrada, TipoInvalido, HoraObligatoria, HoraInvalida, RepeticionObligatoria }` y `static string Mensaje(AltaDeberError)`.
- `src/features/deberes/domain/deber-activo.cs` (nuevo) — función pura `EsActivo(Repeticion, bool tieneInstanciaVigenteHoy)` y record de vista `DeberActivoVista(int Id, TipoDeber Tipo, TimeOnly Hora, Repeticion Repeticion)`.
- `src/features/deberes/data/deberes-service.cs` (nuevo) — `DeberesService` (scoped): `DarDeAltaAsync`, `ListarActivosAsync`.
- `src/features/hogar/data/mascotas-service.cs` (nuevo) — `MascotasService` (scoped): `ObtenerAsync(int id)` → `MascotaVista?`, `ListarAsync()`.
- `src/features/hogar/domain/mascota-vista.cs` (nuevo) — `record MascotaVista(int Id, string Nombre)`.
- `src/features/deberes/ui/configuracion.cshtml` + `configuracion.cshtml.cs` (nuevos) — página de configuración de deberes.
- `src/features/deberes/ui/_configuracion-deberes.cshtml` (nuevo) — parcial con la lista de deberes activos y el formulario de alta (con mensajes de error).
- `src/program.cs` (modificado) — registrar `DeberesService` y `MascotasService`.
- `tests/features/deberes/validacion-alta-deber-tests.cs` (nuevo).
- `tests/features/deberes/deberes-service-tests.cs` (nuevo).
- `tests/features/deberes/configuracion-page-tests.cs` (nuevo).

**Lógica**
- `Validar` (puro) recibe los tres campos como texto y devuelve **todos** los errores encontrados:
  - `tipo`: `"comida"` → `Comida`, `"paseo"` → `Paseo`; cualquier otro valor, vacío o nulo → `TipoInvalido`.
  - `hora`: nula o solo espacios → `HoraObligatoria`; si no coincide con `^([01][0-9]|2[0-3]):[0-5][0-9]$` → `HoraInvalida`; si coincide → `TimeOnly`.
  - `repeticion`: `"solo-hoy"` → `SoloHoy`, `"todos-los-dias"` → `TodosLosDias`; otro valor, vacío o nulo → `RepeticionObligatoria`.
- `DarDeAltaAsync(int mascotaId, string? tipo, string? hora, string? repeticion, CancellationToken)`:
  1. Si la mascota no existe → `Falla([MascotaNoEncontrada])`.
  2. `Validar`; si falla, devuelve esos errores sin escribir nada.
  3. Toma el `GeneracionLock`; calcula `hoy = ZonaHogar.Hoy(time.GetUtcNow())`; crea `Deber { MascotaId, Tipo, Hora, Repeticion, FechaAlta = hoy }` y su `Instancia { Fecha = hoy, Hora }` (FR-07, aunque la hora ya haya pasado) en un único `SaveChangesAsync` (transacción implícita); libera el lock.
  4. Devuelve `Ok(deber.Id)`.
- `ListarActivosAsync(int mascotaId)`: devuelve los deberes de esa mascota (FR-03) tales que `Repeticion == TodosLosDias`, o `Repeticion == SoloHoy` y su instancia tiene `Fecha == hoy` sin `RegistroCumplimiento` (deber activo del PRD), ordenados por `Hora` y luego `Tipo`.
- Página: muestra el nombre de la mascota, la lista de deberes activos ("Paseo · 18:00 · todos los días" / "Comida · 20:00 · solo para hoy") y el formulario (radios de tipo, `<input type="time">` de hora con `name="hora"`, radios de repetición). El formulario usa `hx-post` a la misma ruta con `hx-target` sobre el contenedor del parcial y `hx-swap="outerHTML"`; sin JS hace POST normal. Enlace "Volver al estado del día" a `/`.

**Contrato HTTP**
- `GET /mascotas/{mascotaId:int}/deberes`
  - Request: `mascotaId` int en la ruta.
  - Response 200: HTML de la página completa (nombre de la mascota, deberes activos, formulario vacío).
  - Errores: 404 si la mascota no existe (página 404 por defecto, sin datos).
  - Auth: ninguna (riesgo aceptado del PRD).
- `POST /mascotas/{mascotaId:int}/deberes`
  - Request: `application/x-www-form-urlencoded` con `tipo` (string), `hora` (string, HH:MM), `repeticion` (string), `__RequestVerificationToken` (string). Header opcional `HX-Request: true`.
  - Response con `HX-Request`: 200 con el HTML del parcial `_configuracion-deberes` (lista actualizada y formulario vacío si el alta fue aceptada; lista sin cambios, formulario con los valores enviados y los mensajes de error si no).
  - Response sin `HX-Request`: alta aceptada → 303 a `GET /mascotas/{mascotaId}/deberes`; alta rechazada → 200 con la página completa y los mensajes de error.
  - Errores: 404 si la mascota no existe; 400 si falta o es inválido el token antiforgery (comportamiento del framework); 200 con mensajes para errores de validación.
  - Auth: ninguna (riesgo aceptado del PRD).

**Validación de entrada**
- Los tres campos se bindean como `string?` (nunca como enum ni `TimeOnly`, para que valores como "medicación" o "25:00" lleguen a `Validar` y no fallen en el model binding).
- `tipo`: valores permitidos `comida` | `paseo`; longitud máxima 20 (más largo → `TipoInvalido`).
- `hora`: formato `HH:MM` 24 h (`00:00`–`23:59`), longitud exacta 5; otra cosa → `HoraInvalida`; vacío → `HoraObligatoria`.
- `repeticion`: valores permitidos `solo-hoy` | `todos-los-dias`; longitud máxima 20.
- `mascotaId`: entero por restricción de ruta (`:int`); no entero → 404 por ruteo.
- La salida HTML se codifica con el encoding por defecto de Razor (nunca `Html.Raw` con datos de usuario).

**Manejo de errores**
- `MascotaNoEncontrada` → 404 (`NotFound()`), sin crear nada.
- `TipoInvalido` → mensaje "El tipo debe ser comida o paseo."
- `HoraObligatoria` → mensaje "La hora es obligatoria."
- `HoraInvalida` → mensaje "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59."
- `RepeticionObligatoria` → mensaje "Elegí \"solo para hoy\" o \"todos los días\"."
- Token antiforgery ausente o inválido → 400 del framework; no se crea nada.
- Errores de base (`DbUpdateException` u otros) → no se capturan en el servicio: se propagan al manejador global (500 genérico, Bloque 1) y quedan en el log. El índice único `(DeberId, Fecha)` no puede chocar en el alta porque el deber es nuevo y el alta toma el `GeneracionLock`.

**Tests requeridos**
- [ ] `Validar_PaseoHoraTodosLosDias_Ok` — `("paseo","18:00","todos-los-dias")` → Ok con `Paseo`, 18:00, `TodosLosDias` — valida FR-02, FR-04.
- [ ] `Validar_SinHora_HoraObligatoria` — hora nula y `"  "` → `HoraObligatoria` — valida AC-04.
- [ ] `Validar_Hora2500_HoraInvalida` — `"25:00"`, `"12:60"`, `"9:00"`, `"123456"` → `HoraInvalida` — valida AC-05.
- [ ] `Validar_TipoMedicacionOVacio_TipoInvalido` — `"medicación"`, `""`, nulo, texto de 21 caracteres → `TipoInvalido` — valida AC-06.
- [ ] `Validar_SinRepeticion_RepeticionObligatoria` — nulo, `""`, `"semanal"` → `RepeticionObligatoria` — valida AC-07.
- [ ] `Validar_VariosErrores_DevuelveTodos` — tipo y hora inválidos a la vez → ambos errores — valida FR-05.
- [ ] `DarDeAlta_PaseoTodosLosDias_ApareceEnActivos` — valida AC-02, FR-06.
- [ ] `DarDeAlta_ParaFirulais_NoApareceEnMichi` — valida AC-03, FR-03.
- [ ] `DarDeAlta_MascotaInexistente_MascotaNoEncontrada` — `mascotaId = 999` → error tipado y 0 deberes — error documentado.
- [ ] `DarDeAlta_DatosInvalidos_NoEscribeNada` — 0 deberes y 0 instancias tras un alta rechazada — valida FR-05.
- [ ] `DarDeAlta_HoraYaPasada_GeneraInstanciaDeHoy` — a las 10:30, alta "paseo 10:00 solo hoy" → existe una instancia de hoy 10:00 — valida FR-07, AC-09.
- [ ] `ListarActivos_SoloHoyDeAyerNoCumplido_NoAparece` — valida AC-08, FR-11.
- [ ] `ListarActivos_SoloHoyConRegistro_NoAparece` — instancia de hoy con registro → no activo — valida FR-06.
- [ ] `ListarActivos_TodosLosDiasConAyerExpirada_SigueApareciendo` — valida AC-19.
- [ ] `GetConfiguracion_MascotaExistente_200ConFormulario` — valida FR-02.
- [ ] `GetConfiguracion_MascotaInexistente_404` — error documentado.
- [ ] `PostAlta_Htmx_DevuelveParcialConDeber` — POST con `HX-Request` → 200 con el parcial que lista "Paseo · 18:00 · todos los días" — valida AC-02.
- [ ] `PostAlta_SinHtmx_Redirige303` — valida FR-02.
- [ ] `PostAlta_SinHora_MuestraMensajeYNoCrea` — valida AC-04.
- [ ] `PostAlta_Hora2500_MuestraMensajeYNoCrea` — valida AC-05.
- [ ] `PostAlta_TipoMedicacion_MuestraMensajeYNoCrea` — valida AC-06.
- [ ] `PostAlta_SinRepeticion_MuestraMensajeYNoCrea` — valida AC-07.
- [ ] `PostAlta_MascotaInexistente_404` — error documentado.
- [ ] `PostAlta_SinTokenAntiforgery_400` — error documentado.
- [ ] `PostAlta_FallaDeBase_Devuelve500Generico` — en Production, con un interceptor de EF de test que lanza en `SaveChanges`, el POST válido devuelve 500 genérico y no hay deber creado — error documentado.
- [ ] `PostAlta_TipoConHtml_SeMuestraCodificado` — `tipo = "<script>"` → la respuesta no contiene `<script>` sin codificar — validación de salida.

**Criterio de completitud**
Los 26 tests del bloque pasan; `configuracion.cshtml.cs` no referencia `PetCareDbContext`
(verificable con `grep`); dar de alta un deber desde el navegador en `/mascotas/1/deberes` lo muestra
en la lista sin recargar la página.

---

## Bloque 3 — Instancias: generación diaria, reinicio, vencimiento y expiración

**Archivos**
- `src/features/instancias/domain/estado-instancia.cs` (nuevo) — `static class EstadoInstancia`: `bool EsVencida(DateOnly fecha, TimeOnly hora, DateTime ahoraLocal)` = `ahoraLocal >= fecha.ToDateTime(hora).AddMinutes(15)` (fecha y hora combinadas, sin aritmética sobre `TimeOnly`); `bool EsVigente(DateOnly fecha, bool tieneRegistro, DateOnly hoy)` = `!tieneRegistro && fecha == hoy`.
- `src/features/instancias/domain/generacion-instancias.cs` (nuevo) — `static class GeneracionInstancias`: `IReadOnlyList<InstanciaAGenerar> QueFaltan(DateOnly dia, IEnumerable<DeberParaGenerar> deberes, IReadOnlySet<int> deberesConInstanciaEseDia)`: deberes con `Repeticion == TodosLosDias`, `FechaAlta <= dia` y sin instancia ese día. Records `DeberParaGenerar(int Id, TimeOnly Hora, Repeticion Repeticion, DateOnly FechaAlta)` e `InstanciaAGenerar(int DeberId, DateOnly Fecha, TimeOnly Hora)`.
- `src/features/instancias/domain/generacion-error.cs` (nuevo) — `enum GeneracionError { InstanciaYaExistia }`.
- `src/features/instancias/data/instancias-service.cs` (nuevo) — `InstanciasService` (scoped): `Task<Resultado<int, GeneracionError>> GenerarDelDiaAsync(CancellationToken)`.
- `src/shared/hosting/generacion-diaria-worker.cs` (nuevo) — `GeneracionDiariaWorker : BackgroundService`.
- `src/program.cs` (modificado) — registrar `InstanciasService` y el hosted service.
- `tests/features/instancias/estado-instancia-tests.cs` (nuevo).
- `tests/features/instancias/generacion-instancias-tests.cs` (nuevo).
- `tests/features/instancias/instancias-service-tests.cs` (nuevo).
- `tests/features/instancias/generacion-diaria-worker-tests.cs` (nuevo).

**Lógica**
- Vencimiento (FR-10) y expiración (FR-11) **no se guardan**: se calculan al leer con `EstadoInstancia`. Una instancia de un día anterior sin registro deja de ser vigente al cambiar `hoy`; no hay job de expiración.
- `GenerarDelDiaAsync` (FR-08, FR-09): toma el `GeneracionLock`; `hoy = ZonaHogar.Hoy(time.GetUtcNow())`; carga deberes `TodosLosDias` y el conjunto de `DeberId` con instancia en `hoy`; calcula `QueFaltan`; inserta y guarda; devuelve `Ok(cantidadCreada)`. Idempotente: una segunda ejecución el mismo día crea 0. Solo genera el día actual (los días en que el servidor estuvo caído no se generan, como en PRD-001).
- `GeneracionDiariaWorker.ExecuteAsync`: ejecuta una pasada al arrancar (FR-09) y después una pasada por cada tick de `PeriodicTimer(TimeSpan.FromMinutes(1), timeProvider)` hasta la cancelación. Cada pasada crea un scope de DI y llama a `GenerarDelDiaAsync`; el worker no tiene lógica propia. Expone `EjecutarPasadaAsync(CancellationToken)` como `internal` (con `InternalsVisibleTo` para tests).

**Modelo de datos**
- Sin cambios de esquema: usa `Instancia` y su índice único (`DeberId`, `Fecha`) del Bloque 1.

**Validación de entrada**
- No aplica: este bloque no recibe entrada de usuario.

**Manejo de errores**
- Choque con el índice único (`DbUpdateException` cuyo `InnerException` es `SqliteException` con `SqliteErrorCode == 19`, constraint) → `GenerarDelDiaAsync` limpia el `ChangeTracker` y devuelve `Falla([InstanciaYaExistia])`; el worker lo registra con `LogInformation` (`EventId` 3001 `InstanciaYaExistia`) y sigue. Otras `DbUpdateException` no se capturan en el servicio.
- Cualquier otra excepción dentro de una pasada → el worker la captura (excepto `OperationCanceledException` por cancelación), la registra con `LogError` (`EventId` 3002 `GeneracionFallida`, con la excepción) y sigue con el próximo tick. El host no se detiene y la excepción no se pierde.
- Cancelación del host → `OperationCanceledException` termina el loop normalmente.

**Tests requeridos**
- [ ] `EsVencida_0910_False` — instancia de hoy 09:00, ahora 09:10 → false — valida AC-16, FR-10.
- [ ] `EsVencida_0915_True` — ahora 09:15 → true — valida AC-17, FR-10.
- [ ] `EsVencida_0515Para0800_False` — valida AC-24.
- [ ] `EsVencida_2350Alas0005DelDiaSiguiente_True` — borde de medianoche — valida FR-10.
- [ ] `EsVencida_2350Alas0004DelDiaSiguiente_False` — borde de medianoche — valida FR-10.
- [ ] `EsVigente_InstanciaDeAyer_False` — valida AC-18, FR-11.
- [ ] `EsVigente_ConRegistro_False` — valida FR-11.
- [ ] `QueFaltan_TodosLosDias_IncluyeManana` — deber "todos los días" dado de alta hoy → incluido para mañana — valida AC-10, FR-04, FR-08.
- [ ] `QueFaltan_SoloHoy_NoGeneraManana` — valida AC-11, FR-04.
- [ ] `QueFaltan_FechaAltaPosterior_NoIncluye` — valida FR-08.
- [ ] `QueFaltan_YaTieneInstancia_NoIncluye` — valida FR-09.
- [ ] `GenerarDelDia_0005_CreaInstanciaDelNuevoDia` — reloj en 03:05 UTC (00:05 local) del día nuevo → existe la instancia de las 08:00 de ese día — valida AC-12, FR-08.
- [ ] `GenerarDelDia_2359_NoCreaInstanciaDeManana` — reloj en 02:59 UTC (23:59 local) → solo instancias del día actual — valida AC-13.
- [ ] `GenerarDelDia_DosVeces_UnaSolaInstancia` — valida AC-15, FR-09.
- [ ] `GenerarDelDia_ChoqueDeIndiceUnico_DevuelveInstanciaYaExistia` — se inserta la instancia por otra vía entre la lectura y el guardado (interceptor de EF en el test) → `Falla([InstanciaYaExistia])`, sin excepción y con una sola instancia — error documentado.
- [ ] `Worker_ArrancaA1000TrasCaida_GeneraInstanciaYQuedaPendiente` — sin instancia de hoy, reloj en 10:00 local, `StartAsync` → instancia de las 08:00 de hoy creada y `EsVencida` = true — valida AC-14, FR-09.
- [ ] `Worker_Reinicio_NoDuplica` — instancia de hoy ya creada, reinicio a las 10:00 → sigue habiendo una sola — valida AC-15.
- [ ] `Worker_CruzaMedianoche_GeneraEnMenosDe5Minutos` — `FakeTimeProvider` en 23:59 local, avanzar el reloj de a 1 min → la instancia del día nuevo existe a más tardar a las 00:01 local — valida NFR-01, AC-12.
- [ ] `Worker_PasadaQueLanza_RegistraErrorYSigue` — servicio que lanza en la primera pasada → `LogError` con `EventId` 3002 y la segunda pasada se ejecuta — error documentado.
- [ ] `Worker_InstanciaYaExistia_RegistraInformacionYSigue` — servicio que devuelve `InstanciaYaExistia` → `LogInformation` con `EventId` 3001 — error documentado.
- [ ] `Worker_Cancelacion_TerminaSinError` — `StopAsync` durante la espera del timer → `ExecuteAsync` termina sin excepción y sin `LogError` — error documentado.

**Criterio de completitud**
Los 21 tests del bloque pasan; `grep -rE "DateTime(Offset)?\.(Now|UtcNow)|TimeZoneInfo\.Local" src/`
no devuelve resultados; con `dotnet run` y la base migrada, el log muestra la pasada inicial del
worker sin errores.

---

## Bloque 4 — Estado del día

**Archivos**
- `src/features/estado-del-dia/domain/estado-del-dia.cs` (nuevo) — records de vista: `EstadoDelDia(IReadOnlyList<SeccionMascota> Secciones)`; `SeccionMascota(int MascotaId, string Nombre, IReadOnlyList<InstanciaVista> Instancias, int PaseosHoy, UltimoRegistro? UltimaComida, UltimoRegistro? UltimoPaseo)` con `bool SinTareasPendientes => Instancias.Count == 0`; `InstanciaVista(int Id, TipoDeber Tipo, TimeOnly Hora, bool Pendiente)`; `UltimoRegistro(DateOnly Fecha, TimeOnly HoraReal)`.
- `src/features/estado-del-dia/domain/armado-estado-del-dia.cs` (nuevo) — `static EstadoDelDia Armar(IReadOnlyList<MascotaVista> mascotas, IReadOnlyList<InstanciaLeida> instancias, IReadOnlyList<RegistroLeido> registros, DateTime ahoraLocal)`. Puro. Records de entrada `InstanciaLeida(int Id, int MascotaId, TipoDeber Tipo, DateOnly Fecha, TimeOnly Hora, bool TieneRegistro)` y `RegistroLeido(int MascotaId, TipoDeber Tipo, DateOnly Fecha, TimeOnly HoraReal)`.
- `src/features/estado-del-dia/domain/textos-estado-del-dia.cs` (nuevo) — funciones puras de formato: `Paseos(int n)` ("0 paseos", "1 paseo", "2 paseos"), `FechaHora(UltimoRegistro)` ("dd/MM/yyyy HH:mm"), etiquetas de tipo ("Comida", "Paseo").
- `src/features/estado-del-dia/data/estado-del-dia-service.cs` (nuevo) — `EstadoDelDiaService` (scoped): `Task<EstadoDelDia> ObtenerAsync(CancellationToken)`.
- `src/features/estado-del-dia/ui/index.cshtml` + `index.cshtml.cs` (nuevos) — `@page "/"`, pantalla principal.
- `src/features/Shared/_Layout.cshtml` (modificado, creado en el Bloque 1) — `<meta viewport>`, `site.css`, `htmx.min.js`, título "PetCare Hogar".
- `src/wwwroot/css/site.css` (nuevo) — estilos mobile-first.
- `src/wwwroot/lib/htmx/htmx.min.js` (nuevo) — HTMX en una versión 2.0.x exacta, descargado de la release oficial de GitHub (`bigskysoftware/htmx`), vendorizado (usado por el formulario del Bloque 2).
- `src/wwwroot/lib/htmx/VERSION.txt` (nuevo) — versión exacta, URL de origen y SHA-256 de `htmx.min.js` (M4).
- `src/program.cs` (modificado) — registrar `EstadoDelDiaService`.
- `tests/features/estado-del-dia/armado-estado-del-dia-tests.cs` (nuevo).
- `tests/features/estado-del-dia/textos-estado-del-dia-tests.cs` (nuevo).
- `tests/features/estado-del-dia/index-page-tests.cs` (nuevo).
- `tests/features/estado-del-dia/sin-scroll-horizontal-tests.cs` (nuevo) — Playwright.
- `tests/infra/playwright-fixture.cs` (nuevo) — levanta la app con `WebApplicationFactory.UseKestrel()` en un puerto libre y SQLite en memoria; instala Chromium una vez con `Microsoft.Playwright.Program.Main(["install", "chromium"])` (idempotente); expone `IBrowser`.

**Lógica**
- `ObtenerAsync`: `ahora = ZonaHogar.AhoraLocal(time.GetUtcNow())`, `hoy = DateOnly.FromDateTime(ahora)`; lee mascotas del hogar (orden por `Nombre`), instancias con `Fecha == hoy` (con su tipo y si tienen registro) y registros de cumplimiento de las mascotas (tipo, fecha, hora real); llama a `Armar`.
- `Armar` (puro):
  - Una sección por mascota (FR-12), aunque no tenga instancias (FR-17).
  - Instancias vigentes de la mascota (`EsVigente`), ordenadas por `Hora` y luego `Tipo`, con `Pendiente = EsVencida(...)` (FR-13, FR-10).
  - `PaseosHoy` = cantidad de registros de paseo con `Fecha == hoy` (FR-14).
  - `UltimaComida` / `UltimoPaseo` = registro de ese tipo con mayor (`Fecha`, `HoraReal`), de cualquier día, o `null` (FR-15, FR-16).
- Vista por sección (`data-testid="seccion-{mascotaId}"`): título con el nombre; lista de instancias ("Comida · 08:00") con la marca "pendiente" (`data-testid="marca-pendiente"`) si corresponde; si no hay instancias, "No hay tareas pendientes."; resumen: `Paseos(n)`, "Última comida: dd/MM/yyyy HH:mm" o "Sin comidas registradas", "Último paseo: dd/MM/yyyy HH:mm" o "Sin paseos registrados"; enlace "Configurar deberes" a `/mascotas/{id}/deberes`.
- Sin actualización automática: la pantalla se actualiza al abrirla o recargarla (FEAT-001b la agrega).
- CSS (NFR-02): `box-sizing: border-box` global; `body` sin `min-width`; contenedor `max-width: 40rem; padding: 0 1rem`; textos con `overflow-wrap: anywhere`; inputs y botones `width: 100%` en el formulario; sin tablas de ancho fijo.

**Contrato HTTP**
- `GET /`
  - Request: sin parámetros.
  - Response 200: HTML del estado del día.
  - Errores: 500 genérico (página `/error`) ante fallas de base o excepciones no previstas, según Bloque 1.
  - Auth: ninguna (riesgo aceptado del PRD).

**Validación de entrada**
- No aplica: la página no recibe entrada de usuario. Los nombres de mascota se muestran con el encoding por defecto de Razor.

**Manejo de errores**
- Falla de base u otra excepción en `ObtenerAsync` → no se captura: el manejador global devuelve 500 genérico y la excepción queda en el log (Bloque 1).
- Hogar sin mascotas → no ocurre en este ticket (seed fijo, sin alta ni baja de mascotas; RF-38 queda fuera de alcance). Si ocurriera, `Armar` devuelve 0 secciones y la página se muestra vacía sin error.

**Tests requeridos**
- [ ] `Armar_ComidaYPaseoA1200_ComidaPendientePaseoSinMarca` — valida AC-20, FR-12, FR-13.
- [ ] `Armar_DosPaseosYComidaHoy_ResumenCorrecto` — "2 paseos", último paseo hoy 11:00, última comida hoy 13:00 — valida AC-21, FR-14, FR-15, FR-16.
- [ ] `Armar_RegistrosDeAyer_MuestraFechasDeAyerYCeroPaseos` — valida AC-22.
- [ ] `Armar_SinInstancias_SeccionConSinTareasPendientes` — valida FR-17.
- [ ] `Armar_ExcluyeInstanciasDeAyerYCumplidas` — valida FR-11, FR-12.
- [ ] `Armar_OrdenaPorHora` — valida FR-12.
- [ ] `Armar_SinMascotas_CeroSecciones` — caso de error documentado (no ocurre con el seed).
- [ ] `Paseos_Pluraliza` — 0 → "0 paseos", 1 → "1 paseo", 2 → "2 paseos" — valida FR-14.
- [ ] `FechaHora_Formato` — `25/10/2026 11:00` — valida FR-15, FR-16.
- [ ] `Index_PrimeraVez_MuestraDosSeccionesVacias` — exactamente las secciones Firulais y Michi, cada una con "0 paseos", "Sin comidas registradas", "Sin paseos registrados" y "No hay tareas pendientes." — valida AC-01.
- [ ] `Index_DeberDeFirulais_NoApareceEnSeccionDeMichi` — valida AC-03.
- [ ] `Index_AltaConHoraPasada_MuestraPendiente` — a las 10:30 alta "paseo 10:00 solo hoy" vía `DeberesService` → `GET /` muestra la instancia con marca pendiente — valida AC-09.
- [ ] `Index_Manana0800_MuestraInstanciaDeTodosLosDias` — alta hoy, avanzar el reloj a mañana 08:00, `GenerarDelDiaAsync`, `GET /` → instancia 09:00 — valida AC-10.
- [ ] `Index_Manana0800_NoMuestraSoloHoyDeAyer` — valida AC-11.
- [ ] `Index_0005_MuestraInstanciaDelNuevoDia` — valida AC-12.
- [ ] `Index_2359_NoMuestraInstanciaDeManana` — valida AC-13.
- [ ] `Index_0910_SinMarcaPendiente` — valida AC-16.
- [ ] `Index_0915_MarcaPendiente` — valida AC-17.
- [ ] `Index_0700_NoMuestraAyerYMuestraHoySinMarca` — valida AC-18.
- [ ] `Index_1200_ComidaPendientePaseoSinMarca` — valida AC-20.
- [ ] `Index_0515Local_NoMarcaPendiente` — reloj en 08:15 UTC con comida 08:00 → sin marca — valida AC-24.
- [ ] `Index_FallaDeBase_Devuelve500Generico` — en Production, con un interceptor de EF de test que lanza al consultar, `GET /` devuelve 500 genérico sin detalles — error documentado.
- [ ] `Index_NombreConHtml_SeMuestraCodificado` — mascota insertada en el test con nombre `<b>x</b>` → aparece codificado — validación de salida.
- [ ] `SinScrollHorizontal_360px_EstadoDelDia` — viewport 360×740: `document.documentElement.scrollWidth <= window.innerWidth` — valida AC-23, NFR-02.
- [ ] `SinScrollHorizontal_360px_ConfiguracionDeDeberes` — con 3 deberes activos — valida AC-23, NFR-02.
- [ ] `SinScrollHorizontal_360px_AltaDeDeberConErrores` — formulario enviado con los 3 errores visibles, y luego un alta aceptada — valida AC-23, NFR-02.

**Criterio de completitud**
Los 26 tests del bloque pasan (los de Playwright con Chromium instalado); `index.cshtml.cs` no
referencia `PetCareDbContext`; con `dotnet ef database update --project src` y `dotnet run --project
src`, `GET /` muestra las secciones de Firulais y Michi.

---

## Verificación final

- `dotnet build petcare.slnx` sin warnings ni errores.
- `dotnet test petcare.slnx`: los 81 tests de los 4 bloques pasan.
- Cada AC-01…AC-24 tiene al menos un test que pasa (tabla "Cobertura").
- `grep -rE "DateTime(Offset)?\.(Now|UtcNow)|TimeZoneInfo\.Local|EnsureCreated|\.Migrate\(" src/` sin resultados.
- Ningún PageModel de `src/features/**/ui/` referencia `PetCareDbContext`.
- `grep -rE "FromSqlRaw|FromSqlInterpolated|ExecuteSqlRaw|ExecuteSqlInterpolated|SqlQueryRaw|Html\.Raw" src/` sin resultados (M2, R3).
- Ningún `Log*` de `src/` recibe `Email` (M3).
- Los `.csproj` y `.config/dotnet-tools.json` usan versiones exactas (M4).
- `git status` limpio de `bin/`, `obj/`, `*.db`.
- Flujo manual en navegador a 360 px: abrir `/`, entrar a "Configurar deberes" de Firulais, dar de alta "comida 08:00 todos los días", volver y ver la instancia (pendiente si ya pasaron 15 minutos de las 08:00).

## Rollback

Ticket inicial, sin datos productivos: revertir los commits de la rama. La migración `Inicial` se
deshace con `dotnet ef database update 0 --project src` o borrando la base de desarrollo local
manualmente (nunca desde código).
