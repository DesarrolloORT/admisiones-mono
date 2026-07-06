# 07 — Plan de refactor por etapas

Ordenado de menor a mayor riesgo. **Nada de esto se ha ejecutado** — es el plan. Cada acción indica impacto, riesgo, archivos, orden y si requiere test previo. Apoyarse en la suite existente ([06](06-tests.md)) y respetar la restricción de no tocar lógica de negocio legacy sin confirmación.

Leyenda riesgo: 🟢 bajo · 🟡 medio · 🔴 alto.

---

## Etapa 1 — Quick wins seguros

| # | Acción | Impacto | Riesgo | Archivos | ¿Test antes? |
|---|--------|---------|:---:|----------|:---:|
| 1.1 | Eliminar bloque `if (!result.Success)` duplicado e inalcanzable | Limpieza | 🟢 | `AuthController.cs:437-442` | No (código muerto probado por tests existentes) |
| 1.2 | Quitar `using System.Text.Json` sin uso | Limpieza | 🟢 | `FondoDeBecaService.cs:9` | No |
| 1.3 | Validar null de `issuer`/`audience` en `TokenService` (fallar explícito si faltan) | Corrige bug latente de tokens con claims null | 🟢 | `TokenService.cs:31-32` | Sí — agregar caso a `TokenServiceTests` |
| 1.4 | Reemplazar strings mágicos de estado por constantes ("Confirmada", "DEFINITIVO"/"D", "SI"/"NO", "SE", `TipoPago`) | Robustez, legibilidad | 🟢 | `BecasService.cs:24`, `ConfirmarPreInscripcionHelper.cs`, `EncuestaInicialValidator.cs`, `InscripcionesService.cs:24-25` | No (refactor equivalente) |
| 1.5 | Extraer valores mágicos de Tivenos a constantes | Legibilidad | 🟢 | `TivenosEnvioService.cs:76-84` | No |
| 1.6 | Decidir destino de `FondoDeBecaController` (activar / borrar / documentar) | Reduce confusión y tests-fantasma | 🟢 (documentar) / 🟡 (borrar) | `FondoDeBecaController.cs` + `FondoDeBecaService.cs` + tests | Requiere confirmación de negocio |

---

## Etapa 2 — Limpieza de código muerto

Ver [03](03-metodos-sin-referencias.md). **Nada de esta etapa se aplica sin confirmar que no hay consumidores externos.**

| # | Acción | Impacto | Riesgo | Archivos | ¿Test antes? |
|---|--------|---------|:---:|----------|:---:|
| 2.1 | Quitar dependencia inyectada `ICatalogosService` no usada de `RegistroService` | Ctor más limpio | 🟢 | `RegistroService.cs:34` + tests que construyen el service | Sí — ajustar mocks en `RegistroServiceTests` |
| 2.2 | Quitar dependencia `IDbConnectionContext` no usada de `FondoDeBecaService` | Ctor más limpio | 🟢 | `FondoDeBecaService.cs:17` + tests | Sí |
| 2.3 | Confirmar y (si aplica) quitar registro de `IGenericRepository`, `IBandejaService`, `DbConnectionContext` concreto si no tienen consumidor | Menos ruido en DI | 🟡 | `DomainServicesExtensions.cs:100,129,60` | Requiere confirmación (módulos) |
| 2.4 | Resolver métodos de `FondoDeBecaService` / `CatalogosService.ObtenerFondosDeBecaPorProducto` según decisión 1.6 | Elimina código sin ruta | 🟡 | `FondoDeBecaService.cs`, `CatalogosService.cs:300` | Requiere confirmación |

---

## Etapa 3 — Separación de responsabilidades

Extraer los métodos "god" y mover lógica de presentación/infra fuera de los servicios de dominio.

| # | Acción | Impacto | Riesgo | Archivos | ¿Test antes? |
|---|--------|---------|:---:|----------|:---:|
| 3.1 | Extraer submétodos de `LoginFlowService.EjecutarAsync` (~150): separar rate-limit dual, autenticación y decisión reCAPTCHA→2FA | Mantenibilidad, testeabilidad | 🟡 | `LoginFlowService.cs` | **Sí** — primero cubrir el rate-limit dual (brecha alta [06](06-tests.md)) |
| 3.2 | Descomponer `AuthService.CompletarPasswordAsync` (~128) en pasos (validar / persona / imágenes / LDAP / tokens) | Mantenibilidad | 🟡 | `AuthService.cs:329` | Sí — `AuthServiceTests` |
| 3.3 | Descomponer `RegistroService.CompletarNuevaPersonaAsync` (~126) y unificar wrappers LDAP con scope-factory | Mantenibilidad | 🟡 | `RegistroService.cs:308,669-710` | Sí — `RegistroServiceTests` |
| 3.4 | Descomponer `EncuestaInicialService.GuardarEncuestaInicial` (~119); aislar bachillerato legacy (`1304`) sin cambiar la fórmula | Mantenibilidad | 🔴 (legacy) | `EncuestaInicialService.cs:71,365-407` | **Sí** — crear tests dedicados primero (brecha alta) |
| 3.5 | Mover el template HTML del mail 2FA a un helper (como `PasswordMailTemplateHelper`) | Separa presentación | 🟢 | `DosFactoresAuthService.cs:388` | No (equivalente) |
| 3.6 | Mover el mock de becas de `PersonaController.ObtenerMisBecas` a un stub de servicio o marcarlo explícito | Coherencia de capas | 🟢 | `PersonaController.cs:100-134` | Requiere confirmación (¿feature futura?) |
| 3.7 | Centralizar manejo de errores: helper que loguee `ex` real y devuelva mensaje genérico (elimina fuga de `ex.Message`, R2) | **Seguridad** + consistencia | 🟡 | AuthService, RegistroService, PersonaService, PasswordActivationService | Sí — tests de que no se filtra el detalle |

---

## Etapa 4 — Mejora de tests

Prerrequisito de las etapas 3 y 5 en los puntos marcados. Ver brechas en [06](06-tests.md).

| # | Acción | Impacto | Riesgo | Archivos | Orden |
|---|--------|---------|:---:|----------|-------|
| 4.1 | Test dedicado de `RefreshTokenService` que fuerce fallo del 2º `SaveChanges` (demuestra R1) | Habilita fix 5.1 | 🟢 | nuevo `RefreshTokenServiceTests` | Antes de 5.1 |
| 4.2 | Tests dedicados de `EncuestaInicialService` (guardar, completitud, bachillerato) | Habilita 3.4 | 🟢 | nuevo test | Antes de 3.4 |
| 4.3 | Ampliar `LoginFlowServiceTests` al rate-limit dual | Habilita 3.1 | 🟢 | `LoginFlowServiceTests` | Antes de 3.1 |
| 4.4 | Introducir algunos tests de integración con `UseInMemoryDatabase` (o base de prueba) para queries EF/Devart | Confianza en mapeos reales | 🟡 | nuevo proyecto/carpeta de integración | Paralelo |
| 4.5 | Tests de comportamiento de stores Redis con fake de `IConnectionMultiplexer` | Cierra brecha infra | 🟡 | nuevos tests | Opcional |

---

## Etapa 5 — Mejoras de arquitectura

Cambios estructurales; hacer al final, con red de tests.

| # | Acción | Impacto | Riesgo | Archivos | ¿Test antes? |
|---|--------|---------|:---:|----------|:---:|
| 5.1 | **Arreglar R1**: `SaveRefreshTokenAsync` con `Remove`+`Add` en un solo `SaveChangesAsync` (o transacción explícita) | **Robustez crítica** | 🟡 | `RefreshTokenService.cs:27-35` | **Sí** (4.1) |
| 5.2 | **Arreglar R3**: envolver en transacción la aceptación de reglamento en `ConfirmarPreInscripcionHelper` y agregar `using` al UoW en `RegistroService.ConfirmarNuevaPersonaAsync` | Atomicidad | 🟡 | `ConfirmarPreInscripcionHelper.cs:163-174`, `RegistroService.cs:232` | Sí |
| 5.3 | Extraer `IInscripcionesyPagosApiClient` e inyectar por interfaz | Testeabilidad, DI | 🟡 | `InscripcionesyPagosApiClient.cs`, `HttpClientExtensions.cs`, `InscripcionesService.cs`, `CatalogosService.cs` | Sí (tests HTTP ya existen) |
| 5.4 | Dar interfaz a `EncuestaInicialService`, registrarlo en DI y **eliminar el `new` manual** en `InscripcionesService` | DI correcto | 🟡 | `EncuestaInicialService.cs`, `DomainServicesExtensions.cs`, `InscripcionesService.cs:388-405` | **Sí** (4.2) |
| 5.5 | Unificar acceso a Redis en `PasswordActivationService`: usar `IHashTokenStore`/abstracción en vez de `IConnectionMultiplexer` crudo; y unificar mail vía `IEmailSender` (quitar `EnvioMail` concreto) | Consistencia, testeabilidad | 🟡 | `PasswordActivationService.cs:341`, DI | Sí |
| 5.6 | `RegistroFlowService`: dejar de llamar métodos estáticos de `PasswordActivationService`; usar la interfaz inyectada | Desacople | 🟡 | `RegistroFlowService.cs:252-253` + mover los estáticos a la interfaz | Sí |
| 5.7 | Centralizar configuración: migrar `Environment.GetEnvironmentVariable` a `IConfiguration`/Options (TokenService, TokenServiceInternalApi, AuthService, PasswordActivationService) | Testeabilidad, uniformidad | 🟡 | varios | Sí |
| 5.8 | Investigar y, si es posible, eliminar el service-locator `IServiceScopeFactory` en `AuthService`/`RegistroService` (revisar scope real) | Salud DI | 🔴 | `AuthService.cs:599`, `RegistroService.cs:669` | Sí |
| 5.9 | Dividir `InscripcionesService` (~660 líneas) por paso del flujo (interés / encuesta / confirmación / pagos) | Mantenibilidad | 🔴 | `InscripcionesService.cs` | Sí (tests amplios ya existen, ~63) |
| 5.10 | Separar `PersonaService` en perfil / media-documentos / credenciales | Mantenibilidad | 🔴 | `PersonaService.cs` | Sí (~28 tests) |

---

## Orden global recomendado

```
Etapa 1 (todo) → Etapa 2 (lo confirmado) → Etapa 4.1-4.3 (tests que habilitan) 
→ Etapa 3 (extracciones, cada una tras su test) → Etapa 5.1-5.2 (robustez) 
→ Etapa 5.3-5.7 (desacople) → Etapa 5.8-5.10 (reestructura mayor)
```

**Principio rector:** ningún cambio de comportamiento sin test que lo respalde primero; los ítems 🔴 y los que tocan lógica legacy (bachillerato, fórmulas) requieren confirmación explícita antes de tocarse (ver memoria del proyecto: *preservar LogicaORT legacy*).
