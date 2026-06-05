# Flujo de registro de usuario

> Tipo: explanation

Este documento describe como el frontend decide cada caso de registro despues de
`POST /Registro/EvaluarDocumento`.

## Decision principal

La decision vive en `resolveRegisterFlow(...)`. La UI no deberia consumir los
flags crudos del backend directamente; primero se traducen a un `RegisterFlowKind`
estable para que la fachada pueda operar por caso de uso.

| Prioridad | Condicion                                          | Flujo frontend       | Siguiente paso                                    |
| --------- | -------------------------------------------------- | -------------------- | ------------------------------------------------- |
| 1         | `usuarioExistente`                                 | `user-exists`        | Mostrar mensaje y dejar visible el acceso a login |
| 2         | `solicitudAltaExistente`                           | `application-exists` | Mostrar mensaje de solicitud pendiente            |
| 3         | `tipoDocumento === 'CI'` y `requiereVerificacion`  | `existing-person`    | Pedir apellido + email, verificar identidad       |
| 4         | `tipoDocumento === 'CI'` y `requiereAltaPersona`   | `new-person`         | Pedir datos completos                             |
| 5         | `tipoDocumento !== 'CI'` y `requiereAltaSolicitud` | `new-application`    | Pedir datos completos                             |

Si ninguna condicion matchea, la pantalla muestra un error funcional y no avanza.

## Endpoints por caso

| Flujo                | Tipo de documento | Endpoint                                                                              |
| -------------------- | ----------------- | ------------------------------------------------------------------------------------- |
| `existing-person`    | `CI`              | `POST /Registro/VerificarIdentidad`, luego `POST /Registro/ConfirmarPersonaExistente` |
| `new-person`         | `CI`              | `POST /Registro/ConfirmarNuevaPersona`                                                |
| `new-application`    | distinto de `CI`  | `POST /Registro/ConfirmarSolicitudAlta`                                               |
| `user-exists`        | cualquiera        | No confirma registro; la accion esperada es ir a login                                |
| `application-exists` | cualquiera        | No confirma registro; se informa que la solicitud ya existe                           |

## Campos por paso

| Paso              | Flujo                                              | Campos requeridos                                                      |
| ----------------- | -------------------------------------------------- | ---------------------------------------------------------------------- |
| Identidad         | Todos                                              | Tipo y numero de documento                                             |
| Verificacion      | `existing-person`                                  | Primer apellido, email y confirmacion de email                         |
| Datos personales  | `new-person`, `new-application`                    | Datos personales, ubicacion, direccion, telefono, email y confirmacion |
| Interes academico | `existing-person`, `new-person`, `new-application` | Propuesta academica, carrera y comienzo                                |

## OCR

`POST /Registro/AnalizarAdjunto` solo precarga campos del formulario. No decide el
flujo funcional ni reemplaza `POST /Registro/EvaluarDocumento`; el usuario siempre
debe continuar desde identidad para que el backend indique el caso real.

