# PRD FEAT-001b: Cumplimiento, email de vencimiento y actualización automática del estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| Tracker | ninguno |
| Fecha | 2026-10-25 |
| PRD loops | 1 |
| PRD padre | `docs/daw/prd/prd-FEAT-001.md` (índice del split) |
| Depende de | FEAT-001a (`docs/daw/prd/prd-FEAT-001a.md`) |
| PRD de origen | `docs/daw/prd/PRD.md` (PRD-001); entre paréntesis, el RF/RNF/AC de origen |

## Contexto y Problema

FEAT-001a deja a los cuidadores ver las comidas y paseos de hoy de cada mascota y cuáles vencieron,
pero no les deja decir que una tarea ya se hizo, no avisa a nadie si se olvidó, y la pantalla solo
cambia al recargarla. Este sub-ticket completa el ciclo de FEAT-001:

- El cuidador **marca una instancia como cumplida**, con la hora real (editable), y puede
  **eliminar un registro del día** cargado por error.
- El sistema manda un **email de vencimiento** a todos los cuidadores cuando una instancia vence sin
  cumplirse, con reintentos si el servicio de email falla, aunque nadie tenga la app abierta.
- El estado del día abierto en pantalla **se actualiza solo** dentro de los 15 segundos.

## Objetivos

- Que registrar un cumplimiento lleve 5 toques/clics o menos desde el estado del día, y que los
  demás cuidadores lo vean en su pantalla dentro de los 15 segundos sin recargar.
- Que una comida o un paseo olvidados disparen un email a todos los cuidadores dentro de los 5
  minutos posteriores a su vencimiento, aunque nadie tenga la app abierta.

## Glosario

Se usan los términos del glosario de PRD-001 y de FEAT-001a. Precisión para este ticket:

- **Registro del día:** registro de cumplimiento cuya fecha (FR-05) es el día actual.

## Requerimientos Funcionales

Entre paréntesis, el requerimiento de origen en PRD-001.

### Actualización automática

- FR-01: El sistema debe actualizar el estado del día abierto en pantalla, sin que el cuidador lo recargue, cuando cambia cualquier dato que se muestra en él: porque otro cuidador da de alta un deber, registra un cumplimiento o elimina un registro, o porque una instancia pasa a estar vencida o expira. (RF-31)

### Registro de cumplimiento

- FR-02: El sistema debe permitir registrar, desde el estado del día, el cumplimiento de cualquier instancia vigente. (RF-21)
- FR-03: Al registrar un cumplimiento, el sistema debe pedir la hora real de cumplimiento, mostrando la hora actual como valor por defecto y permitiendo editarla. (RF-23)
- FR-04: El sistema debe rechazar una hora real de cumplimiento posterior a la hora actual, sin crear el registro e informando que la hora no puede ser posterior a la actual. (RF-23b, RF-36)
- FR-05: El sistema debe guardar como fecha de un registro de cumplimiento el día en que se registra. (RF-23c)
- FR-06: El sistema debe rechazar el registro de cumplimiento de una instancia que ya tiene su cumplimiento registrado, sin crear un segundo registro e informando que la instancia ya fue cumplida. (RF-21b, RF-21c)
- FR-07: El sistema debe rechazar el registro de cumplimiento de una instancia que ya expiró, sin crear el registro e informando que la instancia expiró. *(Supuesto marcado: PRD-001 no cubre este caso; surge de que una pantalla abierta antes de la medianoche puede enviar el cumplimiento después.)*

### Registros del día y su eliminación

- FR-08: El sistema debe mostrar, en la sección de cada mascota del estado del día, sus registros del día, cada uno con su tipo y su hora real, junto con la opción de eliminarlo. *(Supuesto marcado: PRD-001 pide poder eliminar registros del día, RF-25, pero no dice desde dónde; se ubican en el estado del día.)*
- FR-09: El sistema debe permitir eliminar un registro del día. (RF-25)
- FR-10: El sistema debe rechazar la eliminación de un registro de cumplimiento cuya fecha no es el día actual, sin eliminarlo e informando que solo se pueden eliminar registros del día. (RF-25, RF-36)
- FR-11: Al eliminar un registro de cumplimiento, el sistema debe volver a dejar vigente la instancia correspondiente, sin afectar al deber. (RF-26)
- FR-12: El sistema debe rechazar la eliminación de un registro que ya fue eliminado, informando que el registro ya fue eliminado. *(Supuesto marcado: análogo a RF-21b para dos cuidadores que eliminan el mismo registro.)*

### Email de vencimiento

- FR-13: El sistema debe enviar un email de vencimiento a todos los cuidadores del hogar cuando una instancia pasa al estado vencida y todavía no se envió (FR-17) un email de vencimiento para esa instancia, sin depender de que haya un cuidador con la aplicación abierta. (RF-32)
- FR-14: El sistema no debe enviar email de vencimiento para una instancia que ya estaba vencida en el momento en que se generó. (RF-32b)
- FR-15: El sistema debe incluir en el email de vencimiento el nombre de la mascota, el tipo de deber y la fecha y hora de la instancia vencida. (RF-37)
- FR-16: Cuando el servicio de envío de emails rechaza o no responde un email de vencimiento, el sistema debe reintentar su envío hasta que el servicio lo acepte o hasta que pasen 5 minutos desde que se cumplió la condición que lo dispara. (RF-32c)
- FR-17: El sistema debe considerar enviado un email de vencimiento solo cuando el servicio de envío de emails lo aceptó. (RF-32d)
- FR-18: El sistema debe considerar fallido un intento de envío al que el servicio de envío de emails no responde dentro de los 30 segundos. (RF-32e)

## Requerimientos No Funcionales

- NFR-01: El sistema debe entregar al servicio de envío de emails cada email de vencimiento dentro de los 5 minutos posteriores a que se cumpla la condición que lo dispara, incluida su detección, sin depender de que haya cuidadores con la aplicación abierta, mientras el servidor esté en ejecución, salvo que el servicio rechace o no responda durante toda esa ventana. (RNF-01)
- NFR-02: La actualización automática del estado del día (FR-01) debe reflejarse en los cuidadores con la aplicación abierta dentro de los 15 segundos posteriores al cambio que la origina, mientras el servidor esté en ejecución. (RNF-04)
- NFR-03: Registrar el cumplimiento de una instancia debe requerir 5 toques/clics o menos, desde el estado del día hasta la confirmación inclusive, aceptando la hora por defecto. (RNF-03)
- NFR-04: Las pantallas de este ticket (registro de cumplimiento y eliminación de registro) deben poder usarse completas desde un navegador móvil con un viewport de 360 píxeles de ancho, sin scroll horizontal. (RNF-02)

## Criterios de Aceptación

En formato EARS. Entre paréntesis, los FR/NFR que valida cada uno y, al final, el AC de origen en
PRD-001. Las horas son UTC−3 (FEAT-001a, NFR-03).

### Actualización automática

- AC-01 (FR-01, FR-02, NFR-02): WHEN dos cuidadores tienen abierto el estado del día y uno de ellos registra el cumplimiento de una instancia vigente de paseo de Firulais, THE sistema SHALL, dentro de los 15 segundos siguientes y sin recarga manual, dejar de mostrar esa instancia en la pantalla del otro cuidador y mostrarle la cantidad de paseos de Firulais aumentada en 1. (AC-47)
- AC-02 (FR-01, NFR-02): WHILE un cuidador tiene abierto desde las 12:14 el estado del día sin interactuar, con el deber "paseo, hora fija 12:00" sin cumplimiento registrado, WHEN llegan las 12:15, THE sistema SHALL mostrar esa instancia marcada como "pendiente" a más tardar a las 12:15:15. (AC-47b)
- AC-03 (FR-01, NFR-02): WHEN dos cuidadores tienen abierto el estado del día y uno de ellos da de alta para Firulais "comida, hora fija 21:00, solo para hoy" a las 12:00, THE sistema SHALL mostrar esa instancia en la sección de Firulais de la pantalla del otro cuidador dentro de los 15 segundos siguientes, sin recarga manual.
- AC-04 (FR-01, NFR-02): WHILE un cuidador tiene abierto desde las 23:59 el estado del día sin interactuar, con la instancia de hoy de "paseo, hora fija 20:00, todos los días" sin cumplir, WHEN llegan las 00:00, THE sistema SHALL dejar de mostrar esa instancia a más tardar a las 00:00:15.

### Registro de cumplimiento

- AC-05 (FR-02): WHEN un cuidador registra, desde el estado del día, el cumplimiento de una instancia vigente de paseo de Firulais, THE sistema SHALL dejar de mostrar esa instancia y mostrar la cantidad de paseos de Firulais aumentada en 1. (AC-32)
- AC-06 (FR-03, FR-05): WHEN a las 10:30 un cuidador registra el cumplimiento de una instancia vigente de comida aceptando la hora propuesta, THE sistema SHALL mostrar la hora 10:30 como valor propuesto y, después del registro, la última comida con la fecha de hoy y la hora 10:30. (AC-35)
- AC-07 (FR-03): WHEN a las 10:30 un cuidador registra el cumplimiento de una instancia vigente de comida cambiando la hora propuesta a 10:00, THE sistema SHALL mostrar la última comida con la fecha de hoy y la hora 10:00. (AC-35b)
- AC-08 (FR-04): IF a las 10:30 un cuidador registra el cumplimiento de una instancia vigente de comida cambiando la hora propuesta a 11:00, THEN THE sistema SHALL rechazar el registro, informarle que la hora no puede ser posterior a la actual y seguir mostrando la instancia en el estado del día. (AC-35c, AC-63c)
- AC-09 (FR-06): IF dos cuidadores ven la misma instancia vigente de paseo de Firulais y el cuidador 2 registra su cumplimiento después del cuidador 1 pero antes de que su pantalla se actualice, THEN THE sistema SHALL rechazar el registro del cuidador 2, informarle que la instancia ya fue cumplida y mostrar la cantidad de paseos de Firulais aumentada solo en 1. (AC-32b)
- AC-10 (FR-07): IF un cuidador abre a las 23:59 el registro de cumplimiento de la instancia de hoy de "paseo, hora fija 20:00" sin cumplir y lo confirma a las 00:01, THEN THE sistema SHALL rechazar el registro, informarle que la instancia expiró y no crear ningún registro de cumplimiento.
- AC-11 (FR-02): WHEN un cuidador registra el cumplimiento de la instancia de un deber "paseo, hora fija 09:00, solo para hoy", THE sistema SHALL dejar de mostrar ese deber en la configuración de deberes de la mascota (deja de ser un deber activo, FEAT-001a FR-06). (AC-19d adaptado)
- AC-12 (FR-02, FR-05): WHEN a las 10:00 un cuidador consulta el estado del día, después de que a las 09:05 se registró el cumplimiento del único deber de Firulais, "paseo, hora fija 09:00, solo para hoy", THE sistema SHALL mostrar la sección de Firulais con "1 paseo", el último paseo con la fecha de hoy y la hora 09:05 y el mensaje de que no hay tareas pendientes. (AC-65b)

### Registros del día y su eliminación

- AC-13 (FR-08): WHEN hoy a las 09:10 un cuidador registra el cumplimiento de un paseo de Firulais con la hora 09:05, THE sistema SHALL mostrar en la sección de Firulais un registro del día de tipo paseo con la hora 09:05 y la opción de eliminarlo.
- AC-14 (FR-09): WHEN un cuidador elimina uno de los 2 registros de paseo de hoy de Firulais, THE sistema SHALL mostrar "1 paseo" en la sección de Firulais. (AC-37)
- AC-15 (FR-10): IF un cuidador envía una solicitud para eliminar un registro de paseo cargado ayer a las 18:00, sin paseos registrados hoy, THEN THE sistema SHALL rechazar la operación, informarle que solo se pueden eliminar registros del día y seguir mostrando el último paseo con la fecha de ayer y la hora 18:00. (AC-37b)
- AC-16 (FR-11): WHEN a las 10:00 un cuidador elimina el registro de cumplimiento cargado por error a las 08:10 para la instancia de hoy de "comida, hora fija 08:00", THE sistema SHALL volver a mostrar esa instancia en el estado del día marcada como "pendiente". (AC-38)
- AC-17 (FR-12): IF dos cuidadores ven el mismo registro del día y el cuidador 2 intenta eliminarlo después de que el cuidador 1 lo eliminó, THEN THE sistema SHALL rechazar la eliminación del cuidador 2, informarle que el registro ya fue eliminado y mostrar la instancia correspondiente vigente una sola vez.

### Email de vencimiento

- AC-18 (FR-13, NFR-01): WHILE ningún cuidador tiene la aplicación abierta desde las 09:00, con el deber "comida, hora fija 09:00" sin cumplimiento registrado, WHEN llegan las 09:15, THE sistema SHALL entregar al servicio de envío un email de vencimiento dirigido a `ana@ejemplo.com` y a `beto@ejemplo.com` a más tardar a las 09:20. (AC-48)
- AC-19 (FR-13): WHEN llegan las 09:20, con el deber "comida, hora fija 09:00" cuya instancia de hoy se cumplió a las 09:05, THE sistema SHALL no haber enviado ningún email de vencimiento para esa instancia. (AC-49)
- AC-20 (FR-13): WHEN llegan las 23:59, con la instancia de hoy de "paseo, hora fija 09:00" nunca cumplida, THE sistema SHALL haber enviado exactamente un email de vencimiento para esa instancia.
- AC-21 (FR-15): WHEN se envía el email de vencimiento de la instancia de hoy de "comida, hora fija 09:00" de Firulais, THE sistema SHALL incluir en el email "Firulais", el tipo "comida" y la fecha de hoy con la hora 09:00. (AC-64)
- AC-22 (FR-13, FR-11, NFR-01): WHEN a las 10:00 un cuidador elimina el registro cargado por error a las 08:10 para la instancia de hoy de "comida, hora fija 08:00", que por ese registro no generó email a las 08:15, THE sistema SHALL entregar al servicio de envío el email de vencimiento de esa instancia a más tardar a las 10:05. (AC-38b)
- AC-23 (FR-13, FR-11): WHEN a las 10:00 un cuidador elimina el registro cargado por error a las 09:00 para la instancia de hoy de "comida, hora fija 08:00", cuyo email de vencimiento ya se había enviado a las 08:15, THE sistema SHALL no enviar un segundo email de vencimiento para esa instancia en lo que resta del día. (AC-38c)
- AC-24 (FR-14): WHEN hoy a las 10:30 un cuidador da de alta "paseo, hora fija 10:00, solo para hoy" y llegan las 23:59 sin cumplimiento, THE sistema SHALL no haber enviado ningún email de vencimiento para esa instancia. (AC-14)
- AC-25 (FR-14): IF el servidor estuvo detenido entre las 23:00 de ayer y las 10:00 de hoy, con el deber "comida, hora fija 08:00, todos los días", THEN THE sistema SHALL, al volver a estar en ejecución a las 10:00, no enviar email de vencimiento para la instancia de las 08:00 de hoy. (AC-28c)
- AC-26 (FR-16, NFR-01): IF el servicio de envío rechaza todo envío entre las 09:15 y las 09:17, con el deber "comida, hora fija 09:00" sin cumplimiento registrado, THEN THE sistema SHALL lograr que el servicio acepte el email de vencimiento de esa instancia a más tardar a las 09:20. (AC-62)
- AC-27 (FR-16): IF el servicio de envío rechaza todo envío entre las 09:15 y las 09:30, con el deber del AC-26, THEN THE sistema SHALL no intentar enviar el email de vencimiento de esa instancia después de las 09:20. (AC-62b)
- AC-28 (FR-17, FR-13): IF en el escenario del AC-27 un cuidador registra a las 10:00 por error el cumplimiento de esa instancia y lo elimina a las 10:30, THEN THE sistema SHALL entregar al servicio de envío el email de vencimiento de esa instancia a más tardar a las 10:35. (AC-62c)
- AC-29 (FR-18, FR-16, NFR-01): IF el servicio de envío no responde a ningún intento entre las 09:15 y las 09:17, con el deber del AC-26, THEN THE sistema SHALL dar por fallido cada intento a los 30 segundos y lograr que el servicio acepte el email a más tardar a las 09:20. (AC-62d)

### Requerimientos no funcionales

- AC-30 (NFR-04): WHEN un cuidador, en un navegador móvil con un viewport de 360 píxeles de ancho, completa cada uno de estos flujos: (1) registrar un cumplimiento editando la hora, (2) eliminar un registro del día, THE sistema SHALL completar cada flujo sin mostrar scroll horizontal en ninguna de sus pantallas. (AC-59 adaptado)
- AC-31 (NFR-03): WHEN un cuidador, desde el estado del día, registra el cumplimiento de una instancia vigente de paseo aceptando la hora por defecto, THE sistema SHALL dejar de mostrar la instancia habiendo requerido 5 toques/clics o menos. (AC-60)

## Fuera de Alcance

- Todo lo de FEAT-001a, que este ticket da por entregado: datos precargados, alta de deberes, configuración de deberes, generación de instancias, vencimiento y expiración, y el estado del día con su resumen.
- Registro, inicio y cierre de sesión, código de hogar y control de acceso (PRD-001: RF-01a a RF-01l, RF-02 a RF-02d, RF-03, RF-03b, RF-36b). **Cualquiera que llegue a la URL ve y modifica los datos** (riesgo aceptado, ver abajo).
- Deberes de medicación, baño y visita/estudio y sus emails (RF-20, RF-24, RF-28c, RF-28d, RF-30a, RF-30b, RF-33, RF-34, RF-35).
- Dar de baja un deber o una instancia (RF-12a, RF-14 a RF-14e); por lo tanto RF-21d y RF-21e no aplican.
- Editar un registro de cumplimiento (se elimina y se vuelve a registrar, igual que en PRD-001).
- Registrar quién (qué cuidador) hizo cada acción.
- Enviar, al volver el servidor, emails cuya condición se cumplió mientras estaba caído, o emails cuyos reintentos no lograron aceptación dentro de los 5 minutos (se pierden, igual que en PRD-001).
- Todo lo listado en "Fuera de Alcance" de PRD-001.

## Riesgos y Mitigaciones

- **Riesgo aceptado — sin control de acceso:** sin sesión, cualquiera que alcance la URL puede ver y modificar datos, incluido registrar o eliminar cumplimientos (contradice RF-03/RF-03b de PRD-001). Aceptado por el usuario en DEFINE (2026-10-25). Condición: la app se usa **solo en desarrollo local** hasta que se entregue el ticket de registro y sesión; se revisa en ese ticket.
- Riesgo: el envío de email falla o llega tarde y el cuidador deja de confiar en la alarma → mitigación: reintentos durante 5 minutos (FR-16), y la instancia igual se muestra como "pendiente" en la interfaz (FEAT-001a).
- Riesgo: dos cuidadores actúan sobre la misma instancia o el mismo registro a la vez → mitigación: rechazos explícitos (FR-06, FR-12) en lugar de duplicar o perder datos.
- Riesgo: el polling cada pocos segundos genera carga → aceptado para el volumen de un solo hogar (igual que en PRD-001).

## Dependencias

- FEAT-001a: hogar, cuidadores y mascotas precargados, deberes, instancias (con su vencimiento y expiración), registros de cumplimiento como datos y el estado del día.
- Servicio de envío de emails (SMTP vía MailKit, según `AGENTS.md`). En desarrollo los emails se escriben en la consola y no se envían (regla de `AGENTS.md`); las credenciales SMTP no van en `appsettings*.json`.
- Proceso programado en segundo plano que detecte vencimientos y reintente los emails, independiente de que haya cuidadores con la app abierta (FR-13, FR-16, NFR-01).
- Mecanismo de refresco en el cliente (polling con HTMX) para FR-01 / NFR-02.
- SQLite + EF Core 10 para persistir los emails de vencimiento aceptados (FR-17); los cambios de esquema van por migración (regla de `AGENTS.md`).
- `TimeProvider` inyectado como única fuente de la hora (regla de `AGENTS.md`), necesario para FR-03, FR-04, FR-05, FR-07, FR-10 y FR-16.
- PRD de origen: `docs/daw/prd/PRD.md` (PRD-001). PRD padre: `docs/daw/prd/prd-FEAT-001.md`.
