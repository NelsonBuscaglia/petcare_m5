# SAST FEAT-001a: Deberes de comida y paseo: alta, instancias y estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Fecha | 2026-10-25 |
| Alcance | `src/` y `tests/` (C# .NET 10, Razor Pages, EF Core 10 SQLite, HTMX 2.0.10 vendorizado), commits 3a1bf4d..bb979f8 |
| Método | Búsqueda de patrones por categoría del catálogo §4 + revisión manual de puntos de entrada + `dotnet list package --vulnerable --include-transitive` |
| Threat model | `docs/daw/security/threat-FEAT-001a.md` (M1–M4, RA-1, RA-2) |

## Resultado

**PASSED** — 0 Critical, 0 High, 0 Medium. 4 Low/Informational documentados.

## Categorías revisadas

| ID | Categoría | Resultado | Evidencia |
|---|---|---|---|
| F-SAST-01 | Secretos hardcodeados | ✅ Limpio | Sin claves, contraseñas ni credenciales SMTP en `src/` ni `appsettings*.json`. La única cadena de conexión es `Data Source=petcare.db` (archivo local, sin credenciales). Ningún `*.db`, `.env`, `.pfx` ni `.key` versionado; `*.db` está en `.gitignore`. |
| F-SAST-02 | Inyección SQL | ✅ Limpio | Sin `FromSql*`, `ExecuteSql*`, `SqlQuery*`, `SqliteCommand` ni `CommandText` en `src/` (M2). Todo el acceso es LINQ de EF Core (parametrizado). |
| F-SAST-03 | Inyección de comandos | ✅ Limpio | Sin `Process.Start` en `src/`. (`Microsoft.Playwright.Program.Main` solo en `tests/infra/playwright-fixture.cs`, con argumentos constantes.) |
| F-SAST-04 | Deserialización insegura | ✅ Limpio | Sin `BinaryFormatter` ni `TypeNameHandling`. |
| F-SAST-05 | Path traversal | ✅ Limpio | Sin acceso a archivos con datos del usuario en `src/`. |
| F-SAST-06 | XSS | ✅ Limpio | Sin `Html.Raw`/`HtmlString` (M2/R3). Toda salida pasa por el encoding de Razor; cubierto por `PostAlta_TipoConHtml_SeMuestraCodificado` e `Index_NombreConHtml_SeMuestraCodificado`. Ningún atributo `hx-*` lleva datos del usuario; HTMX 2.0.10 sin CVE conocido. |
| F-SAST-07 | SSRF | ✅ Limpio | Sin `HttpClient`/`WebRequest` en `src/`. |
| F-SAST-08 | Criptografía rota | ✅ No aplica | Sin criptografía ni contraseñas en este ticket. |
| F-SAST-09 | Debug en producción | ✅ Limpio | `UseExceptionHandler("/error")` fuera de Development (`src/program.cs`); la página de excepción del desarrollador solo en Development (default de ASP.NET Core). Sin `EnableSensitiveDataLogging`. Logging de SQL en `Warning` en Development (M3). |
| F-SAST-10 | Datos sensibles en logs | ✅ Limpio | Los únicos logs (`generacion-diaria-worker.cs`, EventId 3000–3002) registran cantidades y la excepción, nunca emails (M3). |
| F-SAST-11 | Upload sin restricción | ✅ No aplica | No hay uploads. |
| F-SAST-12 | CSRF | ✅ Limpio | El único POST (`/mascotas/{id}/deberes`) valida antiforgery por defecto; test `PostAlta_SinTokenAntiforgery_400`. Ver L-2 sobre `/error`. |
| F-SAST-13 / F-SAST-16 | CVE en dependencias | ✅ Limpio | `dotnet list petcare.slnx package --vulnerable --include-transitive`: "no vulnerable packages" en `petcare-web` y `petcare-tests` (fuente nuget.org, 2026-10-25). Versiones exactas (M4). |
| F-SAST-14 | Validación de entrada incompleta | ✅ Limpio | `tipo`, `hora`, `repeticion` como `string?` validados contra listas cerradas y regex `^([01][0-9]|2[0-3]):[0-5][0-9]\z` (`validacion-alta-deber.cs`); `mascotaId` con restricción de ruta `:int` y 404 si no existe. |
| F-SAST-15 | Manejo de errores que filtra detalles | ✅ Limpio | 500 genérico sin stack ni mensaje; tests `Error_*_Devuelve500Generico`, `PostAlta_FallaDeBase_Devuelve500Generico`, `Index_FallaDeBase_Devuelve500Generico`. |
| F-SAST-17 | Funciones inseguras | ✅ Limpio | Sin `eval` ni ejecución dinámica. |

## Hallazgos Low / Informational (W-SAST-01: documentados, no bloquean)

| Id | Severidad | Archivo | Hallazgo | Disposición |
|---|---|---|---|---|
| L-1 | Low | `src/program.cs` | Sin headers de seguridad (`X-Content-Type-Options`, `X-Frame-Options`/CSP `frame-ancestors`, `Referrer-Policy`): la app podría embeberse en un iframe (clickjacking sobre el alta de deberes). | Riesgo bajo mientras la app escuche solo en `localhost` (M1, RA-1). Agregar en el ticket de registro y sesión, junto con HTTPS. |
| L-2 | Informational | `src/features/Shared/error.cshtml.cs:10` | `[IgnoreAntiforgeryToken]` en `/error`. | Falso positivo: el modelo no tiene handlers (no cambia estado); el atributo existe para que `UseExceptionHandler` pueda re-ejecutar la página tras un POST fallido. |
| L-3 | Informational | `src/program.cs` | Sin HTTPS/HSTS. | Cubierto por el riesgo aceptado RA-2 (threat model), condicionado a uso solo en `localhost`. |
| L-4 | Informational | `.gitignore` | No incluye `.env`. | El proyecto no usa archivos `.env` (configuración por `appsettings*.json` y variables de entorno); no hay ninguno en el repo. Agregarlo si se introducen. |

## Supresiones

Ninguna (no hay hallazgos Medium).

```
/daw-security-sast — PASSED
Secretos ✅ · Inyección ✅ · XSS/funciones inseguras ✅ · CSRF ✅ · Dependencias ✅
Supresiones: 0
Total: 17 categorías limpias, 0 vulnerabilidades (0 critical, 0 high, 0 medium); 4 low/info documentados
Report: docs/daw/security/sast-FEAT-001a.md
```
