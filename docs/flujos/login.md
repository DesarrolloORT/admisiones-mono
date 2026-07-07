# Login

## Objetivo

Permitir que una persona con cuenta activa inicie sesion, complete 2FA cuando
corresponda y acceda a rutas protegidas.

## Entrada del usuario

Pantalla `/iniciar-sesion`, formulario de tipo de documento, numero de documento
y password. Si el backend exige segundo factor, el usuario continua por
`/confirmacion-correo/verificar-codigo` y `/verificar-codigo`.

## Recorrido frontend

- Ruta o feature Angular: `src/app/features/auth/auth.routes.ts`.
- Componentes principales: `Login`, `EmailConfirmation` y `TwoFactorValidationPage`.
- Servicio de feature: `AuthSessionService`.
- Endpoint adapter: `AuthEndpoint`.

Detalle vigente: [Inicio de sesion](../LOGIN-FLOW.md).

## Recorrido backend

- Controller o endpoint backend: `POST /Auth/Login`,
  `POST /Auth/VerificarCodigo2FA`, `POST /Auth/ReenviarCodigo2FA`,
  `POST /Auth/RefreshToken` y `POST /Auth/Logout`.
- Service o caso de uso principal: autenticacion, 2FA por correo y refresh de
  token en `api-admisiones`.
- DTOs/contratos relevantes: resultado de login autenticado o con 2FA pendiente,
  verificacion de codigo y refresh por cookies HttpOnly.
- Link al repo backend:
  [DesarrolloORT/api-admisiones](https://github.com/DesarrolloORT/api-admisiones).

## Estados y errores

- Validaciones visibles: documento, password y codigo 2FA de seis digitos.
- Errores esperados: credenciales invalidas, rate limit, codigo 2FA invalido o
  sesion expirada.
- Mensajes al usuario: se normalizan desde `AuthEndpoint` y la UI mantiene la
  sesion local solo para presentacion y guards.

## Accesibilidad

Los campos deben mantener labels visibles o accesibles, foco operativo por
teclado y errores asociados al control. El flujo 2FA permite pegar el codigo y
mantiene navegacion de foco entre inputs.

## Pendientes

- TODO: confirmar URL publicada de Swagger/OpenAPI del backend.
