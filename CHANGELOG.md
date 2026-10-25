# Changelog

Todos los cambios relevantes del proyecto se registran en este archivo.
El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Unreleased]

### Added
- [FEAT-001a] Datos precargados: hogar, cuidadores ana@ejemplo.com y beto@ejemplo.com, mascotas Firulais y Michi.
- [FEAT-001a] Configuración de deberes por mascota: alta de deberes de comida o paseo a hora fija, "solo para hoy" o "todos los días", con validación y lista de deberes activos.
- [FEAT-001a] Instancias diarias: se generan al dar de alta, al empezar cada día y al reiniciar el servidor; quedan "pendiente" a los 15 minutos y expiran al terminar el día (UTC−3).
- [FEAT-001a] Estado del día como pantalla principal: una sección por mascota con sus instancias vigentes, paseos de hoy, última comida y último paseo; usable a 360 px.
