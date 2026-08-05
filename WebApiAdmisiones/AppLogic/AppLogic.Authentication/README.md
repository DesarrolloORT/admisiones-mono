# AppLogic.Authentication

**Nivel 4 — depende de `AppLogic.Contracts`, `AppLogic.Identity` y `AppLogic.Platform`.**
Es el módulo más grande (~3.000 líneas).

## Qué resuelve

Login, segundo factor, tokens de sesión, alta de la contraseña inicial y recupero. Todo lo que pasa
**antes** de que exista una sesión válida, más la renovación de esa sesión.

La contraseña vive en **LDAP**, no en la base de admisiones. Acá solo se guarda metadata (fecha y
usuario de última modificación).

## Los 3 flujos que hay que entender

### 1. Login (con gate de 2FA)

```
ILoginFlowService.ExecuteAsync(request, ip, recaptchaScore)
  → IAuthenticateWithLdap        valida credenciales, NO emite tokens
  → si score de captcha bajo     → ITwoFactorAuthService.StartAsync → 202 Accepted + sessionId
  → si no                        → IIssueTokensForPerson → 200 + cookies
```

**`AuthenticateWithLdap` no emite tokens a propósito.** Emitirlos es decisión del llamador, y solo
cuando el login está completo. Si los emitiera, un login con 2FA pendiente ya tendría sesión válida.

El segundo factor se cierra con `ITwoFactorAuthService.VerifyCodeAsync`, que sí llama a
`IIssueTokensForPerson`.

### 2. Contraseña inicial (dos caminos)

`ICompletePasswordFlow.ExecuteAsync(sessionToken, request)` valida la sesión temporal y bifurca según
el `Purpose` del token:

- **Persona nueva** (`"nueva-persona-session"`): los datos están en Redis. Delega en
  `IPendingRegistrationCompletion` (que implementa Registration) para crear la persona, y recién
  después emite tokens y limpia el pending.
- **Persona existente**: cambia la contraseña en LDAP, persiste metadata e imágenes, consume el link
  y emite tokens.

Devuelve `CompletePasswordFlowResult`, que además del `OperationResult` dice si el controller tiene
que limpiar la cookie de activación (decisión HTTP que sigue siendo del controller).

### 3. Recupero

`IRecoverPassword` **responde siempre lo mismo**: persona inexistente, datos que no coinciden o
excepción interna dan todos el mismo mensaje genérico y `Success = true`. Es deliberado: el endpoint
es público y no debe permitir enumerar cuentas. Si vas a "mejorar" el manejo de errores acá, no lo
hagas.

## Qué expone

### Casos de uso (`Contracts/IAuthenticationUseCases.cs`)

| Interfaz | Endpoint |
|---|---|
| `IAuthenticateWithLdap` | interno, lo usa `LoginFlowService` |
| `IIssueTokensForPerson` | interno, único punto de emisión de tokens |
| `IRefreshTokens` | `POST auth/refresh-token` |
| `IRecoverPassword` | `POST auth/recover-password` |
| `ICompletePasswordFlow` | `POST auth/complete-initial-password` |

### Servicios

`ILoginFlowService`, `ITwoFactorAuthService`, `IPasswordActivationService`, `ITokenService`,
`ITokenServiceInternalApi`, `IHashTokenStore`, `ITwoFactorSessionStore`, y el colaborador concreto
`SessionTokenIssuer`.

Se registra todo con `services.AddAuthenticationModule()`.

## `SessionTokenIssuer`: el único lugar que emite tokens

Genera access + refresh, hashea el refresh y lo persiste con `SystemName = "ADMISIONESWEB"`. Lo
comparten los tres caminos que terminan con sesión iniciada: login, refresh y contraseña inicial.

Recibe `IRefreshTokenService` **por parámetro** y no inyectado, porque `IssueTokensForPerson` puede
tener que resolverlo desde un scope propio (ver más abajo).

## `IPendingRegistrationCompletion`: inversión de dependencia

Este módulo **no puede referenciar** `AppLogic.Registration` (sería un ciclo: Registration ya lo
referencia a él). Pero el flujo de contraseña inicial de persona nueva necesita que Registration cree
la persona.

Solución: la interfaz se **declara acá** con las 4 operaciones que hacen falta, y Registration la
implementa. El binding vive en el composition root de Registration:

```csharp
services.AddScoped<IPendingRegistrationCompletion>(sp =>
    sp.GetRequiredService<IRegistrationFlowService>());
```

Si necesitás algo más de Registration desde acá, agregalo a esa interfaz — nunca una referencia de
proyecto.

## Trampas

- **`IServiceScopeFactory` opcional.** `IssueTokensForPerson` lo recibe como parámetro opcional: si
  está, resuelve la uow y el servicio de refresh tokens en un scope propio, porque la emisión puede
  dispararse desde un flujo cuyo scope de request ya se cerró (2FA, alta de persona nueva). En tests
  se pasa null y se usa lo inyectado. Si lo sacás, el 2FA rompe en runtime pero no en los tests.
- **Dos literales de `Method` están congelados a propósito**: `"CompleteInitialPassword"` y
  `"CompletarPasswordAsync"` en `CompletePasswordFlow`. Tienen comentario explicándolo.
- **Estado inconsistente documentado**: en el camino de persona existente, si LDAP ya cambió la
  contraseña (irreversible) y falla el `uow.Save()`, se loguea y se **relanza** la excepción a
  propósito, dejando el link de activación vigente para que el usuario reintente. No lo conviertas en
  un `return` silencioso.
- El link de activación se consume **después** de confirmar la persistencia en base, nunca antes.

## Variables de entorno que necesita

`JWT_SECRET_KEY`, `JWT_ISSUER_TOKEN_ADMISIONES`, `JWT_AUDIENCE_TOKEN_ADMISIONES`,
`JWT_EXPIRE_MINUTES_ADMISIONES`, `JWT_REFRESH_EXPIRE_ADMISIONES`, `PASSWORD_ACTIVATION_SECRET_KEY`,
`SECRET_KEY_API_INSCR_PAGOS` (este último para `TokenServiceInternalApi`, que firma tokens
servicio-a-servicio hacia Inscripciones y Pagos; **solo los genera, no los valida**).

Se leen centralizadamente en `Security/JwtConfiguration`.

## Claves de Redis

`registro:hash-token:{personId}` (link de activación), sesiones 2FA, y los contadores de rate limit
del login: `login-account:{…}`, `login-ip:{…}`, `login-fail-cred-user:{…}`, `login-fail-cred-ip:{…}`.

## Códigos de error

`LOGIN_LDAP_*`, `AUTH_2FA_*`, `AUTH_RL_*`, `REFRESH_TOKEN_*`, `GEN_TOK_*`, `INI_PAS_*`, `ACT_LINK_*`,
`ACT_SES_*`, `ACT_PAS_*`, `ACT_NUP_*`, `NUP_COMP_*`, `REC_PAS_*`, `REC_LINK_*`.
