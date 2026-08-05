# AppLogic.Platform

**Nivel 2 — depende de `AppLogic.Contracts` y de `Core` (MailORT, Utilities).**

## Qué resuelve

Capacidades **técnicas** transversales, sin nada de dominio de admisiones. Si un tipo de acá
menciona "inscripción", "beca" o "persona", está en el proyecto equivocado.

Es lo opuesto a `AppLogic.Contracts`: aquel guarda *contratos de dominio compartidos*, éste guarda
*infraestructura compartida*.

## Qué expone

| Carpeta | Tipo | Para qué |
|---|---|---|
| `Email/` | `IEmailSender`, `OrtEmailSender` | Envío de mails vía `Core/MailORT`. Lo usan Authentication (activación, recupero, 2FA) y Enrollments. |
| `RateLimiting/` | `IRateLimiterService`, `RedisRateLimiterService` | Contador de intentos en Redis con ventana deslizante. Devuelve `RateLimitValidationResult` con `IsAllowed`, `RemainingAttempts`, `PartitionKey` y `ResetTime`. |
| `Serialization/` | `JsonSerialization`, `JsonSerializationDefaults` | Opciones de `System.Text.Json` centralizadas. `JsonSerializationDefaults.Redis` es la que usan todos los stores de Redis: si cambia, cambia el formato de todo lo persistido. |

## Trampas

- **`JsonSerializationDefaults.Redis` es un contrato de datos, no una preferencia.** Todo lo que hay
  en Redis (sesiones 2FA, flujo de registro, personas pendientes, imágenes temporales) está
  serializado con esas opciones. Cambiarlas invalida lo que ya está guardado.
- El rate limiter usa claves con prefijo `ratelimit:{…}`. Las claves concretas del login
  (`login-account:`, `login-ip:`, `login-fail-cred-user:`, `login-fail-cred-ip:`) las arma
  Authentication, no este módulo.
- `OrtEmailSender` envuelve `Core/MailORT`. El armado del **cuerpo** de cada mail es
  responsabilidad de quien lo manda (ver `Authentication/Rules/PasswordMailTemplate`), no de acá.

## Dónde tocar si…

- …querés cambiar cómo se serializa algo en Redis → `Serialization/JsonSerializationDefaults.cs`,
  pero pensá en la migración de lo que ya está guardado.
- …necesitás limitar intentos de un endpoint nuevo → inyectá `IRateLimiterService`; el límite y la
  ventana los define el que llama, no este módulo.
