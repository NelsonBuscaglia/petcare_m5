# PRD FEAT-001a: Deberes de comida y paseo: alta, instancias y estado del día

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Tracker | ninguno |
| Fecha | 2026-10-25 |
| PRD loops | 1 |
| PRD padre | `docs/daw/prd/prd-FEAT-001.md` (índice del split) |
| PRD de origen | `docs/daw/prd/PRD.md` (PRD-001); entre paréntesis, el RF/RNF/AC de origen |

## Contexto y Problema

En un hogar donde varias personas cuidan a las mascotas, nadie sabe con certeza si un paseo o una
comida ya se hicieron, y las tareas se duplican o se olvidan (PRD-001, "Contexto y Problema").

Este es el primer sub-ticket de FEAT-001 y la primera entrega de la app. Cubre la mitad "ver" del
problema para los deberes de **comida y paseo** en modalidad **hora fija**: un cuidador los da de
alta desde la configuración de deberes de la mascota, el sistema genera sus instancias (en el
momento del alta, al empezar cada día y al volver a levantarse el servidor), y todos ven en el
estado del día qué falta, qué venció ("pendiente") y el resumen de cada mascota. Registrar
cumplimientos, el email de vencimiento y la actualización automática llegan en FEAT-001b.

El repo está vacío. Registro, sesión y alta de mascotas quedan para tickets posteriores: este
ticket arranca con datos precargados (decisión del usuario en DEFINE).

## Objetivos

- Que cualquier cuidador vea, al abrir o recargar la app, qué comidas y paseos de hoy tiene cada
  mascota y cuáles vencieron hace 15 minutos o más sin cumplirse.
- Que los deberes que se repiten todos los días tengan su instancia del día disponible a más tardar
  5 minutos después de la medianoche (UTC−3), sin que nadie tenga que abrir la app.

## Glosario

Se usan los términos del glosario de PRD-001 (día, cuidador, deber, deber activo, instancia,
instancia vigente, vencida, expirada, registro de cumplimiento, "pendiente", estado del día,
configuración de deberes), restringidos a comida y paseo en modalidad hora fija. Precisión para
este ticket:

- **Deber activo** (comida/paseo, hora fija): un deber "todos los días" siempre está activo (no hay
  bajas); un deber "solo para hoy" está activo mientras su única instancia está vigente.

## Requerimientos Funcionales

Entre paréntesis, el requerimiento de origen en PRD-001.

### Datos precargados

- FR-01: El sistema debe disponer, sin ningún paso de registro, de un único hogar con los cuidadores `ana@ejemplo.com` y `beto@ejemplo.com` y las mascotas Firulais y Michi, sin deberes dados de alta.

### Configuración de deberes

- FR-02: El sistema debe permitir dar de alta, desde la configuración de deberes de una mascota, un deber de tipo comida o paseo en la modalidad hora fija, indicando la hora en formato HH:MM. (RF-06a parcial, RF-11c)
- FR-03: El sistema debe asociar cada deber a una única mascota: la de la configuración de deberes desde la que se dio de alta. (RF-06b)
- FR-04: El sistema debe permitir elegir, al dar de alta un deber, si es válido solo para el día en que se crea o si se repite todos los días. (RF-08)
- FR-05: El sistema debe rechazar el alta de un deber al que le falta el tipo, la hora o la repetición, o cuyo tipo no es comida ni paseo, o cuya hora no es una hora válida en formato HH:MM, informando cuál de esos datos falta o es inválido. (RF-36)
- FR-06: El sistema debe mostrar, en la configuración de deberes de cada mascota, sus deberes activos, cada uno con su tipo, su hora y su repetición. (RF-11b)

### Instancias

- FR-07: Al dar de alta un deber, el sistema debe generar en ese momento la instancia del día del alta, aunque su horario ya haya pasado. (RF-17b)
- FR-08: El sistema debe generar al inicio de cada día la instancia del día de cada deber que se repite todos los días. (RF-17)
- FR-09: Al volver a estar en ejecución el servidor, el sistema debe generar las instancias del día actual que no se hayan generado, sin duplicar las que ya existen. (RF-17c)
- FR-10: El sistema debe considerar vencida, y marcar como "pendiente", toda instancia vigente cuando pasan 15 minutos desde su horario sin que se haya registrado su cumplimiento. (RF-18, RF-29)
- FR-11: El sistema debe dar por expirada, al finalizar su día, toda instancia no cumplida, sin trasladarla al día siguiente. (RF-19)

### Estado del día

- FR-12: El sistema debe mostrar el estado del día como pantalla principal, con una sección por cada mascota del hogar que contenga sus instancias vigentes. (RF-22)
- FR-13: El sistema debe mostrar cada instancia vigente con su tipo, su horario y, si está vencida, la marca "pendiente". (RF-22, RF-29)
- FR-14: El sistema debe mostrar, en la sección de cada mascota, la cantidad de paseos registrados con fecha del día actual. (RF-27a)
- FR-15: El sistema debe mostrar, en la sección de cada mascota, la fecha y la hora real del último cumplimiento de comida registrado, aunque sea de un día anterior, o que no hay ninguno registrado. (RF-27b)
- FR-16: El sistema debe mostrar, en la sección de cada mascota, la fecha y la hora real del último cumplimiento de paseo registrado, aunque sea de un día anterior, o que no hay ninguno registrado. (RF-27c)
- FR-17: Cuando una mascota no tiene instancias vigentes, el sistema debe mostrar igualmente su sección, con los datos de FR-14, FR-15 y FR-16 y un mensaje que indique que no hay tareas pendientes. (RF-38c)

## Requerimientos No Funcionales

- NFR-01: El sistema debe generar las instancias del día de los deberes que se repiten todos los días (FR-08) dentro de los 5 minutos posteriores al inicio del día, sin depender de que haya cuidadores con la aplicación abierta, mientras el servidor esté en ejecución. (RNF-05)
- NFR-02: Las pantallas de este ticket (estado del día, configuración de deberes y alta de deber) deben poder usarse completas desde un navegador móvil con un viewport de 360 píxeles de ancho, sin scroll horizontal. (RNF-02)
- NFR-03: Todas las horas de los deberes, y el inicio y fin de cada día, deben interpretarse en la zona horaria UTC−3 (desfase fijo de −3 horas respecto de UTC, sin horario de verano). (RNF-06)

## Criterios de Aceptación

En formato EARS. Entre paréntesis, los FR/NFR que valida cada uno y, al final, el AC de origen en
PRD-001. En este ticket todavía no hay forma de registrar cumplimientos desde la interfaz: los AC
que parten de registros existentes (AC-21, AC-22) los toman como datos de partida.

### Datos precargados

- AC-01 (FR-01, FR-12, FR-15, FR-16, FR-17): WHEN un cuidador abre la aplicación por primera vez, THE sistema SHALL mostrar el estado del día con exactamente dos secciones, Firulais y Michi, cada una con "0 paseos", la indicación de que no hay comidas ni paseos registrados y el mensaje de que no hay tareas pendientes. (AC-39d)

### Configuración de deberes

- AC-02 (FR-02, FR-04, FR-06): WHEN un cuidador da de alta, desde la configuración de deberes de Firulais, el deber "paseo, hora fija 18:00, todos los días", THE sistema SHALL mostrar ese deber en la configuración de deberes de Firulais con el tipo paseo, la hora 18:00 y la repetición "todos los días". (AC-19f)
- AC-03 (FR-03): WHEN hoy a las 07:00 un cuidador da de alta "paseo, hora fija 09:00, solo para hoy" para Firulais, THE sistema SHALL no mostrar ninguna instancia de ese deber en la sección de Michi del estado del día ni ese deber en la configuración de deberes de Michi. (AC-09)
- AC-04 (FR-05): IF un cuidador intenta dar de alta un deber de comida sin indicar la hora, THEN THE sistema SHALL rechazar el alta, informarle que la hora es obligatoria y no mostrar el deber en la configuración de deberes.
- AC-05 (FR-05): IF un cuidador envía el alta de un deber con la hora "25:00", THEN THE sistema SHALL rechazar el alta, informarle que la hora no es válida y no mostrar el deber en la configuración de deberes.
- AC-06 (FR-05): IF un cuidador envía el alta de un deber con un tipo distinto de comida y paseo (por ejemplo "medicación") o sin tipo, THEN THE sistema SHALL rechazar el alta, informarle que el tipo debe ser comida o paseo y no mostrar el deber en la configuración de deberes.
- AC-07 (FR-05): IF un cuidador envía el alta de un deber sin elegir la repetición, THEN THE sistema SHALL rechazar el alta, informarle que debe elegir "solo para hoy" o "todos los días" y no mostrar el deber en la configuración de deberes.
- AC-08 (FR-06, FR-11): WHEN hoy un cuidador consulta la configuración de deberes de una mascota con un deber "comida, hora fija 08:00, solo para hoy" dado de alta ayer y no cumplido, THE sistema SHALL no mostrar ese deber. (AC-19c)

### Instancias

- AC-09 (FR-07, FR-10): WHEN hoy a las 10:30 un cuidador da de alta "paseo, hora fija 10:00, solo para hoy", THE sistema SHALL mostrar en el estado del día esa instancia marcada como "pendiente". (AC-14)
- AC-10 (FR-04, FR-08): WHEN mañana a las 08:00 un cuidador consulta el estado del día de una mascota con el deber "paseo, hora fija 09:00, todos los días" dado de alta hoy, THE sistema SHALL mostrar una instancia de ese paseo para las 09:00 de mañana. (AC-11)
- AC-11 (FR-04): WHEN mañana a las 08:00 un cuidador consulta el estado del día de una mascota con el deber "comida, hora fija 20:00, solo para hoy" dado de alta hoy, THE sistema SHALL no mostrar ninguna instancia de ese deber. (AC-12)
- AC-12 (FR-08, NFR-01, NFR-03): WHEN un cuidador consulta el estado del día a las 00:05 en UTC−3 (03:05 UTC), con el deber "comida, hora fija 08:00, todos los días", THE sistema SHALL mostrar la instancia de ese deber para las 08:00 del nuevo día. (AC-28)
- AC-13 (FR-08, NFR-03): WHEN un cuidador consulta el estado del día a las 23:59 en UTC−3 (02:59 UTC del día siguiente), con el deber del AC-12, THE sistema SHALL no mostrar ninguna instancia de ese deber para el día siguiente. (AC-28b)
- AC-14 (FR-09): IF el servidor estuvo detenido entre las 23:00 de ayer y las 10:00 de hoy, con el deber "comida, hora fija 08:00, todos los días", THEN THE sistema SHALL, al volver a estar en ejecución a las 10:00, mostrar la instancia de las 08:00 de hoy marcada como "pendiente". (AC-28c)
- AC-15 (FR-09): WHEN el servidor se reinicia hoy a las 10:00 después de haber generado ya la instancia de las 08:00 de hoy del deber "comida, hora fija 08:00, todos los días", THE sistema SHALL seguir mostrando una única instancia de las 08:00 de hoy para ese deber.
- AC-16 (FR-10): WHEN un cuidador abre o recarga el estado del día a las 09:10, con el deber "comida, hora fija 09:00" sin cumplimiento registrado, THE sistema SHALL mostrar esa instancia sin la marca "pendiente". (AC-29)
- AC-17 (FR-10, FR-13): WHEN un cuidador abre o recarga el estado del día a las 09:15, con el deber del AC-16, THE sistema SHALL mostrar esa instancia marcada como "pendiente". (AC-29b)
- AC-18 (FR-11): WHEN hoy a las 07:00 un cuidador consulta el estado del día, con el deber "comida, hora fija 08:00, todos los días" cuya instancia de ayer no se cumplió, THE sistema SHALL no mostrar la instancia de ayer y mostrar la de hoy sin marca. (AC-30)
- AC-19 (FR-06, FR-11): WHEN hoy un cuidador consulta la configuración de deberes de una mascota con el deber "paseo, hora fija 09:00, todos los días" cuya instancia de ayer expiró sin cumplirse, THE sistema SHALL seguir mostrando ese deber.

### Estado del día

- AC-20 (FR-12, FR-13): WHEN a las 12:00 un cuidador consulta el estado del día, con Firulais teniendo "comida, hora fija 08:00, todos los días" y "paseo, hora fija 18:00, todos los días" sin cumplimientos hoy, THE sistema SHALL mostrar en la sección de Firulais la instancia de comida de las 08:00 marcada como "pendiente" y la de paseo de las 18:00 sin marca, y ninguna de ellas en la sección de Michi. (AC-33 adaptado)
- AC-21 (FR-14, FR-15, FR-16): WHEN un cuidador consulta el estado del día, con Firulais teniendo registros de paseo de hoy a las 09:00 y a las 11:00 y su última comida registrada hoy a las 13:00, THE sistema SHALL mostrar en la sección de Firulais "2 paseos", el último paseo con la fecha de hoy y la hora 11:00 y la última comida con la fecha de hoy y la hora 13:00. (AC-39 adaptado)
- AC-22 (FR-14, FR-15, FR-16): WHEN hoy a las 07:00 un cuidador consulta el estado del día, con la última comida de Firulais registrada ayer a las 20:00 y su último paseo ayer a las 21:00 y sin registros hoy, THE sistema SHALL mostrar la última comida con la fecha de ayer y la hora 20:00, el último paseo con la fecha de ayer y la hora 21:00 y "0 paseos". (AC-39c)

### Requerimientos no funcionales

- AC-23 (NFR-02): WHEN un cuidador, en un navegador móvil con un viewport de 360 píxeles de ancho, completa cada uno de estos flujos: (1) consultar el estado del día, (2) consultar la configuración de deberes, (3) dar de alta un deber, THE sistema SHALL completar cada flujo sin mostrar scroll horizontal en ninguna de sus pantallas. (AC-59 adaptado)
- AC-24 (NFR-03, FR-10): WHEN un cuidador consulta el estado del día a las 05:15 en UTC−3 (08:15 UTC), con el deber "comida, hora fija 08:00, todos los días" sin cumplimiento registrado, THE sistema SHALL no mostrar esa instancia como "pendiente". (AC-61)

## Fuera de Alcance

- Todo lo de FEAT-001b: registrar cumplimientos, ver y eliminar registros del día, email de vencimiento con reintentos y actualización automática del estado del día (en este ticket la pantalla se actualiza solo al abrirla o recargarla).
- Registro, inicio y cierre de sesión, código de hogar y control de acceso (PRD-001: RF-01a a RF-01l, RF-02 a RF-02d, RF-03, RF-03b, RF-36b). En este ticket **cualquiera que llegue a la URL ve y modifica los datos** (riesgo aceptado, ver abajo).
- Alta de mascotas y el estado del día sin mascotas (RF-04 a RF-04d, RF-38, RF-38b).
- Máximo de días entre baños (RF-05, RF-05b, RF-05c).
- Modalidad "cada N horas" (RF-06c, RF-07).
- Deberes de medicación, baño y visita/estudio y todo lo que dependa de ellos (RF-09, RF-09b, RF-10, RF-10b, RF-10c, RF-11, RF-11d, RF-20, RF-28a, RF-28c, RF-28d, RF-30a, RF-30b, RF-33).
- Email recordatorio y email de aviso de turno de baño (RF-34, RF-35, RF-37b, RF-37c).
- Dar de baja un deber o una instancia (RF-12a, RF-12b, RF-14 a RF-14e, RF-15, RF-16).
- Registrar quién (qué cuidador) hizo cada acción.
- Todo lo listado en "Fuera de Alcance" de PRD-001.

## Riesgos y Mitigaciones

- **Riesgo aceptado — sin control de acceso:** sin sesión, cualquiera que alcance la URL puede ver y modificar datos (contradice RF-03/RF-03b de PRD-001). Aceptado por el usuario en DEFINE (2026-10-25). Condición: la app se usa **solo en desarrollo local** hasta que se entregue el ticket de registro y sesión; se revisa en ese ticket.
- Riesgo: hasta que llegue FEAT-001b no se pueden registrar cumplimientos, así que toda instancia termina "pendiente" y luego expira → aceptado: este ticket entrega la parte de "ver"; el resumen (FR-14 a FR-16) muestra "no hay registros" mientras no existan.
- Riesgo: si el servidor está caído no se generan instancias → mitigación: al volver se generan las del día que falten (FR-09) y se muestran como "pendiente" si ya vencieron.
- Riesgo: los datos precargados quedan en la base cuando llegue el registro real → a resolver en el ticket de registro y sesión.

## Dependencias

- Proceso programado en segundo plano que genere las instancias diarias, independiente de que haya cuidadores con la app abierta (FR-08, FR-09, NFR-01).
- SQLite + EF Core 10 para persistir hogar, cuidadores, mascotas, deberes, instancias y registros de cumplimiento; los datos precargados (FR-01) llegan por migración, sin tocar la base por fuera de ellas (regla de `AGENTS.md`).
- `TimeProvider` inyectado como única fuente de la hora (regla de `AGENTS.md`), necesario para FR-07, FR-08, FR-10, FR-11 y NFR-03.
- Reloj del servidor sincronizado con la hora oficial (igual que en PRD-001).
- PRD de origen: `docs/daw/prd/PRD.md` (PRD-001). PRD padre: `docs/daw/prd/prd-FEAT-001.md`.
