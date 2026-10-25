# PRD padre: Deberes de comida y paseo: alta, estado del día, vencimiento con email y cumplimiento

| Métrica | Valor |
|---------|-------|
| Ticket | FEAT-001 |
| Fecha | 2026-10-25 |
| Estado | Partido (split) |

## Sub-tickets

| Sub-ticket | Título | PRD | Dependencias | Estado |
|---|---|---|---|---|
| FEAT-001a | Deberes de comida y paseo: alta, instancias y estado del día | prd-FEAT-001a.md | ninguna | done — mergeado en main (bfbc334, PR #1) |
| FEAT-001b | Cumplimiento, email de vencimiento y actualización automática del estado del día | prd-FEAT-001b.md | depende de a | active |

## Orden de implementación sugerido

a → b

> **La columna `Estado` se mantiene, no es decorativa.** El cierre de RELEASE pasa el sub-ticket
> terminado a `done` (con dónde terminó su rama) y el siguiente a `active`.

## Contexto original

Primera feature de la app (PRD-001, `docs/daw/prd/PRD.md`): que un cuidador dé de alta deberes de
comida o paseo en modalidad hora fija ("solo para hoy" o "todos los días"), los vea en el estado del
día de las mascotas con su estado (vigente, "pendiente" a los 15 minutos, expirada al terminar el
día, cumplida), reciba un email de vencimiento con reintentos cuando se olvidan, vea la pantalla
actualizada sola, marque las instancias como cumplidas con la hora real y pueda eliminar un
registro del día cargado por error. Arranca con datos precargados (1 hogar, cuidadores
`ana@ejemplo.com` y `beto@ejemplo.com`, mascotas Firulais y Michi), sin registro ni control de
acceso (riesgo aceptado: solo desarrollo local).

La versión unificada (35 FR, 6 NFR, 52 AC) pasó `daw-validate-prd` y el control de alcance la
encontró demasiado grande. El usuario decidió partirla en dos sub-tickets: **a** con alta, instancias
y estado del día, y **b** con cumplimiento y eliminación, email de vencimiento y actualización
automática.
