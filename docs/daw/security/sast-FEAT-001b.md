# SAST FEAT-001b: Cumplimiento, email de vencimiento y actualización automática del estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| Fecha | 2026-10-26 |
| Alcance | `src/` y `tests/` modificados en la rama (`main..fdca73b`): features `instancias`, `emails`, `estado-del-dia`, `deberes`, `shared/hosting`, `program.cs`, `appsettings.json`, `site.css` |
| Método | Búsqueda de patrones por categoría del catálogo §4 + revisión manual de puntos de entrada y de las mitigaciones del threat model + `dotnet list petcare.slnx package --vulnerable --include-transitive` |
| Threat model | `docs/daw/security/threat-FEAT-001b.md` (M3 reformulada, M3-bis, M5, M6, M7; riesgo aceptado de email duplicado) |

## Resultado

**PASSED**: 0 Critical, 0 High y 0 Medium. Hay 3 hallazgos Low/Informational documentados.

## Categorías revisadas

| ID | Categoría | Resultado | Evidencia |
|---|---|---|---|
| F-SAST-01 | Secretos hardcodeados | ✅ Limpio | Ningún `appsettings*.json` ni `launchSettings.json` tiene `Email:Smtp:*` (M3-bis). Las credenciales SMTP llegan solo por variables de entorno (`src/program.cs:21`). El único literal que parece una clave (`Clave = "clave-secreta"`) está en `tests/features/emails/opciones-email-tests.cs`: es un dato de prueba, no una credencial. No hay `*.db` ni `.env` versionados. |
| F-SAST-02 | Inyección SQL | ✅ Limpio | No hay `FromSql*`, `ExecuteSql*` ni `SqlQuery*` en `src/`. Todo el acceso es LINQ de EF Core (parametrizado). |
| F-SAST-03 | Inyección de comandos | ✅ Limpio | No hay `Process.Start` en `src/`. |
| F-SAST-04 | Deserialización insegura | ✅ Limpio | No hay `BinaryFormatter` ni `TypeNameHandling`. |
| F-SAST-05 | Path traversal | ✅ Limpio | No hay acceso a archivos con datos del usuario. `ConsolaEnvioEmail` escribe a un `TextWriter` inyectado. |
| F-SAST-06 | XSS | ✅ Limpio | No hay `Html.Raw` ni `innerHTML` en `src/`: toda la salida pasa por el encoding de Razor. Los atributos `hx-*` nuevos (`_estado-del-dia.cshtml:10`) tienen valores constantes; `RefrescoSegundos` es un entero validado y formateado con `InvariantCulture`. |
| F-SAST-07 | SSRF | ✅ Limpio | El único destino de red es el host SMTP. Lo fija la configuración del operador por variable de entorno, nunca un dato del usuario. |
| F-SAST-08 | Criptografía rota | ✅ Limpio | No hay MD5, SHA1 ni DES. SMTP usa `StartTls`, o `SslOnConnect` en el puerto 465 (`smtp-envio-email.cs:30`), y no reemplaza la validación de certificados: no hay `ServerCertificateValidationCallback` (M6). `SecureSocketOptions.None` solo se usa en el constructor `internal` para tests. |
| F-SAST-09 | Debug en producción | ✅ Limpio | `UseExceptionHandler("/error")` está activo fuera de Development (`src/program.cs:56-58`). En Development la app no arranca si `Email:Modo` no es `Consola` (`ValidacionOpcionesEmail`). |
| F-SAST-10 | Datos sensibles en logs | ✅ Limpio | Todos los logs son `[LoggerMessage]` tipados. Registran ids, cantidades, el enum `EnvioEmailError` y el tipo y `HResult` de la excepción, nunca direcciones ni `Exception.Message` (`vencimientos-worker.cs:57-61`, M3 reformulada). Las direcciones solo aparecen en la salida de `ConsolaEnvioEmail`, que es la decisión del usuario en PLAN. |
| F-SAST-11 | Upload sin restricción | ✅ No aplica | No hay uploads. |
| F-SAST-12 | CSRF | ✅ Limpio | Los POST nuevos (`OnPostCumplirAsync` y `OnPostEliminarRegistroAsync`) validan antiforgery por defecto. Lo cubren los tests `Cumplir_SinToken_400` y `EliminarRegistro_SinToken_400`. El handler `Estado` del polling es un GET y no cambia estado. |
| F-SAST-13 / F-SAST-16 | CVE en dependencias | ✅ Limpio | `dotnet list petcare.slnx package --vulnerable --include-transitive` devuelve "no vulnerable packages" en `petcare-web` y `petcare-tests` (nuget.org, 2026-10-26). La dependencia nueva es MailKit/MimeKit 4.18.2, con versión exacta. |
| F-SAST-14 | Validación de entrada incompleta | ✅ Limpio | `horaReal` se valida en `ValidacionCumplimiento.Validar`: no vacía, formato `FormatoHora` y no posterior a la hora actual. `instanciaId` y `registroId` son `int?`: si faltan, la respuesta es 400, y si no existen, 404. `EstadoDelDia:RefrescoSegundos` se valida en 1..15 al arrancar, y se inyecta la misma instancia que se validó. El asunto del email no lleva `\r` ni `\n` (`mensaje-vencimiento.cs:29-30`, M5). |
| F-SAST-15 | Manejo de errores que filtra detalles | ✅ Limpio | El 500 es genérico, sin detalles. Lo cubren `Estado_FallaDeBase_500Generico` y los tests equivalentes de los bloques 2 y 3. Los `catch` usan filtros `when` acotados: ninguno es silencioso y todos registran o traducen a un error tipado. |
| F-SAST-17 | Funciones inseguras | ✅ Limpio | No hay `eval` en el código propio. Ver L-1 sobre el filtro de `hx-trigger`. |

## Hallazgos Low / Informational (W-SAST-01: documentados, no bloquean)

| Id | Severidad | Archivo | Hallazgo | Disposición |
|---|---|---|---|---|
| L-1 | Informational | `src/features/estado-del-dia/ui/_estado-del-dia.cshtml:6-10` | HTMX evalúa el filtro de `hx-trigger` (`[!document.querySelector(...)]`) gracias a `htmx.config.allowEval`. | La expresión es constante y no lleva datos del usuario. Si en el futuro se agrega una CSP sin `unsafe-eval`, hay que revisarla; el comentario en la vista lo deja documentado (decisión del spec, Bloque 5). |
| L-2 | Informational | `src/features/emails/data/smtp-envio-email.cs` | Si el servidor SMTP acepta el email y falla el guardado de `EmailVencimiento`, el email puede salir duplicado. | Es un riesgo aceptado por el usuario en PLAN (2026-10-25). Afecta la integridad del aviso, no la confidencialidad. |
| L-3 | Low | `src/program.cs` | Siguen faltando los headers de seguridad y HTTPS/HSTS, igual que en L-1 y L-3 de `sast-FEAT-001a.md`. | Sin cambios respecto de FEAT-001a: se aceptan mientras la app escuche solo en `localhost` (RA-1 y RA-2). Se agregan en el ticket de registro y sesión. |

## Supresiones

Ninguna, porque no hay hallazgos Medium.

```
/daw-security-sast — PASSED
Secretos ✅ · Inyección ✅ · XSS/funciones inseguras ✅ · CSRF ✅ · Logs ✅ · Dependencias ✅
Supresiones: 0
Total: 17 categorías limpias, 0 vulnerabilidades (0 critical, 0 high, 0 medium); 3 low/info documentados
Report: docs/daw/security/sast-FEAT-001b.md
```

---

## Re-ejecución tras el corrective loop VERIFY→CODE (2026-10-26, commit 79186c8)

| Campo | Valor |
|-------|-------|
| Alcance | `git diff ce58a05..79186c8`: `src/features/emails/data/smtp-envio-email.cs` (extracción de `SeguridadPara`) y 4 archivos de `tests/` |
| Resultado | **PASSED**: 0 Critical, 0 High, 0 Medium. No hay hallazgos nuevos. |

| ID | Resultado | Evidencia |
|---|---|---|
| F-SAST-01 | ✅ | Las únicas credenciales nuevas son constantes de test (`Usuario = "avisos"`, `Clave = "clave-de-prueba"` en `tests/features/emails/smtp-envio-email-tests.cs`) contra el servidor SMTP falso en loopback. En `src/` no hay ninguna. |
| F-SAST-08 | ✅ | `SeguridadPara(int? puerto)` conserva la misma expresión: TLS obligatorio y sin `ServerCertificateValidationCallback` (M6). Ahora lo cubren los tests `SeguridadPara_Puerto_TlsObligatorio` y `Smtp_ConstructorPublicoEnPuertoNo465_ExigeStartTlsYNoEnviaEnClaro`. |
| F-SAST-13 / F-SAST-16 | ✅ | `dotnet list package --vulnerable --include-transitive` no encuentra paquetes vulnerables. |
| Resto (F-SAST-02..07, 09..12, 14, 15, 17) | ✅ | El diff en `src/` es una extracción sin entradas nuevas, sin E/S nueva y sin logs nuevos. |

Pendiente que no es de seguridad, para un ticket aparte (decisión del usuario): `SmtpEnvioEmail.Clasificar` no traduce a error tipado `NotSupportedException` (servidor sin STARTTLS) ni `ServiceNotAuthenticatedException`. Igual nunca se envía en claro.
