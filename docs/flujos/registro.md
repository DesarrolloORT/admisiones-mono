# Registro

## Objetivo

Crear una cuenta o solicitud de alta segun el documento ingresado, sin duplicar
personas, usuarios ni solicitudes existentes.

## Entrada del usuario

Desde `/iniciar-sesion`, accion **Crear cuenta** hacia `/registro`. El usuario
ingresa tipo y numero de documento; luego completa datos personales,
verificacion de identidad o solicitud de alta segun la evaluacion del backend.

## Recorrido frontend

- Ruta o feature Angular: `src/app/features/auth/auth.routes.ts`.
- Componentes principales: `Register`, pasos de identidad y datos personales,
  `EmailConfirmation` y `SetPassword`.
- Servicio de feature: `RegistrationService` coordinado por
  `RegisterFlowFacade`.
- Endpoint adapter: `AuthEndpoint`.

Detalle vigente: [Registro punta a punta](../REGISTER-FLOW.md).

## Recorrido backend

- Controller o endpoint backend: `POST /Registro/EvaluarDocumento`,
  `POST /Registro/VerificarIdentidad`,
  `POST /Registro/ConfirmarNuevaPersona`,
  `POST /Registro/ConfirmarSolicitudAlta`,
  `POST /Auth/ActivarLinkPassword` y `POST /Auth/CompletarPassword`.
- Service o caso de uso principal: evaluacion de documento, sesion de flujo,
  alta de usuario/persona o solicitud, y activacion de password.
- DTOs/contratos relevantes: requests de registro, `X-Flow-Id`, token de
  activacion y cookie temporal `X-Password-Activation`.
- Link al repo backend:
  [DesarrolloORT/api-admisiones](https://github.com/DesarrolloORT/api-admisiones).

## Estados y errores

- Validaciones visibles: documento, email, apellido para persona existente,
  datos personales completos y password.
- Errores esperados: usuario existente, solicitud existente, documento invalido,
  `flowId` ausente o expirado, datos que no coinciden y token de activacion
  vencido o reutilizado.
- Mensajes al usuario: el frontend traduce la evaluacion a un
  `RegisterFlowKind` estable antes de habilitar el siguiente paso.

## Accesibilidad

El formulario debe conservar orden logico de foco, errores por campo y estados
terminales comprensibles sin depender solo de color. Las confirmaciones por
correo deben tener texto de accion claro.

## Pendientes

- TODO: confirmar URL publicada de Swagger/OpenAPI del backend.
- TODO: ajustar el mensaje terminal de `new-application` cuando el backend no
  envia correo de activacion.
