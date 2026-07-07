# Inscripciones

## Objetivo

Permitir que una persona autenticada seleccione propuesta academica, complete
datos requeridos, confirme la preinscripcion y elija un metodo de pago o reserva.

## Entrada del usuario

Ruta protegida `/inscripciones`. El flujo inicia con seleccion de propuesta,
continua con encuesta/datos personales/identidad/reglamento y termina en pago o
estado terminal.

## Recorrido frontend

- Ruta o feature Angular: `src/app/features/inscriptions/inscriptions.routes.ts`.
- Componentes principales: `Inscripcion`, pasos de propuesta academica,
  informacion personal, confirmacion, reserva y exito.
- Servicio de feature: `Inscripciones`.
- Endpoint adapter: `InscripcionesEndpoint`.

Detalle vigente:
[Flow manual de inscripciones](../../src/app/features/inscriptions/INSCRIPCIONES-FLOW.md).

## Recorrido backend

- Controller o endpoint backend: `POST /Inscripciones/InteresProducto`,
  `GET/POST /Inscripciones/EncuestaInicial`,
  `POST /Inscripciones/ConfirmarPreInscripcion`,
  `POST /Inscripciones/Pagar`, `POST /Persona/SubirDocumento` y
  `POST /Persona/SubirFoto`.
- Service o caso de uso principal: interes de producto, encuesta inicial,
  validacion de identidad, reglamento, preinscripcion y pago.
- DTOs/contratos relevantes: interes de producto, encuesta inicial,
  confirmacion de preinscripcion, pago y archivos de identidad.
- Link al repo backend:
  [DesarrolloORT/api-admisiones](https://github.com/DesarrolloORT/api-admisiones).

## Estados y errores

- Validaciones visibles: cascada de propuesta, campos condicionales de encuesta,
  identidad, aceptacion de reglamento y metodo de pago.
- Errores esperados: carga de identidad fallida, encuesta no guardada,
  preinscripcion no confirmada, metodo de pago sin callback automatico o pago
  externo pendiente.
- Mensajes al usuario: los errores bloquean el avance del paso y conservan el
  borrador local cuando aplica.

## Accesibilidad

Los pasos deben mantener encabezados claros, controles con nombre accesible,
errores por seccion y navegacion de teclado. Los campos condicionales no deben
dejar controles ocultos como unica fuente de informacion.

## Pendientes

- TODO: confirmar URL publicada de Swagger/OpenAPI del backend.
- TODO: confirmar callback o consulta automatica para pagos externos.
