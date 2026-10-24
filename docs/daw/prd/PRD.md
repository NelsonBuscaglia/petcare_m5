# PRD-001: PetCare Hogar — seguimiento colaborativo del cuidado de mascotas en la casa

## Contexto y Problema

En hogares donde más de una persona se ocupa del cuidado de una o más mascotas, no existe visibilidad compartida sobre qué tareas de cuidado (paseo, comida, medicación, baño, visita al veterinario o estudios) ya se realizaron. Esto genera dos problemas recurrentes:

- **Tareas duplicadas**: dos personas sacan a pasear al mismo perro sin saberlo.
- **Tareas olvidadas**: todos asumen que otra persona ya se ocupó, y la tarea termina sin hacerse.

Hoy la única forma de resolverlo es preguntando directamente a los demás integrantes del hogar, lo cual es poco confiable y depende de que todos estén disponibles para responder.

**Personas:**
- **Cuidador**: integrante del hogar a cargo de mascotas, que convive con una o más mascotas y comparte responsabilidades del cuidado con otros integrantes del mismo hogar. Necesita, en cualquier momento, saber qué se hizo y qué falta sin tener que preguntarle a nadie.

## Objetivos

- Que cualquier integrante del hogar pueda ver, sin preguntar, el estado actual de cuidado de cada mascota — y que ese estado sea confiable en el momento en que otro integrante está por actuar, no solo cuando refresca la página.
- Que las tareas vencidas o próximas a vencer generen una alarma por email a los integrantes del hogar, para que se hagan cargo del olvido.

## Glosario

- **Día:** período de 00:00 a 23:59 en UTC−3 (RNF-06).
- **Código de hogar:** código que el sistema genera al crear el hogar y que una persona debe ingresar al registrarse para sumarse a él (RF-01c, RF-01d, RF-01e, RF-01g).
- **Cuidador:** persona registrada y asociada al hogar. Todos los cuidadores ven y modifican todos los datos del hogar, incluidos los cargados por otros cuidadores (RF-01a, RF-01b, RF-01c).
- **Deber de cuidado (o deber):** tarea de cuidado de una mascota, tal como fue dada de alta (RF-06a, RF-10).
- **Deber programable:** deber de comida, paseo o medicación, con uno o más horarios por día (RF-06a).
- **Deber puntual:** deber de baño o visita/estudio, con una única fecha y hora (RF-10, RF-10b, RF-10c).
- **Deber activo:** deber no dado de baja que tiene, o todavía puede generar, al menos una instancia vigente (RF-11b, RF-35).
- **Instancia:** cada ocurrencia de un deber en un horario concreto (RF-07, RF-17, RF-17b, RF-17c).
- **Instancia vigente:** instancia que no fue cumplida, no expiró y no fue dada de baja (RF-19, RF-20, RF-22).
- **Instancia vencida:** instancia vigente cuyo plazo de cumplimiento ya pasó (RF-18, RF-30a).
- **Instancia expirada:** instancia de comida o paseo que dejó de ser vigente al terminar su día sin cumplirse (RF-19).
- **Registro de cumplimiento:** constancia de que una instancia se cumplió (RF-21, RF-21b, RF-23, RF-23b, RF-24).
- **"Pendiente":** marca de una instancia vencida de comida, paseo o medicación (RF-29).
- **"No cumplido":** marca de una instancia vencida de baño o visita/estudio (RF-30a, RF-30b).
- **Estado del día:** vista con el resumen del día y las instancias vigentes de todas las mascotas del hogar, con una sección por mascota. Es la pantalla principal, la que se muestra al iniciar sesión (RF-22, RF-27a, RF-27b, RF-27c).
- **Configuración de deberes:** vista de cada mascota con sus deberes activos y su máximo de días entre baños, desde donde se dan de alta y de baja los deberes (RF-05, RF-05b, RF-11b, RF-11c, RF-12a).
- **Dar de baja / eliminar:** "dar de baja" se usa para deberes e instancias (RF-12a, RF-14); "eliminar", para registros de cumplimiento (RF-25).
- **Email de vencimiento:** email que avisa que una instancia venció (RF-32, RF-32b, RF-33).
- **Email recordatorio:** email que avisa de un deber puntual próximo (RF-34).
- **Email de aviso de turno de baño:** email que indica que hay que pedir turno de baño para una mascota (RF-35).

## Requerimientos Funcionales

### Registro, sesión y acceso

- RF-01a: El sistema debe permitir que una persona se registre con email y contraseña.
- RF-01b: Cuando se registra la primera persona, el sistema debe crear el hogar.
- RF-01d: Al crear el hogar, el sistema debe generar un código de hogar.
- RF-01i: El sistema debe generar el código de hogar con 8 caracteres elegidos al azar entre letras mayúsculas y dígitos, excluyendo 0, O, 1, I y L.
- RF-01e: El sistema debe mostrarle el código de hogar a la primera persona al terminar su registro.
- RF-01f: El sistema debe rechazar el registro de una persona con un email que ya pertenece a un cuidador registrado, comparando los emails sin distinguir mayúsculas de minúsculas.
- RF-01g: El sistema debe permitir que un cuidador con sesión iniciada consulte el código de hogar.
- RF-01c: Cuando el hogar ya existe, el sistema debe asociar a la nueva persona al hogar solo si al registrarse ingresa el código de hogar correcto.
- RF-01j: El sistema debe comparar el código de hogar ingresado al registrarse sin distinguir mayúsculas de minúsculas.
- RF-01k: Cuando dos personas completan su registro al mismo tiempo sin que exista el hogar, el sistema debe crear el hogar solo con el primer registro que se complete y evaluar el otro como el de una persona que se suma a un hogar ya existente (RF-01c).
- RF-01l: Cuando el hogar ya existe y el código de hogar ingresado es incorrecto o falta, el sistema debe rechazar el registro informando solo ese motivo, aunque el email ya pertenezca a un cuidador registrado.
- RF-01h: El sistema debe rechazar el registro de una persona con una contraseña de menos de 8 caracteres.
- RF-02: El sistema debe permitir que un cuidador registrado inicie sesión con su email y contraseña, comparando el email sin distinguir mayúsculas de minúsculas.
- RF-02b: El sistema debe permitir que un cuidador con sesión iniciada cierre su sesión.
- RF-02c: El sistema debe cerrar la sesión de un cuidador cuando pasan 30 días desde la última solicitud que hizo con esa sesión, sin contar las solicitudes de actualización automática del estado del día (RF-31).
- RF-02d: Al rechazar un inicio de sesión, el sistema debe informar que el email o la contraseña son incorrectos, sin indicar cuál de los dos.
- RF-03: El sistema debe impedir que una persona sin sesión iniciada vea los datos del hogar (mascotas, deberes y registros de cumplimiento de cuidado).
- RF-03b: El sistema debe impedir que una persona sin sesión iniciada modifique los datos del hogar (mascotas, deberes y registros de cumplimiento de cuidado).

### Mascotas

- RF-04: El sistema debe permitir registrar una o más mascotas en el hogar, indicando su nombre.
- RF-04b: El sistema debe rechazar el registro de una mascota con un nombre que ya pertenece a otra mascota del hogar, comparando los nombres sin distinguir mayúsculas de minúsculas.
- RF-04c: El sistema debe rechazar el registro de una mascota cuyo nombre esté vacío o tenga solo espacios.
- RF-04d: El sistema debe quitar los espacios al principio y al final del nombre de una mascota al registrarla, tanto para guardarlo como para compararlo con los nombres existentes (RF-04b).
- RF-05: El sistema debe permitir definir de forma opcional, desde la configuración de deberes de cada mascota, la cantidad máxima de días esperados entre un baño y el siguiente, sin necesidad de dar de alta un deber de cuidado.
- RF-05b: El sistema debe permitir modificar, desde la configuración de deberes de cada mascota, la cantidad máxima de días entre baños ya definida (RF-05).
- RF-05c: El sistema debe rechazar, al definir (RF-05) o modificar (RF-05b) el máximo de días entre baños, un valor que no sea un número entero mayor que 0.

### Deberes

- RF-06a: El sistema debe permitir dar de alta un deber de cuidado programable de tipo comida, paseo o medicación, en una de estas dos modalidades: (a) hora fija, o (b) cada N horas a partir de una hora de inicio elegida por el cuidador.
- RF-06b: El sistema debe asociar cada deber de cuidado a una única mascota.
- RF-06c: El sistema debe rechazar, en la modalidad "cada N horas", un valor de N que no sea un entero entre 1 y 23.
- RF-08: El sistema debe permitir elegir, al dar de alta un deber de tipo comida, paseo o medicación, si es válido solo para el día en que se crea o si se repite todos los días.
- RF-09: El sistema debe permitir elegir, para un deber de medicación que se repite todos los días, entre "para siempre" o "hasta una fecha" de fin, inclusive (ese día se genera la última instancia).
- RF-09b: El sistema debe rechazar una fecha de fin (RF-09) anterior al día actual.
- RF-10: El sistema debe permitir dar de alta un deber de cuidado puntual de tipo baño o visita/estudio para una mascota, indicando fecha y hora exactas, sin opción de repetición ni recurrencia.
- RF-10b: El sistema debe rechazar el alta de un deber de tipo baño o visita/estudio con una fecha anterior al día actual.
- RF-10c: El sistema debe permitir dar de alta un deber de tipo baño o visita/estudio para el día actual con una hora ya pasada.
- RF-11: El sistema debe permitir elegir, al dar de alta un deber de tipo visita/estudio, un subtipo: veterinario, estudio, vacuna o intervención médica.
- RF-11d: El sistema debe rechazar el alta de un deber de tipo visita/estudio sin subtipo (RF-11).
- RF-11b: El sistema debe mostrar, en la configuración de deberes de cada mascota, sus deberes activos, cada uno con los datos con que fue dado de alta.
- RF-11c: El sistema debe permitir dar de alta los deberes de una mascota (RF-06a, RF-10) desde su configuración de deberes.
- RF-12a: El sistema debe permitir dar de baja un deber de cuidado desde la configuración de deberes de su mascota, en forma definitiva, sin posibilidad de reactivarlo.
- RF-12b: Al dar de baja un deber, el sistema debe dar de baja también todas sus instancias que no tengan su cumplimiento registrado.
- RF-15: Al dar de baja un deber que se repite todos los días, el sistema debe dejar de generar nuevas instancias de ese deber a partir de ese momento.
- RF-16: Al dar de baja un deber que se repite todos los días, el sistema debe conservar sin cambios los registros de cumplimiento de cuidado ya cargados para ese deber.

### Instancias

- RF-07: Para un deber en la modalidad "cada N horas a partir de una hora de inicio", el sistema debe generar una instancia para cada horario que resulta de sumar N horas a la hora de inicio dentro del mismo día (por ejemplo, cada 4 horas desde las 08:00: 08:00, 12:00, 16:00 y 20:00), cada una con su propio horario, independiente de cuándo se cumplió la anterior.
- RF-17: El sistema debe generar al inicio de cada día las instancias del día de cada deber de tipo comida, paseo o medicación que se repite todos los días: una en la modalidad de hora fija, y una por cada horario resultante (RF-07) en la modalidad cada N horas.
- RF-17b: Al dar de alta un deber de tipo comida, paseo o medicación, el sistema debe generar en ese momento las instancias del día del alta, incluidas las de horarios ya pasados.
- RF-17c: Al volver a estar en ejecución el servidor, el sistema debe generar las instancias del día actual (RF-17) que no se hayan generado.
- RF-18: El sistema debe considerar vencida una instancia de un deber de tipo comida, paseo o medicación cuando pasan 15 minutos desde su horario sin que se haya registrado su cumplimiento.
- RF-19: El sistema debe dar por expirada, al finalizar el día al que corresponde, toda instancia no cumplida de un deber de tipo comida o paseo, sin trasladarla al día siguiente.
- RF-20: El sistema debe mantener vigente en los días posteriores toda instancia de un deber de tipo medicación que no se cumplió en su día, sin darla por expirada, hasta que se registre su cumplimiento, se dé de baja la instancia (RF-14) o se dé de baja el deber (RF-12a).
- RF-29: El sistema debe marcar como "pendiente" toda instancia vigente de un deber de comida, paseo o medicación que esté vencida (RF-18), hasta que se registre su cumplimiento, se dé de baja la instancia (RF-14), se dé de baja el deber (RF-12a) o, en el caso de comida y paseo, la instancia expire al terminar su día (RF-19).
- RF-30a: El sistema debe marcar como "no cumplido" toda instancia vigente de un deber de tipo baño o visita/estudio cuya fecha y hora se superaron.
- RF-30b: El sistema debe mantener en los días posteriores la marca "no cumplido" de una instancia marcada según RF-30a, hasta que se registre su cumplimiento, se dé de baja la instancia (RF-14) o se dé de baja el deber (RF-12a).
- RF-14: El sistema debe permitir dar de baja, desde el estado del día, cualquier instancia vigente.
- RF-14b: Al dar de baja la instancia de un deber que tiene una única instancia (baño, visita/estudio, o comida, paseo o medicación de hora fija "solo para hoy"), el sistema debe dar de baja también el deber (RF-12a).
- RF-14c: Al dar de baja una instancia de un deber distinto de los mencionados en RF-14b, el sistema debe conservar el deber y sus demás instancias.
- RF-14d: El sistema debe rechazar la baja de una instancia que ya tiene su cumplimiento registrado o que ya fue dada de baja, sin modificarla.
- RF-14e: Al rechazar una baja según RF-14d, el sistema debe informar al cuidador si la instancia ya fue cumplida o ya fue dada de baja.

### Registro de cumplimiento

- RF-21: El sistema debe permitir registrar, desde el estado del día de una mascota, el cumplimiento de cualquiera de sus instancias vigentes.
- RF-21b: El sistema debe rechazar el registro de cumplimiento de una instancia que ya tiene su cumplimiento registrado, sin crear un segundo registro.
- RF-21c: Al rechazar un registro según RF-21b, el sistema debe informar al cuidador que la instancia ya fue cumplida.
- RF-21d: El sistema debe rechazar el registro de cumplimiento de una instancia que ya fue dada de baja, sin crear el registro.
- RF-21e: Al rechazar un registro según RF-21d, el sistema debe informar al cuidador que la instancia ya fue dada de baja.
- RF-23: Al registrar el cumplimiento de una instancia de un deber de tipo comida, paseo o medicación, el sistema debe pedir la hora real de cumplimiento, mostrando la hora actual como valor por defecto y permitiendo editarla.
- RF-23b: El sistema debe rechazar una hora real de cumplimiento (RF-23) posterior a la hora actual.
- RF-23c: El sistema debe guardar como fecha de un registro de cumplimiento el día en que se registra.
- RF-24: Al registrar el cumplimiento de una instancia de un deber de tipo baño o visita/estudio, el sistema debe guardar solo la fecha, sin pedir hora.
- RF-25: El sistema debe permitir eliminar un registro de cumplimiento de cuidado cuya fecha (RF-23c) es el día actual, aunque corresponda a una instancia de un día anterior, por ejemplo si se cargó en la mascota o el deber equivocado, o con una hora incorrecta.
- RF-26: Al eliminar un registro de cumplimiento de cuidado, el sistema debe volver a dejar vigente la instancia correspondiente, sin afectar al deber.

### Estado del día

- RF-22: El sistema debe mostrar, en el estado del día de cada mascota, sus instancias vigentes: las de comida, paseo o medicación del día, las de medicación que siguen vigentes desde días anteriores, y las de baño o visita/estudio agendadas para hoy o para días anteriores.
- RF-27a: El sistema debe mostrar, en el estado del día de cada mascota, la cantidad de paseos realizados en el día.
- RF-27b: El sistema debe mostrar, en el estado del día de cada mascota, la fecha y la hora real (RF-23) del último cumplimiento de comida registrado, aunque sea de un día anterior, o que no hay ninguno registrado.
- RF-27c: El sistema debe mostrar, en el estado del día de cada mascota, la fecha y la hora real (RF-23) del último cumplimiento de paseo registrado, aunque sea de un día anterior, o que no hay ninguno registrado.
- RF-28a: El sistema debe mostrar, en el estado del día de la mascota (RF-22), el subtipo de cada instancia de un deber de tipo visita/estudio.
- RF-31: El sistema debe actualizar automáticamente el estado del día, sin que el cuidador recargue la página, cuando cambia cualquier dato que se muestra en él: porque otro cuidador del hogar realiza una acción (por ejemplo, registra o elimina un cumplimiento, da de alta o de baja un deber o una instancia, o da de alta una mascota) o porque una instancia pasa a estar vencida ("pendiente" o "no cumplido").
- RF-38: Cuando el hogar no tiene mascotas registradas, el sistema debe mostrar en el estado del día un mensaje que lo indique.
- RF-38b: Cuando el hogar no tiene mascotas registradas, el sistema debe ofrecer desde el estado del día la opción de registrar una mascota (RF-04).
- RF-38c: Cuando una mascota no tiene instancias vigentes, el sistema debe mostrar igualmente su sección en el estado del día, con los datos de RF-27a, RF-27b y RF-27c y un mensaje que indique que no hay tareas pendientes.

### Emails

- RF-32: El sistema debe enviar un email de vencimiento a todos los cuidadores del hogar cuando una instancia de un deber de comida, paseo o medicación pase al estado vencida (RF-18) y todavía no se haya enviado (RF-32d) un email de vencimiento para esa instancia, sin depender de que haya un cuidador con la aplicación abierta en ese momento.
- RF-32b: El sistema no debe enviar email de vencimiento (RF-32, RF-33) para una instancia que ya estaba vencida en el momento en que se generó.
- RF-33: El sistema debe enviar un email de vencimiento a todos los cuidadores del hogar cuando una instancia de un deber de tipo baño o visita/estudio pase al estado vencida (se superaron su fecha y hora sin cumplimiento registrado) y todavía no se haya enviado (RF-32d) un email de vencimiento para esa instancia, sin depender de que haya un cuidador con la aplicación abierta en ese momento.
- RF-28c: El sistema debe incluir el subtipo de un deber de tipo visita/estudio en el email de vencimiento de ese deber (RF-33).
- RF-34: El sistema debe enviar a todos los cuidadores del hogar un único email recordatorio 48 horas antes de la fecha y hora agendadas de un deber de tipo baño o visita/estudio, siempre que el deber se haya dado de alta con al menos 48 horas de anticipación y no se haya dado de baja antes.
- RF-28d: El sistema debe incluir el subtipo de un deber de tipo visita/estudio en el email recordatorio de ese deber (RF-34).
- RF-35: El sistema debe enviar todos los días a las 07:00, por cada mascota que en ese momento cumpla estas tres condiciones a la vez, un email de aviso de turno de baño a todos los cuidadores del hogar, indicando que debe solicitarse turno de baño para esa mascota: (a) la mascota tiene definido un máximo de días entre baños (RF-05); (b) transcurrieron más días que ese máximo desde el último baño registrado o, si no hay ninguno, desde el alta de la mascota, contando días calendario en UTC−3 entre esa fecha y el día actual; y (c) no existe ningún deber de baño activo para esa mascota.
- RF-32c: Cuando el servicio de envío de emails rechaza o no responde un email de vencimiento, recordatorio o aviso de turno de baño (RF-32, RF-33, RF-34, RF-35), el sistema debe reintentar su envío hasta que el servicio lo acepte o hasta que pasen 5 minutos desde que se cumplió la condición que lo dispara (RNF-01).
- RF-32d: El sistema debe considerar enviado un email de vencimiento, recordatorio o aviso de turno de baño solo cuando el servicio de envío de emails lo aceptó.
- RF-32e: El sistema debe considerar fallido un intento de envío al que el servicio de envío de emails no responde dentro de los 30 segundos.
- RF-37: El sistema debe incluir en el email de vencimiento el nombre de la mascota, el tipo de deber y la fecha y hora de la instancia vencida.
- RF-37b: El sistema debe incluir en el email recordatorio el nombre de la mascota, el tipo de deber y la fecha y hora agendadas.
- RF-37c: El sistema debe incluir en el email de aviso de turno de baño el nombre de la mascota, su máximo de días entre baños y la fecha del último baño registrado o, si no hay ninguno, que no hay baños registrados.

### Rechazos

- RF-36: Al rechazar cualquier operación, salvo un inicio de sesión (RF-02d), el sistema debe informar a quien la intentó el motivo concreto del rechazo.
- RF-36b: Al rechazar una operación por falta de sesión iniciada, el sistema debe informar el motivo en la pantalla de inicio de sesión con el mensaje "Iniciá sesión para continuar."

## Requerimientos No Funcionales

- RNF-01: El sistema debe entregar al servicio de envío de emails cualquier email de alarma o recordatorio (vencimiento, recordatorio de 48 horas antes, o aviso de turno de baño) dentro de los 5 minutos posteriores a que se cumpla la condición que lo dispara, incluida su detección, sin depender de que haya cuidadores con la aplicación abierta, en todos los casos, mientras el servidor esté en ejecución, salvo que el servicio de envío de emails rechace o no responda durante toda esa ventana (RF-32c). La demora del servicio de email en llegar al destinatario queda fuera de esta medición.
- RNF-02: La interfaz debe poder usarse completa desde un navegador móvil con un viewport de 360 píxeles de ancho o más: sin scroll horizontal y con todas las funciones disponibles, sin requerir instalación de una app.
- RNF-03: Registrar el cumplimiento de una instancia debe requerir 5 toques/clics o menos, contados desde la pantalla principal (el estado del día) hasta la confirmación del registro inclusive, aceptando la hora por defecto que propone el sistema (RF-23).
- RNF-04: La actualización automática del estado del día descrita en RF-31 debe reflejarse en los cuidadores con la aplicación abierta dentro de los 15 segundos posteriores al cambio que la origina, en todos los casos, mientras el servidor esté en ejecución.
- RNF-05: El sistema debe generar las instancias del día de los deberes que se repiten todos los días (RF-17) dentro de los 5 minutos posteriores al inicio del día, sin depender de que haya cuidadores con la aplicación abierta, en todos los casos, mientras el servidor esté en ejecución.
- RNF-06: Todas las horas de los deberes, y el inicio y fin de cada día, deben interpretarse en la zona horaria UTC−3, sin horario de verano.

## Criterios de Aceptación

### Registro, sesión y acceso

- AC-01 (RF-01a, RF-01b, RF-01d, RF-01e): Dado que no existe ningún cuidador todavía, cuando una persona se registra con email y contraseña, entonces el sistema crea un nuevo hogar y le muestra el código de hogar.
- AC-01b (RF-01g): Dado un hogar con su código y un segundo cuidador registrado con ese código, cuando ese segundo cuidador, con sesión iniciada, consulta el código de hogar, entonces ve el mismo código que se le mostró a la primera persona al registrarse.
- AC-01c (RF-01i): Dado que no existe ningún cuidador todavía, cuando una persona se registra y se crea el hogar, entonces el código de hogar que se le muestra tiene exactamente 8 caracteres, todos letras mayúsculas o dígitos, y ninguno es 0, O, 1, I ni L.
- AC-02 (RF-01c): Dado un hogar ya existente con su código y la mascota A registrada por la primera persona, cuando una segunda persona se registra con email, contraseña y ese código, entonces al iniciar sesión ve en el estado del día una sección para la mascota A.
- AC-02b (RF-01c): Dado un hogar ya existente, cuando una persona intenta registrarse con email, contraseña y un código de hogar incorrecto, entonces el sistema rechaza el registro, no registra a la persona y no le muestra ningún dato del hogar.
- AC-02c (RF-01c): Dado un hogar ya existente, cuando una persona intenta registrarse con email y contraseña pero sin código de hogar, entonces el sistema rechaza el registro, no registra a la persona y no le muestra ningún dato del hogar.
- AC-02d (RF-01f): Dado un cuidador registrado con el email ana@ejemplo.com, cuando otra persona intenta registrarse con ese mismo email, otra contraseña y el código de hogar correcto, entonces el sistema rechaza el registro y no registra a la persona.
- AC-02e (RF-01f): Dado un cuidador registrado con el email ana@ejemplo.com, cuando otra persona intenta registrarse con el email Ana@Ejemplo.com, otra contraseña y el código de hogar correcto, entonces el sistema rechaza el registro y no registra a la persona.
- AC-02f (RF-01h, RF-36): Dado un hogar ya existente, cuando una persona intenta registrarse con el código correcto y la contraseña "abc1234" (7 caracteres), entonces el sistema rechaza el registro, no registra a la persona y le informa que la contraseña debe tener al menos 8 caracteres.
- AC-02g (RF-01h): Dado un hogar ya existente, cuando una persona se registra con el código correcto y la contraseña "abcd1234" (8 caracteres), entonces queda registrada y al iniciar sesión ve el estado del día.
- AC-02h (RF-01j): Dado un hogar con el código "ABCD2345", cuando una persona se registra ingresando el código "abcd2345", entonces queda registrada en el hogar.
- AC-02i (RF-01k, RF-36): Dado que no existe ningún cuidador todavía, cuando dos personas envían al mismo tiempo su registro sin código de hogar, entonces exactamente una de ellas queda registrada, existe un único hogar, y el registro de la otra se rechaza informando que el hogar ya existe y que se necesita su código.
- AC-02j (RF-01l): Dado un cuidador registrado con el email ana@ejemplo.com, cuando otra persona intenta registrarse con ese mismo email y un código de hogar incorrecto, entonces el sistema rechaza el registro y le informa que el código de hogar es incorrecto, sin indicar que el email ya está registrado.
- AC-03 (RF-02): Dado un cuidador registrado con email y contraseña, cuando inicia sesión con esos mismos datos, entonces ve el estado del día.
- AC-03b (RF-02): Dado un cuidador registrado con el email ana@ejemplo.com, cuando inicia sesión con el email Ana@Ejemplo.com y su contraseña, entonces ve el estado del día.
- AC-04 (RF-02): Dado un cuidador registrado, cuando intenta iniciar sesión con una contraseña incorrecta, entonces el sistema rechaza el inicio de sesión y no muestra ningún dato del hogar.
- AC-04f (RF-02d): Dado un cuidador registrado con el email ana@ejemplo.com, cuando una persona intenta iniciar sesión con ese email y una contraseña incorrecta, y luego con el email no registrado beto@ejemplo.com, entonces el sistema rechaza ambos intentos con el mismo mensaje, que indica que el email o la contraseña son incorrectos.
- AC-04c (RF-02c): Dado un cuidador que hizo su última solicitud con su sesión el día 1 a las 10:00, cuando el día 31 a las 10:01 intenta ver el estado del día desde ese mismo navegador, entonces el sistema lo rechaza sin mostrar ningún dato y le pide iniciar sesión.
- AC-04d (RF-02c): Dado un cuidador que inició sesión el día 1 y consultó el estado del día el día 20 a las 10:00, cuando el día 45 a las 10:00 consulta el estado del día desde ese mismo navegador, entonces lo ve sin volver a iniciar sesión.
- AC-04e (RF-02c, RF-31): Dado un cuidador que hizo su última solicitud el día 1 a las 10:00 y dejó abierto el estado del día desde entonces, cuando el día 31 a las 10:01 la pantalla envía su solicitud de actualización automática, entonces el sistema la rechaza sin devolver ningún dato.
- AC-04b (RF-02b): Dado un cuidador con sesión iniciada, cuando cierra su sesión, entonces cualquier intento posterior de ver el estado de una mascota desde ese mismo navegador es rechazado sin mostrar ningún dato, hasta que vuelva a iniciar sesión.
- AC-05 (RF-03): Dada una persona sin sesión iniciada, cuando envía una solicitud para ver el estado de una mascota del hogar, incluida la solicitud de actualización automática (RF-31), entonces el sistema le niega el acceso y no devuelve ningún dato.
- AC-05c (RF-03): Dada una persona sin sesión iniciada, cuando envía una solicitud para ver la configuración de deberes de una mascota del hogar, entonces el sistema le niega el acceso y no devuelve ningún dato.
- AC-05b (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para registrar el cumplimiento de una instancia de un deber de una mascota del hogar, entonces el sistema rechaza la operación y no se crea ningún registro de cumplimiento.
- AC-05d (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para registrar una mascota, entonces el sistema rechaza la operación y no se crea ninguna mascota.
- AC-05e (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para dar de alta un deber de una mascota del hogar, entonces el sistema rechaza la operación y no se crea ningún deber.
- AC-05f (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para dar de baja un deber activo, entonces el sistema rechaza la operación y el deber sigue apareciendo en la configuración de deberes.
- AC-05g (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para dar de baja una instancia vigente, entonces el sistema rechaza la operación y la instancia sigue apareciendo en el estado del día.
- AC-05h (RF-03b): Dada una persona sin sesión iniciada, cuando envía una solicitud para eliminar un registro de cumplimiento del día, entonces el sistema rechaza la operación y el registro se conserva.
- AC-05i (RF-03b): Dada una mascota con un máximo de 20 días entre baños, cuando una persona sin sesión iniciada envía una solicitud para cambiarlo a 30, entonces el sistema rechaza la operación y la configuración de deberes de esa mascota sigue mostrando un máximo de 20.
- AC-05j (RF-01g): Dada una persona sin sesión iniciada, cuando envía una solicitud para consultar el código de hogar, entonces el sistema le niega el acceso y no devuelve el código.
- AC-05k (RF-36b, RF-03): Dada una persona sin sesión iniciada, cuando solicita ver el estado del día, entonces el sistema no le devuelve ningún dato del hogar y le muestra la pantalla de inicio de sesión con el mensaje "Iniciá sesión para continuar."

### Mascotas

- AC-06 (RF-04): Dado un hogar sin mascotas registradas, cuando se registra una mascota con nombre, entonces el estado del día muestra una sección para esa mascota con su nombre.
- AC-06b (RF-04b): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar otra mascota con el nombre Firulais, entonces el sistema rechaza el registro y el estado del día sigue mostrando una sola sección para Firulais.
- AC-06c (RF-04b): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar otra mascota con el nombre FIRULAIS, entonces el sistema rechaza el registro y el estado del día sigue mostrando una sola sección para Firulais.
- AC-06d (RF-04c): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar una mascota con el nombre vacío, entonces el sistema rechaza el registro y el estado del día sigue mostrando una sola sección, la de Firulais.
- AC-06e (RF-04c): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar una mascota con un nombre formado solo por tres espacios, entonces el sistema rechaza el registro y el estado del día sigue mostrando una sola sección, la de Firulais.
- AC-06f (RF-04d): Dado un hogar sin mascotas registradas, cuando un cuidador registra una mascota con el nombre "  Firulais  " (con espacios al principio y al final), entonces el estado del día muestra una sección con el nombre "Firulais", sin espacios.
- AC-06g (RF-04d, RF-04b): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar otra mascota con el nombre "Firulais " (con un espacio al final), entonces el sistema rechaza el registro y el estado del día sigue mostrando una sola sección para Firulais.
- AC-08 (RF-05): Dada una mascota sin ningún deber de cuidado dado de alta, cuando un cuidador define 20 días como máximo entre baños y guarda, entonces la configuración de deberes de esa mascota muestra un máximo de 20.
- AC-08b (RF-05c): Dada una mascota sin máximo de días entre baños definido, cuando un cuidador intenta definir 0 como máximo y guarda, entonces el sistema rechaza la operación y la configuración de deberes de esa mascota sigue sin máximo definido.
- AC-07 (RF-05b): Dada una mascota con un máximo de 20 días entre baños, cuando un cuidador lo cambia a 30 y guarda, entonces la configuración de deberes de esa mascota muestra un máximo de 30.
- AC-07b (RF-05c): Dada una mascota con un máximo de 20 días entre baños, cuando un cuidador intenta cambiarlo a −5 y guarda, entonces el sistema rechaza la operación y la configuración de deberes de esa mascota sigue mostrando un máximo de 20.

### Deberes

- AC-09 (RF-06b): Dado un deber de "paseo, cada 4 horas a partir de las 08:00" dado de alta hoy para la mascota A, cuando un cuidador consulta el estado del día de la mascota B, entonces no aparece ninguna instancia de ese deber.
- AC-10 (RF-06a, RF-07): Dado un deber de "paseo cada 4 horas a partir de las 08:00, repetir todos los días", cuando un cuidador consulta el estado del día de la mascota a las 07:00, entonces se muestran exactamente cuatro instancias de ese deber: 08:00, 12:00, 16:00 y 20:00.
- AC-10b (RF-06c): Dado el alta de un deber de paseo en la modalidad "cada N horas", cuando un cuidador ingresa N = 0, entonces el sistema rechaza el alta y el deber no aparece en la configuración de deberes.
- AC-10c (RF-06c): Dado el alta de un deber de paseo en la modalidad "cada N horas", cuando un cuidador ingresa N = 24, entonces el sistema rechaza el alta y el deber no aparece en la configuración de deberes.
- AC-11 (RF-06a, RF-08, RF-17): Dado un deber de "paseo, hora fija 09:00, repetir todos los días" dado de alta hoy para una mascota, cuando un cuidador consulta el estado del día de esa mascota mañana a las 08:00, entonces se muestra una instancia de ese paseo para las 09:00 de mañana.
- AC-12 (RF-08): Dado un deber de "comida, hora fija 20:00, solo para hoy" dado de alta hoy para una mascota, cuando un cuidador consulta mañana a las 08:00 el estado del día de esa mascota, entonces no aparece ninguna instancia de ese deber.
- AC-13 (RF-09): Dado un deber de "medicación, hora fija 09:00, repetir todos los días, hasta el día 10", cuando un cuidador consulta el día 10 a las 08:00 el estado del día, entonces se muestra la instancia de ese deber para las 09:00 del día 10.
- AC-13b (RF-09): Dado el mismo deber del AC-13, con la instancia del día 10 cumplida, cuando un cuidador consulta el día 11 a las 08:00 el estado del día, entonces no aparece ninguna instancia de ese deber.
- AC-13c (RF-09, RF-20): Dado el mismo deber del AC-13, con la instancia del día 10 sin cumplir, cuando un cuidador consulta el día 11 a las 08:00 el estado del día, entonces aparece la instancia del día 10 marcada como "pendiente" y no aparece ninguna instancia del día 11.
- AC-13d (RF-09b): Dado el día de hoy, cuando un cuidador intenta dar de alta una medicación que se repite todos los días con fecha de fin ayer, entonces el sistema rechaza el alta y el deber no aparece en la configuración de deberes.
- AC-15 (RF-09): Dado un deber de "medicación, hora fija 09:00, repetir todos los días, para siempre" dado de alta hace 30 días, cuando un cuidador consulta hoy a las 08:00 el estado del día, entonces se muestra la instancia de ese deber para las 09:00 de hoy.
- AC-16 (RF-10): Dado un deber de visita/estudio con subtipo "vacuna" agendado para hoy a las 10:00 para una mascota, con su cumplimiento registrado hoy, cuando un cuidador consulta mañana el estado del día de esa mascota, entonces no aparece ninguna instancia de ese deber.
- AC-17 (RF-10, RF-22): Dado un deber de baño dado de alta el lunes para una mascota y agendado para el viernes a las 15:00, cuando un cuidador consulta el viernes a las 09:00 el estado del día de esa mascota, entonces el baño aparece sin la marca "no cumplido".
- AC-17b (RF-10b): Dado el día de hoy, cuando un cuidador intenta dar de alta un baño agendado para ayer, entonces el sistema rechaza el alta y el deber no aparece en la configuración de deberes.
- AC-17c (RF-10b): Dado el día de hoy, cuando un cuidador intenta dar de alta una visita/estudio con subtipo "veterinario" agendada para ayer, entonces el sistema rechaza el alta y el deber no aparece en la configuración de deberes.
- AC-17d (RF-10c, RF-30a, RF-32b): Dado que hoy a las 10:30 un cuidador da de alta un baño agendado para hoy a las 10:00, cuando consulta el estado del día, entonces ve esa instancia marcada como "no cumplido" y, al llegar las 23:59, el sistema no envió ningún email de vencimiento para ella.
- AC-18 (RF-11): Dado el alta de un deber de tipo visita/estudio, cuando un cuidador elige su subtipo, entonces las únicas opciones disponibles son veterinario, estudio, vacuna e intervención médica.
- AC-18b (RF-11d, RF-36): Dado el alta de un deber de tipo visita/estudio, cuando un cuidador intenta guardarlo sin elegir subtipo, entonces el sistema rechaza el alta, le informa que debe elegir un subtipo, y el deber no aparece en la configuración de deberes.
- AC-19b (RF-11b): Dada una mascota con un deber de baño agendado para dentro de 10 días a las 15:00 y un deber de "paseo cada 4 horas a partir de las 08:00, repetir todos los días", cuando un cuidador consulta la configuración de deberes de la mascota, entonces ve cada deber una sola vez: el baño con esa fecha y la hora 15:00, y el paseo con su modalidad "cada 4 horas a partir de las 08:00" y "repetir todos los días".
- AC-19c (RF-11b): Dado un deber de "comida, hora fija 08:00, solo para hoy" dado de alta ayer y no cumplido, cuando hoy un cuidador consulta la configuración de deberes de la mascota, entonces ese deber no aparece.
- AC-19d (RF-11b): Dado un deber de baño agendado para hoy a las 10:00 con su cumplimiento registrado, cuando un cuidador consulta la configuración de deberes de la mascota, entonces ese deber no aparece.
- AC-19e (RF-11b): Dado el escenario del AC-13c (una medicación terminada por fecha el día 10, con la dosis de ese día sin cumplir), cuando el día 11 un cuidador consulta la configuración de deberes de la mascota, entonces el deber aparece.
- AC-19f (RF-11c): Dada la configuración de deberes de una mascota, cuando un cuidador da de alta un deber de "paseo, hora fija 18:00, repetir todos los días", entonces ese deber aparece en la configuración de deberes de la mascota.
- AC-20d (RF-12a, RF-12b): Dado un deber de visita/estudio agendado para mañana a las 10:00, cuando hoy un cuidador da de baja el deber, entonces el deber deja de aparecer en la configuración de deberes y mañana a las 09:00 el estado del día de la mascota no muestra ninguna instancia de ese deber.
- AC-24 (RF-12a, RF-15): Dado un deber de "paseo, hora fija 09:00, repetir todos los días", cuando hoy se da de baja el deber desde la configuración de deberes, entonces deja de aparecer en la configuración y mañana a las 08:00 el estado del día de la mascota no muestra ninguna instancia de ese deber.
- AC-20b (RF-12b, RF-20): Dado un deber de "medicación, hora fija 09:00, repetir todos los días" con las instancias de ayer y de hoy sin cumplir, cuando hoy a las 08:00 se da de baja el deber, entonces ninguna de las dos instancias aparece en el estado del día y, al llegar las 23:59 de hoy, el sistema no envió en el día ningún email de vencimiento para ninguna de ellas.
- AC-25 (RF-16): Dado un deber de "paseo, hora fija 09:00, repetir todos los días" cuya instancia de hoy se cumplió a las 09:05, cuando a las 12:00 se da de baja el deber, entonces el estado del día de la mascota sigue contando ese paseo en la cantidad de paseos realizados.

### Instancias

- AC-28 (RF-17, RNF-05, RNF-06): Dado un deber de "comida, hora fija 08:00, repetir todos los días", cuando un cuidador consulta el estado del día a las 00:05 en UTC−3 (03:05 UTC), entonces se muestra la instancia de ese deber para las 08:00 del nuevo día.
- AC-28b (RF-17, RNF-06): Dado el mismo deber del AC-28, cuando un cuidador consulta el estado del día a las 23:59 en UTC−3 (02:59 UTC), entonces no se muestra ninguna instancia de ese deber para el día siguiente.
- AC-28c (RF-17c, RF-32b): Dado un deber de "comida, hora fija 08:00, repetir todos los días" y el servidor caído entre las 23:00 de ayer y las 10:00 de hoy, cuando el servidor vuelve a estar en ejecución a las 10:00, entonces el estado del día muestra la instancia de las 08:00 de hoy marcada como "pendiente" y el sistema no envía email de vencimiento para ella.
- AC-14 (RF-17b, RF-32b): Dado que hoy a las 10:30 un cuidador da de alta "paseo, hora fija 10:00, solo para hoy", cuando consulta el estado del día, entonces ve esa instancia marcada como "pendiente" y, al llegar las 23:59, el sistema no envió ningún email de vencimiento para ella.
- AC-14b (RF-17b, RF-32, RF-32b): Dado que hoy a las 13:00 un cuidador da de alta "comida cada 4 horas a partir de las 08:00, solo para hoy", cuando llegan las 16:20 sin cumplimientos registrados, entonces el estado del día muestra las instancias de las 08:00, 12:00 y 16:00 como "pendiente" y la de las 20:00 sin marca, y el sistema envió un email de vencimiento solo para la de las 16:00.
- AC-29 (RF-18): Dado un deber de "medicación, hora fija 09:00" sin cumplimiento registrado, cuando un cuidador abre la aplicación (o recarga la pantalla) a las 09:10 y consulta el estado, entonces esa instancia todavía no se muestra como "pendiente".
- AC-29b (RF-18): Dado el mismo deber del AC-29, cuando un cuidador abre la aplicación (o recarga la pantalla) a las 09:15 y consulta el estado, entonces esa instancia se muestra como "pendiente".
- AC-30 (RF-19): Dado un deber de "comida, hora fija 08:00, repetir todos los días" cuya instancia de ayer no se cumplió, cuando un cuidador consulta hoy a las 07:00 el estado del día, entonces no aparece la instancia de ayer y aparece la de hoy, sin marca.
- AC-31 (RF-20, RF-22): Dado un deber de "medicación, hora fija 09:00, repetir todos los días" cuya instancia de ayer no se cumplió, cuando un cuidador consulta hoy a las 08:00 el estado del día de la mascota, entonces ve dos instancias de ese deber: la de ayer, marcada como "pendiente", y la de hoy, sin marca.
- AC-31b (RF-20, RF-29): Dado el escenario del AC-31, cuando un cuidador registra el cumplimiento de la instancia de ayer, entonces esa instancia deja de figurar como "pendiente" y la de hoy sigue vigente.
- AC-44 (RF-29): Dado un deber de "paseo cada 4 horas a partir de las 08:00" cuya instancia de las 12:00 no tiene cumplimiento registrado, cuando un cuidador abre la aplicación (o recarga la pantalla) a las 12:20 y consulta el estado, entonces esa instancia se muestra marcada como "pendiente".
- AC-45 (RF-30a, RF-30b): Dado un deber de visita/estudio agendado para hace 3 días y sin cumplimiento registrado, cuando un cuidador consulta hoy el estado de esa mascota, entonces su instancia se muestra marcada como "no cumplido".
- AC-46 (RF-30b, RF-21): Dado el mismo deber del AC-45, cuando un cuidador registra su cumplimiento, entonces esa instancia deja de aparecer en el estado del día de la mascota.
- AC-20 (RF-14, RF-14b): Dado un deber de baño agendado para hoy a las 15:00 sin cumplimiento registrado, cuando a las 10:00 un cuidador da de baja su instancia, entonces la instancia deja de aparecer en el estado del día y el deber deja de aparecer en la configuración de deberes de la mascota.
- AC-20c (RF-14, RF-14b): Dado un deber de "comida, hora fija 20:00, solo para hoy" sin cumplimiento registrado, cuando a las 10:00 un cuidador da de baja su instancia, entonces la instancia deja de aparecer en el estado del día y el deber deja de aparecer en la configuración de deberes de la mascota.
- AC-23 (RF-14, RF-14c): Dado un deber de "paseo cada 4 horas a partir de las 08:00, repetir todos los días", cuando a las 07:00 un cuidador da de baja la instancia de las 12:00, entonces el estado del día muestra solo las instancias de las 08:00, 16:00 y 20:00, y el deber sigue apareciendo en la configuración de deberes.
- AC-23b (RF-14, RF-14c): Dado el escenario del AC-23, cuando al día siguiente a las 07:00 un cuidador consulta el estado del día, entonces se muestran las cuatro instancias de ese deber.

### Registro de cumplimiento

- AC-32 (RF-21): Dada una mascota con una instancia vigente de un deber de paseo, cuando un cuidador registra su cumplimiento desde el estado del día de la mascota, entonces esa instancia deja de aparecer en el estado del día y la cantidad de paseos realizados de la mascota aumenta en 1.
- AC-32b (RF-21b, RF-21c): Dados dos cuidadores con el estado del día abierto mostrando la misma instancia vigente de paseo de la mascota A, cuando el cuidador 1 registra su cumplimiento y, antes de que la pantalla del cuidador 2 se actualice, el cuidador 2 registra el cumplimiento de esa misma instancia, entonces el sistema rechaza el registro del cuidador 2, le informa que la instancia ya fue cumplida, y la cantidad de paseos realizados de la mascota A aumentó solo en 1.
- AC-32c (RF-21d, RF-21e): Dados dos cuidadores con el estado del día abierto mostrando la misma instancia vigente de paseo de la mascota A, cuando el cuidador 1 da de baja esa instancia y, antes de que la pantalla del cuidador 2 se actualice, el cuidador 2 registra su cumplimiento, entonces el sistema rechaza el registro del cuidador 2, le informa que la instancia ya fue dada de baja, y la cantidad de paseos realizados de la mascota A no cambia.
- AC-32d (RF-14d, RF-14e): Dados dos cuidadores con el estado del día abierto mostrando la misma instancia vigente de paseo de la mascota A, cuando el cuidador 1 registra su cumplimiento y, antes de que la pantalla del cuidador 2 se actualice, el cuidador 2 da de baja esa instancia, entonces el sistema rechaza la baja del cuidador 2, le informa que la instancia ya fue cumplida, y la cantidad de paseos realizados de la mascota A aumentó en 1.
- AC-32e (RF-14d, RF-14e): Dados dos cuidadores con el estado del día abierto mostrando la misma instancia vigente de paseo de la mascota A, cuando el cuidador 1 da de baja esa instancia y, antes de que la pantalla del cuidador 2 se actualice, el cuidador 2 también la da de baja, entonces el sistema rechaza la baja del cuidador 2 y le informa que la instancia ya fue dada de baja.
- AC-32f (RF-12b, RF-21d, RF-21e): Dados dos cuidadores, el cuidador 2 con el estado del día abierto mostrando una instancia vigente de un deber de paseo de la mascota A, cuando el cuidador 1 da de baja ese deber desde la configuración de deberes y, antes de que la pantalla del cuidador 2 se actualice, el cuidador 2 registra el cumplimiento de esa instancia, entonces el sistema rechaza el registro, le informa que la instancia ya fue dada de baja, y la cantidad de paseos realizados de la mascota A no cambia.
- AC-33b (RF-21): Dado el escenario del AC-33, cuando a las 12:00 un cuidador registra el cumplimiento de la instancia de baño, entonces esa instancia deja de aparecer en el estado del día y la del paseo sigue apareciendo.
- AC-35 (RF-23): Dada una instancia vigente de un deber de comida, cuando un cuidador registra su cumplimiento a las 10:30 aceptando la hora que propone el sistema, entonces el estado del día de la mascota muestra la última comida con la fecha de hoy y la hora 10:30.
- AC-35b (RF-23): Dada una instancia vigente de un deber de comida, cuando un cuidador registra su cumplimiento a las 10:30 y cambia la hora propuesta a 10:00, entonces el estado del día de la mascota muestra la última comida con la fecha de hoy y la hora 10:00.
- AC-35c (RF-23b): Dada una instancia vigente de un deber de comida, cuando a las 10:30 un cuidador registra su cumplimiento cambiando la hora propuesta a 11:00, entonces el sistema rechaza el registro y la instancia sigue apareciendo en el estado del día.
- AC-35d (RF-23c, RF-23b): Dada una instancia de un deber de "medicación, hora fija 21:00" de ayer, sin cumplimiento registrado, cuando hoy a las 08:00 un cuidador registra su cumplimiento cambiando la hora propuesta a 22:00, entonces el sistema rechaza el registro (la hora corresponde a hoy y es posterior a la actual) y la instancia sigue marcada como "pendiente".
- AC-36 (RF-24): Dada una instancia vigente de un deber de baño, cuando un cuidador registra su cumplimiento, entonces el sistema no le pide hora y la instancia deja de aparecer en el estado del día.
- AC-37 (RF-25): Dado que la mascota A tiene 2 paseos registrados hoy, uno de ellos cargado por error, cuando el cuidador elimina ese registro, entonces el estado del día de la mascota A muestra 1 paseo.
- AC-37b (RF-25): Dado un registro de cumplimiento de paseo cargado ayer a las 18:00 y ningún paseo registrado hoy, cuando hoy un cuidador envía una solicitud para eliminarlo, entonces el sistema rechaza la operación y el último paseo de la mascota sigue mostrándose con la fecha de ayer y la hora 18:00.
- AC-37c (RF-25, RF-26): Dada una instancia de un deber de "medicación, hora fija 09:00" de ayer, sin cumplir, cuyo cumplimiento se registró por error hoy a las 08:00, cuando hoy a las 10:00 un cuidador elimina ese registro, entonces el sistema lo acepta y la instancia de ayer vuelve a aparecer en el estado del día marcada como "pendiente".
- AC-38 (RF-26): Dado un deber de "comida, hora fija 08:00" cuya instancia de hoy tiene un cumplimiento registrado por error a las 08:10, cuando a las 10:00 un cuidador elimina ese registro, entonces esa instancia vuelve a aparecer en el estado del día de la mascota, marcada como "pendiente".

### Estado del día

- AC-33 (RF-22): Dada una mascota con un deber de paseo con horario hoy a las 18:00 y un deber de baño agendado para hoy a las 15:00, cuando un cuidador consulta el estado del día de la mascota a las 12:00, entonces ve la instancia del paseo de las 18:00 y la del baño de las 15:00.
- AC-39 (RF-22, RF-27a, RF-27b, RF-27c): Dado que hoy una mascota fue paseada 2 veces, la última a las 11:00, comió por última vez a las 13:00, tiene una instancia de medicación de ayer no cumplida y tiene un deber de visita/estudio vencido sin cumplir, cuando cualquier cuidador del hogar consulta el estado del día, entonces ve "2 paseos", el último paseo con la fecha de hoy y la hora 11:00, la última comida con la fecha de hoy y la hora 13:00, la medicación pendiente y la instancia de visita/estudio marcada como "no cumplido".
- AC-39b (RF-22): Dada una mascota con un deber de visita/estudio agendado para dentro de 3 días, cuando un cuidador consulta el estado del día, entonces ese deber no aparece.
- AC-39c (RF-27b, RF-27c): Dada una mascota cuyo último cumplimiento de comida registrado fue ayer a las 20:00 y el último de paseo ayer a las 21:00, sin cumplimientos registrados hoy, cuando un cuidador consulta hoy a las 07:00 el estado del día, entonces ve la última comida con la fecha de ayer y la hora 20:00, y el último paseo con la fecha de ayer y la hora 21:00.
- AC-39d (RF-27b, RF-27c): Dada una mascota recién registrada, sin ningún cumplimiento de comida ni de paseo, cuando un cuidador consulta el estado del día, entonces ve que no hay ninguna comida ni ningún paseo registrados.
- AC-65 (RF-38, RF-38b): Dado un hogar sin mascotas registradas, cuando un cuidador consulta el estado del día, entonces ve un mensaje que indica que no hay mascotas y la opción de registrar una.
- AC-65b (RF-38c): Dada la mascota A con un único deber, "paseo, hora fija 09:00, solo para hoy", cuya instancia se cumplió hoy a las 09:05, cuando un cuidador consulta el estado del día a las 10:00, entonces ve la sección de la mascota A con "1 paseo", el último paseo con la fecha de hoy y la hora 09:05, y un mensaje que indica que no hay tareas pendientes.
- AC-40 (RF-28a): Dado un deber de visita/estudio con subtipo "vacuna" agendado para hoy y no cumplido, cuando un cuidador consulta el estado del día de esa mascota, entonces su instancia se muestra con el subtipo "vacuna".
- AC-47 (RF-31, RNF-04): Dados dos cuidadores con la aplicación abierta mirando el estado del día, cuando uno de ellos registra el cumplimiento de una instancia vigente de paseo de la mascota A, entonces dentro de los 15 segundos siguientes, sin recargar manualmente, en la pantalla del otro esa instancia deja de aparecer y la cantidad de paseos realizados de la mascota A aumenta en 1.
- AC-47b (RF-31, RNF-04): Dado un cuidador que a las 12:14 tiene abierto el estado de una mascota con un deber de "paseo, hora fija 12:00" sin cumplimiento registrado, y que no interactúa con la pantalla, cuando llegan las 12:15, entonces a más tardar a las 12:15:15 su pantalla muestra esa instancia marcada como "pendiente", sin recargar manualmente.
- AC-47c (RF-31, RNF-04): Dado un cuidador que a las 14:59 tiene abierto el estado de una mascota con un deber de baño agendado para hoy a las 15:00 sin cumplimiento registrado, y que no interactúa con la pantalla, cuando se superan las 15:00, entonces a más tardar a las 15:00:15 su pantalla muestra esa instancia marcada como "no cumplido", sin recargar manualmente.
- AC-47d (RF-31, RNF-04): Dados dos cuidadores con la aplicación abierta mirando el estado del día, cuando uno de ellos da de baja una instancia vigente de un paseo, entonces el otro deja de ver esa instancia dentro de los 15 segundos siguientes, sin recargar manualmente.

### Emails

- AC-38b (RF-32, RNF-01): Dado el escenario del AC-38 (el registro erróneo de las 08:10 evitó el email de las 08:15), cuando a las 10:00 un cuidador elimina ese registro, entonces el sistema envía un email de vencimiento para esa instancia a más tardar a las 10:05.
- AC-38c (RF-32): Dado un deber de "comida, hora fija 08:00" cuya instancia de hoy ya generó un email de vencimiento a las 08:15 y luego recibió un cumplimiento registrado por error a las 09:00, cuando a las 10:00 un cuidador elimina ese registro, entonces el sistema no envía un segundo email de vencimiento para esa instancia en lo que resta del día.
- AC-48 (RF-32, RNF-01): Dado un deber de "medicación, hora fija 09:00" sin cumplimiento registrado y ningún cuidador del hogar con la aplicación abierta desde las 09:00, cuando llegan las 09:15, entonces el sistema envía un email de vencimiento a todos los cuidadores del hogar a más tardar a las 09:20.
- AC-49 (RF-32): Dado un deber de "medicación, hora fija 09:00" cuya instancia de hoy tiene su cumplimiento registrado a las 09:05, cuando llegan las 09:20, entonces el sistema no envió ningún email de vencimiento para esa instancia.
- AC-43 (RF-33, RF-28c, RNF-01): Dado un deber de visita/estudio con subtipo "vacuna" agendado para hoy a las 10:00 y sin cumplimiento registrado, cuando se superan las 10:00, entonces a más tardar a las 10:05 se envía el email de vencimiento y ese email indica el subtipo "vacuna".
- AC-50 (RF-33, RNF-01): Dado un deber de visita/estudio agendado para hoy a las 10:00, sin cumplimiento registrado y ningún cuidador del hogar con la aplicación abierta desde las 10:00, cuando se superan las 10:00, entonces el sistema envía un email de vencimiento a todos los cuidadores del hogar a más tardar a las 10:05.
- AC-51 (RF-34, RNF-01): Dado un deber de baño dado de alta el lunes y agendado para el viernes a las 15:00, y ningún cuidador del hogar con la aplicación abierta desde el miércoles a las 15:00, cuando llega el miércoles a las 15:00, entonces el sistema envía un email recordatorio a todos los cuidadores del hogar a más tardar a las 15:05.
- AC-51b (RF-34): Dado el mismo deber del AC-51, cuando llega el viernes a las 15:00, entonces el sistema envió exactamente un email recordatorio para ese deber.
- AC-51c (RF-34): Dado un deber de baño dado de alta el lunes y agendado para el viernes a las 15:00, cuando el martes un cuidador lo da de baja, entonces al llegar el viernes a las 15:00 el sistema no envió ningún email recordatorio para ese deber.
- AC-52 (RF-34): Dado un deber de baño dado de alta el jueves a las 10:00 y agendado para el viernes a las 15:00, cuando llega el viernes a las 15:00, entonces el sistema no envió ningún email recordatorio para ese deber.
- AC-52b (RF-34): Dado un deber de baño dado de alta el miércoles a las 15:00 y agendado para el viernes a las 15:00, cuando llega el viernes a las 15:00, entonces el sistema envió exactamente un email recordatorio para ese deber.
- AC-42 (RF-34, RF-28d, RNF-01): Dado un deber de visita/estudio con subtipo "vacuna" dado de alta el lunes y agendado para el viernes a las 15:00, cuando llega el miércoles a las 15:00, entonces a más tardar a las 15:05 se envía el email recordatorio y ese email indica el subtipo "vacuna".
- AC-53 (RF-35): Dada una mascota con un máximo de 20 días entre baños, cuyo último baño registrado fue hace 25 días, y sin ningún deber de baño activo, cuando llegan las 23:59 de hoy, entonces el sistema envió en el día a todos los cuidadores del hogar exactamente un email de aviso de turno de baño para esa mascota.
- AC-53b (RF-35, RNF-01): Dado el escenario del AC-53, cuando llegan las 07:00, entonces a más tardar a las 07:05 el sistema envía el email de aviso de turno de baño para esa mascota.
- AC-54 (RF-35): Dado el escenario del AC-53 pero con un deber de baño agendado a futuro para esa mascota, cuando llegan las 23:59 de hoy, entonces el sistema no envió en el día ningún email de aviso de turno de baño para esa mascota.
- AC-54b (RF-35): Dado el escenario del AC-53 pero con un baño agendado para hoy a las 15:00, cuando llegan las 23:59 de hoy, entonces el sistema no envió en el día ningún email de aviso de turno de baño para esa mascota.
- AC-54c (RF-35): Dado el escenario del AC-53 pero con un baño agendado para hace 2 días, marcado como "no cumplido", cuando llegan las 23:59 de hoy, entonces el sistema no envió en el día ningún email de aviso de turno de baño para esa mascota.
- AC-53c (RF-35, RNF-01): Dado el escenario del AC-54, cuando a las 10:00 un cuidador da de baja el deber de baño futuro, entonces el sistema no envía el email de aviso de turno de baño para esa mascota ese día, y lo envía al día siguiente a más tardar a las 07:05.
- AC-53d (RF-35): Dada una mascota con un máximo de 20 días entre baños, cuyo último baño registrado fue hace exactamente 20 días, y sin ningún deber de baño activo, cuando llegan las 23:59 de hoy, entonces el sistema no envió en el día ningún email de aviso de turno de baño para esa mascota.
- AC-55 (RF-35): Dada una mascota sin máximo de días entre baños definido, cuyo último baño registrado fue hace 60 días, y sin ningún deber de baño activo, cuando llegan las 23:59 de hoy, entonces el sistema no envió en el día ningún email de aviso de turno de baño para esa mascota.
- AC-56 (RF-35): Dada una mascota con un máximo de 20 días entre baños, dada de alta hace 25 días, sin ningún baño registrado y sin ningún deber de baño activo, cuando llegan las 23:59 de hoy, entonces el sistema envió en el día a todos los cuidadores del hogar exactamente un email de aviso de turno de baño para esa mascota.
- AC-62 (RF-32c, RNF-01): Dado un deber de "medicación, hora fija 09:00" sin cumplimiento registrado y el servicio de envío de emails rechazando todo envío entre las 09:15 y las 09:17, cuando el servicio vuelve a aceptar envíos a las 09:17, entonces el servicio recibe y acepta el email de vencimiento de esa instancia a más tardar a las 09:20.
- AC-62b (RF-32c): Dado un deber de "medicación, hora fija 09:00" sin cumplimiento registrado y el servicio de envío de emails rechazando todo envío entre las 09:15 y las 09:30, cuando llegan las 23:59, entonces el sistema no intentó enviar el email de vencimiento de esa instancia después de las 09:20.
- AC-62c (RF-32d, RF-32): Dado el escenario del AC-62b, cuando a las 10:00 un cuidador registra por error el cumplimiento de esa instancia y a las 10:30 lo elimina, entonces el sistema envía el email de vencimiento para esa instancia a más tardar a las 10:35.
- AC-62d (RF-32e, RF-32c, RNF-01): Dado un deber de "medicación, hora fija 09:00" sin cumplimiento registrado y el servicio de envío de emails sin responder a ningún intento entre las 09:15 y las 09:17, cuando el servicio vuelve a responder normalmente a las 09:17, entonces el servicio recibe y acepta el email de vencimiento de esa instancia a más tardar a las 09:20.
- AC-64 (RF-37): Dado un deber de "medicación, hora fija 09:00" de la mascota Firulais sin cumplimiento registrado, cuando se envía su email de vencimiento, entonces el email indica "Firulais", el tipo "medicación" y la fecha de hoy con la hora 09:00.
- AC-64b (RF-37b): Dado un deber de baño de la mascota Firulais dado de alta el lunes y agendado para el viernes a las 15:00, cuando se envía su email recordatorio el miércoles, entonces el email indica "Firulais", el tipo "baño" y la fecha del viernes con la hora 15:00.
- AC-64c (RF-37c): Dado el escenario del AC-53, con la mascota Firulais, cuando se envía el email de aviso de turno de baño, entonces el email indica "Firulais", el máximo de 20 días y la fecha del último baño registrado, de hace 25 días.
- AC-64d (RF-37c): Dado el escenario del AC-56, cuando se envía el email de aviso de turno de baño, entonces el email indica que no hay baños registrados para esa mascota.
- AC-58 (RF-35): Dado el escenario del AC-53 y que ayer se envió el email de aviso de turno de baño, sin que desde entonces se haya registrado ni agendado un baño, cuando llegan las 23:59 de hoy, entonces el sistema envió en el día exactamente un email de aviso de turno de baño para esa mascota.

### Rechazos

- AC-63 (RF-36, RF-04b): Dado un hogar con la mascota Firulais registrada, cuando un cuidador intenta registrar otra mascota con el nombre FIRULAIS, entonces el sistema rechaza el registro y le informa que ya existe una mascota con ese nombre.
- AC-63b (RF-36, RF-06c): Dado el alta de un deber de paseo en la modalidad "cada N horas", cuando un cuidador ingresa N = 24, entonces el sistema rechaza el alta y le informa que N debe ser un número entero entre 1 y 23.
- AC-63c (RF-36, RF-23b): Dada una instancia vigente de un deber de comida, cuando a las 10:30 un cuidador registra su cumplimiento cambiando la hora propuesta a 11:00, entonces el sistema rechaza el registro y le informa que la hora no puede ser posterior a la actual.
- AC-63d (RF-36, RF-01c): Dado un hogar ya existente, cuando una persona intenta registrarse con un código de hogar incorrecto, entonces el sistema rechaza el registro y le informa que el código de hogar es incorrecto.
- AC-63e (RF-36, RF-04c): Dado un hogar sin mascotas registradas, cuando un cuidador intenta registrar una mascota con el nombre vacío, entonces el sistema rechaza el registro y le informa que el nombre de la mascota no puede estar vacío.
- AC-63f (RF-36, RF-05c): Dada una mascota sin máximo de días entre baños definido, cuando un cuidador intenta definir 0 como máximo, entonces el sistema rechaza la operación y le informa que el máximo debe ser un número entero mayor que 0.

### Requerimientos no funcionales

- AC-59 (RNF-02): Dado un navegador móvil con un viewport de 360 píxeles de ancho, cuando un cuidador completa cada uno de estos flujos: (1) registrarse creando el hogar, (2) registrarse con código de hogar, (3) iniciar sesión, (4) cerrar sesión, (5) registrar una mascota, (6) definir y modificar el máximo de días entre baños, (7) dar de alta un deber de hora fija, (8) dar de alta un deber cada N horas, (9) dar de alta un deber de baño, (10) dar de alta un deber de visita/estudio con subtipo, (11) dar de baja un deber, (12) registrar un cumplimiento editando la hora, (13) dar de baja una instancia, (14) eliminar un registro de cumplimiento, (15) consultar el código de hogar; entonces ninguna pantalla de esos flujos muestra scroll horizontal y cada flujo se completa.
- AC-60 (RNF-03): Dado un cuidador con sesión iniciada en la pantalla principal y una mascota con una instancia vigente de un deber de paseo, cuando registra su cumplimiento aceptando la hora por defecto, entonces la instancia deja de aparecer en el estado del día habiendo usado 5 toques/clics o menos.
- AC-61 (RNF-06): Dado un deber de "comida, hora fija 08:00, repetir todos los días" sin cumplimiento registrado, cuando un cuidador consulta el estado del día a las 05:15 en UTC−3 (08:15 UTC), entonces esa instancia no se muestra como "pendiente".

## Fuera de Alcance

Lo siguiente no forma parte de esta versión. Su inclusión no implica que esté planificado para una versión futura.

- Roles, permisos diferenciados o administrador entre cuidadores del hogar: todos los cuidadores del hogar ven y modifican todos los datos del hogar (mascotas, deberes y registros de cumplimiento), incluidos los cargados por otros cuidadores.
- Múltiples hogares en el sistema (existe un único hogar compartido por todos los cuidadores).
- Notificaciones push o por WhatsApp/SMS (solo email en esta versión).
- Consultar registros de cumplimiento de días anteriores, historial, estadísticas o gráficos de actividad (el sistema muestra solo los registros del día actual, salvo la fecha y la hora de la última comida y del último paseo, RF-27b y RF-27c).
- Recomendaciones o sugerencias automáticas de horarios (IA).
- Reactivar un deber dado de baja (la baja es definitiva; si se necesita de nuevo, se da de alta otra vez).
- Editar un registro de cumplimiento de cuidado ya creado (se resuelve eliminando y volviendo a registrar).
- Invitación, aprobación o moderación para unirse a un hogar (alcanza con conocer el código de hogar).
- Recuperación de contraseña, verificación de email o login social.
- Actualización instantánea de la pantalla entre cuidadores (la actualización automática puede demorar hasta 15 segundos, RNF-04).
- Recordatorios múltiples o escalonados para baño/visita (solo un email recordatorio, 48 horas antes, RF-34).
- Gestión real de turnos con el veterinario o la peluquería (el sistema solo registra la fecha y hora que el cuidador ya acordó por fuera; no agenda ni se integra con terceros).
- Vista de calendario con los deberes de baño o visita/estudio futuros (en esta versión se consultan en la configuración de deberes y se avisan con el email recordatorio).
- Editar el nombre de una mascota o darla de baja (sí se puede modificar su máximo de días entre baños, RF-05b).
- Quitar el máximo de días entre baños de una mascota una vez definido (se puede modificar, RF-05b, pero no volver a dejarlo sin definir).
- Editar un deber de cuidado ya creado (se resuelve dando de baja y volviendo a dar de alta).
- Salir de un hogar o dar de baja un cuidador.
- Cambiar el email o la contraseña de un cuidador.
- Datos de la mascota además del nombre (especie, raza, edad, foto).
- Requisitos de complejidad de contraseña, más allá de la longitud mínima de 8 caracteres (RF-01h).
- Registrar un cumplimiento con una fecha anterior al día en que se registra (por ejemplo, una dosis de medicación dada ayer a la noche y registrada hoy queda con la fecha de hoy; RF-23c).
- Requisitos de accesibilidad (compatibilidad con lectores de pantalla, contraste mínimo, tamaño mínimo de áreas táctiles) más allá de RNF-02 y RNF-03.
- Bloqueo o demora tras intentos fallidos de inicio de sesión.
- Uso sin conexión o instalación como PWA (la app requiere conexión).
- Registrar un cumplimiento que no corresponde a ninguna instancia. Para registrar una tarea no agendada, el cuidador da de alta el deber correspondiente (por ejemplo, "paseo, hora fija, solo para hoy", o un baño o visita/estudio puntual) y registra el cumplimiento de su instancia.
- Enviar un email cuyos reintentos no lograron que el servicio de envío lo aceptara dentro de los 5 minutos posteriores a su condición de disparo (ese email se pierde; RF-32c).
- Enviar, al volver a estar en ejecución el servidor, los emails cuya condición de disparo se cumplió mientras estaba caído (esos emails se pierden; RNF-01).

## Riesgos y Dependencias

- Riesgo: el envío de email falla o llega tarde y el cuidador deja de confiar en la alarma → mitigación: el sistema reintenta el envío durante los 5 minutos posteriores a la condición de disparo (RF-32c), y la instancia de deber igual se muestra como "pendiente" o "no cumplido" en la interfaz (RF-29, RF-30a, RF-30b) aunque el email no llegue a enviarse.
- Riesgo: el código de hogar se filtra y una persona ajena se suma al hogar → el formato del código (RF-01i) impide adivinarlo por prueba; el riesgo aceptado es solo que alguien lo comparta, como limitación conocida para el alcance de este ejercicio (no hay aprobación de altas, ver Fuera de Alcance).
- Riesgo: quien conoce el código de hogar puede averiguar si un email ya está registrado, porque el registro lo informa (RF-01f, RF-36) → se acepta: con el código esa persona ya puede sumarse al hogar y ver todos sus datos; sin el código no obtiene esa información (RF-01l).
- Riesgo: la aleatoriedad del código de hogar (RF-01i) no se puede verificar con un criterio de aceptación binario (AC-01c verifica solo su formato) → mitigación: se valida en la revisión del diseño que el código se genere al azar.
- Riesgo: dar de baja el deber completo cuando se quería dar de baja solo una instancia corta las alarmas sin que se note de inmediato → mitigación: la baja de un deber y la de una instancia se hacen desde vistas distintas (configuración de deberes y estado del día).
- Riesgo: sin fecha de fin en un tratamiento de medicación, el sistema alertaría indefinidamente después de terminado → mitigación: opción "hasta fecha" en la configuración de medicación recurrente (RF-09, AC-13).
- Riesgo: el polling entre cuidadores genera carga innecesaria en el servidor → mitigación: un intervalo de 15 s es aceptable para el volumen de un solo hogar; no está pensado para escalar a muchos hogares simultáneos.
- Riesgo: si el servidor está caído no se generan instancias ni se envían alarmas → mitigación: al volver se generan las instancias del día que falten (RF-17c) y la interfaz igual muestra "pendiente" o "no cumplido" (RF-29, RF-30a).
- Dependencia: servicio de envío de emails.
- Dependencia: mecanismo de autenticación por email y contraseña, con generación y validación del código de hogar (RF-01a a RF-01g, RF-02, RF-02b, RF-02c, RF-03, RF-03b).
- Dependencia: proceso programado (job/cron/scheduler) que genere instancias diarias, revise vencimientos y recordatorios, evalúe el aviso de turno de baño y reintente los emails que el servicio de envío no aceptó, independientemente de que haya cuidadores con la aplicación abierta (RF-17, RF-32, RF-32b, RF-32c, RF-32d, RF-33, RF-34, RF-35, RNF-01, RNF-05).
- Dependencia: mecanismo de refresco en el cliente (polling) para reflejar en pantalla los cambios realizados por otros cuidadores y los vencimientos de instancias (RF-31, RNF-04).
- Dependencia: SQLite (o motor equivalente) para persistir hogares, cuidadores, mascotas, deberes (con su modalidad, subtipo si aplica, y referencia a su regla de origen cuando son recurrentes) y registros de cumplimiento de cuidado, y los emails de vencimiento, recordatorio y aviso de turno de baño que el servicio de envío aceptó (RF-32, RF-32d, RF-34, RF-35).
- Dependencia: reloj del servidor sincronizado con la hora oficial; un desvío de minutos altera los vencimientos, el cambio de día, los avisos y recordatorios, y la ventana de envío de emails (RF-18, RF-30a, RF-34, RF-35, RNF-01, RNF-06).
