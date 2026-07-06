# 04 — Dependencias y arquitectura

Grafo de referencias entre proyectos, extraído de los `.csproj`. Arquitectura en capas con `Core` como submódulo compartido.

```
WebApiAdmisiones (Web / composition root, net10.0)
 ├─→ AppLogic ──────────→ BusinessLogic (entities + interfaces IServices/IRepositories)
 ├─→ DataAccess ────────→ BusinessLogic        (implementa repos/UoW)
 ├─→ Core/AzureService, MailORT, LdapService, Utilities, ConnectionContext
 └─→ Core/Modules/ModBandeja (AppLogic+BusinessLogic+DataAccess), ModGenericBase/DataAccess

AppLogic  ──→ BusinessLogic, Core/{ConnectionContext, LdapService, MailORT, ModBandejaAppLogic, Utilities}
DataAccess ──→ BusinessLogic, Core/ConnectionContext
BusinessLogic ──→ (nada; solo entidades e interfaces)
UnitTesting ──→ todo
```

**Regla de capas esperada:** `Web → AppLogic → BusinessLogic ← DataAccess`. `AppLogic` **no** referencia a `DataAccess` (bien: se comunican por las interfaces de `BusinessLogic`). Solo el composition root (`Web`) conoce `DataAccess` para registrar implementaciones en DI.

## Tabla de dependencias

| Proyecto origen | Proyecto destino | Tipo | ¿Esperada? | Problema | Recomendación |
|-----------------|------------------|------|:---:|---------|---------------|
| WebApiAdmisiones | AppLogic | ProjectReference | ✅ Sí | — | — |
| WebApiAdmisiones | DataAccess | ProjectReference | ✅ Sí | Necesaria en el composition root para registrar `EntityFrameworkUnitOfWorkFactory`, `GenericRepository`, `RefreshTokenService` y los `DbContext`. | Mantener el uso limitado a `DomainServicesExtensions`. No usar tipos de `DataAccess` en controllers. |
| WebApiAdmisiones | BusinessLogic | transitiva (vía AppLogic/DataAccess) | ✅ Sí | — | — |
| WebApiAdmisiones | Core/AzureService, MailORT, LdapService, Utilities, ConnectionContext | ProjectReference | ✅ Sí | — | — |
| WebApiAdmisiones | Core/Modules/ModBandeja{AppLogic,BusinessLogic,DataAccess}, ModGenericBaseDataAccess | ProjectReference | ✅ Sí (módulos) | `BandejaService` se registra pero el API no lo consume (ver [05](05-di.md)). | Confirmar si ModBandeja está en uso o pendiente de cableado. |
| AppLogic | BusinessLogic | ProjectReference | ✅ Sí | — | — |
| AppLogic | Core/ConnectionContext, LdapService, MailORT, ModBandejaAppLogic, Utilities | ProjectReference | ✅ Sí | `PasswordActivationService` depende de la **clase concreta** `EnvioMail` (MailORT) en lugar de `IEmailSender`. Acoplamiento a implementación, no violación de capa. | Inyectar `IEmailSender` (ya existe y se usa en `DosFactoresAuthService`). |
| AppLogic | DataAccess | **ausente** | ✅ Correcto | Ninguno — es lo deseable. | Mantener. |
| DataAccess | BusinessLogic | ProjectReference | ✅ Sí | — | — |
| DataAccess | Core/ConnectionContext | ProjectReference | ✅ Sí | — | — |
| DataAccess | AppLogic | **ausente** | ✅ Correcto | **No hay** `DataAccess → AppLogic` (violación clásica evitada). | Mantener. |
| BusinessLogic | (ninguno) | — | ✅ Sí | Capa de contratos pura. | Mantener. |
| Core/* | WebApiAdmisiones | **ausente** | ✅ Correcto | **No hay** `Core → Web` (violación evitada). | Mantener. |

## Verificaciones específicas solicitadas

| Chequeo | Resultado | Evidencia |
|---------|-----------|-----------|
| ¿`DataAccess` depende de `AppLogic`? | ❌ **No** (correcto) | `DataAccess.csproj` solo referencia `BusinessLogic` + `ConnectionContext`. |
| ¿`BusinessLogic` depende de `WebApi`? | ❌ **No** (correcto) | `BusinessLogic.csproj` no tiene `ProjectReference`. |
| ¿`Core` depende de `WebApi`? | ❌ **No** (correcto) | Ningún `.csproj` de `Core` referencia la Web. |
| ¿Controllers acceden a `DbContext`? | ❌ **No** (correcto) | Los 8 controllers solo inyectan servicios de `AppLogic` + transversales. Ver [01](01-matriz-controllers.md). |
| ¿Controllers acceden directo a `DataAccess`/repos? | ❌ **No** (correcto) | Ningún controller inyecta repos ni `IUnitOfWork`. |
| ¿Servicios con demasiadas dependencias? | ⚠️ **Por alcance, no por número** | Máximos: `AuthService` 10 (4 opcionales), `RegistroService`/`RegistroFlowService` 6, `LoginFlowService`/`DosFactoresAuthService`/`PasswordActivationService`/`InscripcionesService` 5. Ninguno es extremo en número, pero `AuthService`, `InscripcionesService`, `RegistroService`, `PersonaService` y `EncuestaInicialService` concentran múltiples responsabilidades (ver [02](02-matriz-services.md)). |

## Hallazgos de acoplamiento interno (dentro de capa, no entre proyectos)

Estos no son violaciones de capa, pero degradan testeabilidad:

1. **`InscripcionesService` → `new EncuestaInicialService(...)`** ([InscripcionesService.cs:388-405](../../WebApiAdmisiones/AppLogic/Services/Inscripciones/InscripcionesService.cs)): instanciación manual dentro de `AppLogic`, sin pasar por DI. `EncuestaInicialService` no tiene interfaz.
2. **`InscripcionesService` y `CatalogosService` → `InscripcionesyPagosApiClient` (clase concreta)**: el typed HttpClient no expone interfaz. No se puede mockear en test sin `HttpMessageHandler` falso.
3. **`RegistroFlowService` → métodos estáticos de `PasswordActivationService`** ([:252-253](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroFlowService.cs#L252-L253)): usa estáticos en lugar de la interfaz que ya tiene inyectada.
4. **Uso de `IServiceScopeFactory` como service-locator** en `AuthService` y `RegistroService` para resolver `IUnitOfWorkFactory`/`IRefreshTokenService` en runtime — indica un posible desajuste de scope (servicios Scoped resueltos desde un contexto donde el scope no está disponible). Revisar si es evitable con inyección directa.

## Conclusión

La **macro-arquitectura es correcta y disciplinada**: sin dependencias circulares ni inversiones de capa. La deuda de dependencias es **intra-capa** (falta de interfaces, instanciación manual, service-locator) y se aborda en la Etapa 5 de [07](07-plan-refactor.md).
