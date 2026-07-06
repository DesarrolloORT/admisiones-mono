# Auditoría técnica — Resumen ejecutivo

> **Alcance:** análisis estático de `WebApiAdmisiones` (Web API .NET 10) y su submódulo `Core`.
> **Método:** lectura de `.sln`/`.csproj`, `Program.cs`, `Extensions`, 8 controllers, ~21 servicios de `AppLogic`/`DataAccess`, helpers, registro DI y 57 archivos de test.
> **Fecha:** 2026-07-06. **No se modificó código.** Los hallazgos citan archivo y línea aproximada; lo no verificable con certeza se marca *requiere confirmación*.

Documentos de detalle:
- [01 — Matriz de controllers](01-matriz-controllers.md)
- [02 — Matriz de servicios](02-matriz-services.md)
- [03 — Métodos sin referencias](03-metodos-sin-referencias.md)
- [04 — Dependencias y arquitectura](04-dependencias-arquitectura.md)
- [05 — Inyección de dependencias](05-di.md)
- [06 — Tests](06-tests.md)
- [07 — Plan de refactor](07-plan-refactor.md)

---

## Foto general

Arquitectura en capas correcta y consistente: `WebApi → AppLogic → BusinessLogic ← DataAccess`, con `Core` como submódulo compartido. **No se detectaron violaciones graves de dependencias entre proyectos** (ver [04](04-dependencias-arquitectura.md)): ningún controller toca `DbContext` ni `DataAccess` directamente, ningún proyecto de capa baja referencia a la Web.

El proyecto tiene una **base de seguridad sólida** (rate limiting dual OWASP, reCAPTCHA, cookies HttpOnly, 2FA por email, sanitización de input, redacción de logs, validación de esquemas JSON, autorización a nivel de dato en fondo de becas) y una **suite de tests amplia** (~769 casos, todos los controllers y 15/21 servicios con test dedicado).

Los problemas son mayormente de **calidad interna y consistencia**, no de arquitectura macro: servicios sobrecargados con métodos largos, algunas clases sin interfaz instanciadas con `new`, código muerto/desactivado, y fugas de detalle de excepción al cliente.

## Principales riesgos

| # | Riesgo | Severidad | Referencia |
|---|--------|-----------|-----------|
| R1 | `RefreshTokenService.SaveRefreshTokenAsync` hace dos `SaveChangesAsync` sin transacción: si falla el segundo, la persona queda sin ningún refresh token. | **Alta** | [02](02-matriz-services.md), `DataAccess/Services/RefreshTokenService.cs:27-35` |
| R2 | Fuga de `ex.Message` al cliente en múltiples `catch` (AuthService, RegistroService, PersonaService, PasswordActivationService). Information disclosure. | **Alta** | [02](02-matriz-services.md) |
| R3 | Escrituras no atómicas dentro de flujos no transaccionales: `ConfirmarPreInscripcionHelper` commitea la aceptación de reglamento por separado; `RegistroService.ConfirmarNuevaPersonaAsync` crea el UoW sin `using`. | **Alta** | `AppLogic/Helpers/ConfirmarPreInscripcionHelper.cs:163-174`; `RegistroService.cs:232` |
| R4 | Datos personales sensibles (imágenes de cédula, personas pendientes, tokens de sesión 2FA) en Redis sin cifrado explícito. | Media | `RegistroDocumentoImagenCacheService.cs`, `RedisTwoFactorSessionStore.cs` |
| R5 | Posible enumeración de usuarios en login (404 "persona no existe" vs 401 "credenciales"), mitigada solo por rate limit. | Media | `AuthService.cs:~100` |
| R6 | Métodos "god": `LoginFlowService.EjecutarAsync` (~150), `AuthService.CompletarPasswordAsync` (~128), `RegistroService.CompletarNuevaPersonaAsync` (~126), `EncuestaInicialService.GuardarEncuestaInicial` (~119). Difíciles de mantener y probar exhaustivamente. | Media | [02](02-matriz-services.md) |
| R7 | Uso de `IServiceScopeFactory` como service-locator en `AuthService`/`RegistroService` para resolver UoW en runtime → posible problema de scoping DI encubierto. | Media | `AuthService.cs:599-604`, `RegistroService.cs:669-710` |
| R8 | `FondoDeBecaController` con **todos** sus endpoints comentados → `FondoDeBecaService` (13 métodos) sin consumidor en runtime. Código desactivado en `main`. | Media | [03](03-metodos-sin-referencias.md), `Controllers/FondoDeBecaController.cs` |

## Principales mejoras (estructurales)

1. **Extraer interfaces faltantes** para `InscripcionesyPagosApiClient` y `EncuestaInicialService`, y **dejar de hacer `new EncuestaInicialService(...)`** dentro de `InscripcionesService` (romper el anti-patrón de instanciación manual). Habilita mocking y respeta DI.
2. **Unificar la fuente de configuración**: hoy conviven `IConfiguration` y `Environment.GetEnvironmentVariable` para secrets/expiraciones (TokenService, TokenServiceInternalApi, parte de AuthService). Centralizar en `IConfiguration`/Options.
3. **Unificar el acceso a Redis**: hay tres vías (`ITwoFactorSessionStore`, `IHashTokenStore`, `IConnectionMultiplexer` crudo en `PasswordActivationService`). El uso crudo rompe la abstracción.
4. **Dividir los servicios sobrecargados** (`InscripcionesService`, `PersonaService`, `RegistroService`) por responsabilidad; extraer los métodos >60 líneas.
5. **Manejo de errores estándar**: helper único que loguee la excepción real y devuelva un mensaje genérico al cliente (elimina R2 de un plumazo).

## Quick wins (bajo riesgo, alto orden)

| Quick win | Archivo | Riesgo |
|-----------|---------|--------|
| Eliminar bloque `if (!result.Success)` duplicado (código muerto inalcanzable) | `AuthController.cs:437-442` | Nulo |
| Quitar dependencia inyectada no usada `ICatalogosService` en `RegistroService` | `RegistroService.cs:34` | Bajo (cambia ctor + DI test) |
| Quitar dependencia inyectada no usada `IDbConnectionContext` y `using System.Text.Json` en `FondoDeBecaService` | `FondoDeBecaService.cs:9,17` | Bajo |
| Envolver `SaveRefreshTokenAsync` en una transacción / `Remove`+`Add` en un solo `SaveChanges` (arregla R1) | `RefreshTokenService.cs:27-35` | Medio (requiere test antes) |
| Reemplazar strings mágicos de estado ("Confirmada", "DEFINITIVO"/"D", "SI"/"NO", "SE") por constantes | varios | Bajo |
| Validar null de `issuer`/`audience` en `TokenService` (hoy emite tokens con claims null en silencio) | `TokenService.cs:31-32` | Bajo |
| Decidir destino de `FondoDeBecaController` (activar, borrar o documentar por qué está comentado) | `FondoDeBecaController.cs` | Bajo |

## Severidad global

**Media.** No hay fallas de arquitectura que exijan reescritura. La deuda es acotada y localizada: robustez transaccional (R1, R3), higiene de errores (R2) y tamaño de servicios (R6). Con el plan por etapas de [07](07-plan-refactor.md) se puede reducir de forma incremental y segura, apoyándose en la suite de tests ya existente.
