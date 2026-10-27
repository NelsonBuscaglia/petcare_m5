# Changelog

Todos los cambios relevantes del proyecto se registran en este archivo.
El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Unreleased]

### Added
- [FEAT-001a] Datos precargados: hogar, cuidadores ana@ejemplo.com y beto@ejemplo.com, mascotas Firulais y Michi.
- [FEAT-001a] Configuración de deberes por mascota: alta de deberes de comida o paseo a hora fija, "solo para hoy" o "todos los días", con validación y lista de deberes activos.
- [FEAT-001a] Instancias diarias: se generan al dar de alta, al empezar cada día y al reiniciar el servidor; quedan "pendiente" a los 15 minutos y expiran al terminar el día (UTC−3).
- [FEAT-001a] Estado del día como pantalla principal: una sección por mascota con sus instancias vigentes, paseos de hoy, última comida y último paseo; usable a 360 px.
- [FEAT-001b] Registro de cumplimiento desde el estado del día: "Cumplido" + "Confirmar" con la hora actual propuesta y editable (no posterior a la actual); rechazo de instancias ya cumplidas o expiradas.
- [FEAT-001b] Registros del día con eliminación: borrar un registro de hoy reactiva su instancia; los de otros días no se pueden eliminar.
- [FEAT-001b] Email de vencimiento: un único aviso a todos los cuidadores cuando una instancia vence (15 min) sin cumplirse, con reintentos durante 5 minutos; por consola en desarrollo y por SMTP (MailKit, TLS obligatorio, destinatarios en Bcc) configurado solo por variables de entorno.
- [FEAT-001b] Actualización automática del estado del día cada 10 s (configurable 1..15 s en `EstadoDelDia:RefrescoSegundos`), pausada mientras hay un formulario de cumplimiento abierto.
