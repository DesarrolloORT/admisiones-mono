# 06 — Tests

Suite **xUnit + Moq** en [WebApiAdmisiones/UnitTesting](../../WebApiAdmisiones/UnitTesting). **57 archivos de test, ~769 casos `[Fact]`/`[Theory]`.** Estilo dominante: unitario con Moq sobre `IUnitOfWorkFactory`/`IUnitOfWork`. **No hay tests de integración con EF InMemory** (`UseInMemoryDatabase` no aparece); la BD siempre se mockea. Clientes HTTP se prueban con `HttpMessageHandler` falso.

Tipos: **U-Moq** (unitario con Moq) · **U-puro** (sin dependencias) · **HTTP-mock** · **Valid** (validadores) · **DI/Infra** (registro/middleware/filtros) · **Contract** (contrato JSON).

## Cobertura por clase productiva principal

| Clase productiva | Test encontrado | Tipo | Casos cubiertos | Casos faltantes | Prioridad |
|------------------|-----------------|------|-----------------|-----------------|:---:|
| AuthController | `Controllers/AuthControllerTests` (~21) | U-Moq | login éxito/fallo/2FA/captcha, cookies, refresh, logout, activar/completar password, 429 | Camino "nueva persona" completo en `CompletarPassword` (flowId inválido, pending expirado) | Media |
| PersonaController | `Controllers/PersonaControllerTests` (~8) | U-Moq | datos, inscripciones, becas, cambiar password, documentos frente/dorso | endpoint `Becas` (mock hardcodeado) sin assertion de contenido | Baja |
| InscripcionesController | `Controllers/InscripcionesControllerTests` (~6) | U-Moq | confirmar preinscripción, pagar, reglamento, detalle, interés | — | Baja |
| CatalogosController | `Controllers/CatalogosControllerTests` (~8) | U-Moq | países (cache hit/miss), encuesta, comienzos, turnos, carreras | fallback de cache que re-invoca service | Baja |
| RegistroController | `Controllers/RegistroControllerTests` (~15) | U-Moq | evaluar doc, confirmar persona/solicitud, verificar identidad, analizar adjunto + cache | fallo de `GuardarAsync` de imágenes (swallow) | Media |
| BecasController | `Controllers/BecasControllerTests` (~2) | U-Moq | inscripciones confirmadas | — | Baja |
| FondoDeBecaController | `Controllers/FondoDeBecaControllerTests` (~21) | U-Moq | catálogos, archivos ingreso/egreso/revalida, 403/404 | ⚠️ **testea endpoints comentados** (no existen en runtime) | Media (revisar) |
| AuthService | `Services/AuthServiceTests` (~20) | U-Moq | recuperar/completar password, auth LDAP, refresh, imágenes temporales | rama de fuga `ex.Message`, refresh con token reusado | Media |
| LoginFlowService | `Security/LoginFlowServiceTests` (~2) | U-Moq | score alto→login, score bajo→2FA | ⚠️ **rate-limit por cuenta/usuario/IP** (lo más complejo del método ~150 líneas) sin cubrir | **Alta** |
| DosFactoresAuthService | `Security/DosFactoresAuthServiceTests` (~11) | U-Moq | iniciar, verificar (expira/incorrecto/max), reenviar, 429 | — | Baja |
| PasswordActivationService | `AppLogic/Services/PasswordActivationServiceTests` (~11) | U-Moq | enviar mails, activar link (válido/manipulado/nueva persona), validar sesión | rama Redis crudo (flujo nueva persona) | Media |
| TokenService | `AppLogic/Services/TokenServiceTests` (~6) | U-puro | access/refresh/hash, excepción sin secret | ⚠️ `issuer`/`audience` null (bug latente L31-32) | Media |
| TokenServiceInternalApi | `AppLogic/Services/TokenServiceInternalApiTests` (~2) | U-puro | token S2S con/sin secret | — | Baja |
| RegistroService | `AppLogic/Services/RegistroServiceTests` (~18) | U-Moq | evaluar/verificar/confirmar/completar, imágenes | rollback LDAP, UoW sin `using` | Media |
| RegistroFlowService | `AppLogic/Services/RegistroFlowServiceTests` (~3) | U-Moq | actualizar pendiente, completar con cache, no borrar si falla | validación de flow/step, race de TTL | Media |
| InscripcionesService | `AppLogic/Services/InscripcionesServiceTests` (~63) | U-Moq | preinscripción, interés, encuesta, detalle por estado, pagos, método pago | — (cobertura muy amplia) | Baja |
| EncuestaInicialService | ❌ **sin test dedicado** | — | (indirecto vía InscripcionesServiceTests + `EncuestaInicialContractTests`) | `GuardarEncuestaInicial` (~119), bachillerato legacy `1304`, sincronización | **Alta** |
| CatalogosService | `AppLogic/Services/CatalogosServiceTests` (~11) | U-Moq + HTTP | catálogos, carreras, turnos nivel API vs vista | ctor sin ApiClient → 500 en turnos 1/2 | Media |
| GeneralService | `AppLogic/Services/GeneralServiceTests` (~2) | U-Moq | fecha vencimiento (sin fecha, DJ temprana) | feriados/límites de días hábiles | Media |
| PersonaService | `AppLogic/Services/PersonaServiceTests` (~28) | U-Moq | documento (vencido/faltante), subir doc, datos (identidad restringida), password | — | Baja |
| BecasService | `AppLogic/Services/BecasServiceTests` (~1) | U-Moq | solo confirmadas | filtro literal "Confirmada", niveles 3y4 | Baja |
| FondoDeBecaService | `AppLogic/Services/FondoDeBecaServiceTests` (~37) | U-Moq | catálogos, archivos (403/404/inválido), content-types | — (amplio, aunque para código desactivado) | Baja |
| RefreshTokenService | ❌ **sin test dedicado** | — | (indirecto vía AuthServiceTests) | **R1: fallo del 2º `SaveChanges`**, revocación, expiración | **Alta** |
| RedisTwoFactorSessionStore | ❌ sin test (solo mock) | — | — | serialización, sesión corrupta, TTL | Media |
| RedisHashTokenStore | ❌ sin test (solo mock) | — | — | store/get/delete, error Redis | Baja |
| RedisRateLimiterService | ❌ sin test (solo mock) | — | — | ventana, reset, remaining | Media |

## Infraestructura y utilidades — bien cubiertas

Tienen tests dedicados y robustos: `CurrentUserService` (~14+3), `LoggingHelper` (~47 entre 3 archivos), `ExceptionHandlingMiddleware` (~15), `ModelBindingErrorLoggingMiddleware` (~9), `JsonSchemaValidationFilter` (~23), `SanitizeAttribute` (~5), `ResponseRedactionHelper` (~23), `RecaptchaService` (~5), `RequireCaptchaFilter` (~7), `EfCoreLoggingInterceptor` (~13), `AuthenticationExtensions` (~12), `ServiceCollectionExtensions` (~73), `DomainServicesExtensions` (~32), `TelemetryExtensions` (~43), `MiddlewarePipelineExtensions` (~5), `FileValidationHelper` (~34), `DocumentUtils` (~9), `Util`/`ValidadorTelefonos`/`EnvioMail`/`OperationResult`/`Constantes`/`FileValidator`, y `BandejaService` (~7).

## Contract test

`AppLogic/Contracts/EncuestaInicialContractTests` (~7): valida que el JSON del contrato del front coincida con las opciones que valida el backend. **Buen patrón**, único de su tipo — considerar replicarlo para otros contratos (login, registro).

## Brechas prioritarias

| # | Brecha | Prioridad | Justificación |
|---|--------|:---:|--------------|
| 1 | `EncuestaInicialService` sin test dedicado | **Alta** | Lógica de negocio compleja (~119 líneas + bachillerato legacy) cubierta solo indirectamente. Bloquea su refactor seguro. |
| 2 | `RefreshTokenService` sin test dedicado | **Alta** | Contiene R1 (transacción). Un test que fuerce fallo del 2º `SaveChanges` demostraría el bug. Prerequisito del fix. |
| 3 | `LoginFlowService` con solo 2 casos | **Alta** | El rate-limit dual (cuenta/usuario/IP) — el núcleo del método de ~150 líneas — no se prueba. |
| 4 | Sin tests de integración de datos | Media | Todo mockeado sobre `IUnitOfWork`; los mapeos EF/Oracle (Devart) no se ejercitan. Un puñado de tests con `UseInMemoryDatabase` o base de prueba daría confianza en queries reales. |
| 5 | Componentes Redis sin test | Media/Baja | Adaptadores de infra; requieren fake de `IConnectionMultiplexer`. Menor prioridad. |
| 6 | Tests de `FondoDeBeca*` sobre código desactivado | Media | ~58 casos ejercitan endpoints comentados. Alinear con la decisión sobre el controller ([03](03-metodos-sin-referencias.md)). |
