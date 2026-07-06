# 03 — Métodos y símbolos sin referencias

> **Regla aplicada:** NO se marca como eliminable ningún método público de controller, endpoint, método de interfaz, método usable por reflexión/serialización o consumible externamente. Los métodos públicos de servicio detrás de endpoints comentados se marcan **requiere confirmación** (pueden reactivarse o consumirse fuera del repo). El análisis es estático; los "0 referencias" se verificaron sobre este repositorio y pueden existir consumidores externos.

## Código muerto seguro (dentro de método)

| Archivo | Clase | Símbolo | Visibilidad | Refs | ¿Eliminable? | Motivo | Riesgo |
|---------|-------|---------|-------------|:---:|:---:|--------|--------|
| [AuthController.cs:437-442](../../WebApiAdmisiones/WebApiAdmisiones/Controllers/AuthController.cs#L437-L442) | `AuthController` | segundo bloque `if (!result.Success)` en `RefreshToken` | privado (cuerpo) | 0 (inalcanzable) | **Sí** | Duplicado exacto del bloque anterior (L430-435); es inalcanzable porque el primero ya hizo `return`. Código muerto puro. | Nulo |

## Dependencias inyectadas no usadas (cambian el ctor + tests DI)

| Archivo | Clase | Símbolo | Visibilidad | Refs | ¿Eliminable? | Motivo | Riesgo |
|---------|-------|---------|-------------|:---:|:---:|--------|--------|
| [RegistroService.cs:34](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L34) | `RegistroService` | parámetro ctor `ICatalogosService catalogosService` | ctor | 0 (nunca asignado a campo) | **Sí** (bajo riesgo) | Se recibe pero no se guarda ni usa. | Bajo — cambia firma del ctor y los tests que lo construyen. |
| [FondoDeBecaService.cs:17](../../WebApiAdmisiones/AppLogic/Services/Becas/FondoDeBecaService.cs#L17) | `FondoDeBecaService` | dependencia `IDbConnectionContext` | campo/ctor | 0 (nunca invocado) | **Sí** (bajo riesgo) | Inyectada pero ningún método la usa. | Bajo. |
| [FondoDeBecaService.cs:9](../../WebApiAdmisiones/AppLogic/Services/Becas/FondoDeBecaService.cs#L9) | — | `using System.Text.Json` | using | 0 | **Sí** | Import sin uso. | Nulo. |
| [PasswordActivationService.cs:529](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L529), [:582](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L582) | `PasswordActivationService` | `GenerarTokenFlowIdPublic`, `ObtenerSecretKeyPublic` (`internal`) | internal | usados por tests **y** por `RegistroFlowService` (estático) | **No** | Se usan; pero son un olor: exponen generación de token/secret solo para tests. Refactor, no borrado. | Medio (rompe RegistroFlowService y tests). |

## Métodos públicos de servicio sin consumidor en runtime (requieren confirmación)

Todos derivan de que **`FondoDeBecaController` tiene sus endpoints comentados** ([FondoDeBecaController.cs](../../WebApiAdmisiones/WebApiAdmisiones/Controllers/FondoDeBecaController.cs)). Los métodos son de interfaz (`IFondoDeBecaServices`) y tienen tests, por eso **no** se marcan eliminables automáticamente.

| Archivo | Clase | Método | Visibilidad | Refs (repo) | ¿Eliminable? | Motivo | Riesgo |
|---------|-------|--------|-------------|:---:|:---:|--------|--------|
| [FondoDeBecaService.cs:85-372](../../WebApiAdmisiones/AppLogic/Services/Becas/FondoDeBecaService.cs) | `FondoDeBecaService` | `SubirArchivoIngreso`, `DescargarArchivoIngreso`, `EliminarArchivoIngreso`, `SubirArchivoEgreso`, `DescargarArchivoEgreso`, `EliminarArchivoEgreso`, `SubirArchivoRevalidaDJ`, `DescargarArchivoRevalidaDJ`, `EliminarArchivoRevalidaDJ` | public (interfaz) | solo interfaz + tests (endpoint comentado L100-293) | **Requiere confirmación** | El endpoint existe comentado → funcionalidad pausada, no descartada. | Alto si se borra y luego se reactiva la feature. |
| [FondoDeBecaService.cs:27,36,48,61](../../WebApiAdmisiones/AppLogic/Services/Becas/FondoDeBecaService.cs) | `FondoDeBecaService` | `ObtenerTiposParentesco`, `ObtenerTiposEgreso`, `ObtenerTiposVivienda`, `ObtenerUniversidades` | public (interfaz) | solo interfaz + tests (**ni endpoint comentado**) | **Requiere confirmación** | No tienen endpoint asociado (ni activo ni comentado). Los más huérfanos, pero podrían consumirse desde otro front/módulo. | Medio. |
| [CatalogosService.cs:300](../../WebApiAdmisiones/AppLogic/Services/Catalogos/CatalogosService.cs#L300) | `CatalogosService` | `ObtenerFondosDeBecaPorProducto` | public (interfaz) | solo interfaz + test (endpoint comentado en `CatalogosController.cs:200-207`) | **Requiere confirmación** | Endpoint comentado. Tiene test. | Medio. |
| [RefreshTokenService.cs:58](../../WebApiAdmisiones/DataAccess/Services/RefreshTokenService.cs#L58) | `RefreshTokenService` | `ValidateRefreshTokenAsync` | public (interfaz) | 0 en flujo (AuthService usa `GetCodigoPersonaByRefreshTokenAsync`) | **Requiere confirmación** | Método de interfaz `IRefreshTokenService`; puede quedar para otro consumidor. Verificar si el flujo de refresh lo necesita. | Medio. |

## Componentes potencialmente inertes a nivel módulo

| Símbolo | Dónde | Situación | Acción sugerida |
|---------|-------|-----------|-----------------|
| `FondoDeBecaController` completo | [Controllers/FondoDeBecaController.cs](../../WebApiAdmisiones/WebApiAdmisiones/Controllers/FondoDeBecaController.cs) | Todos los endpoints comentados. No expone rutas HTTP. | **Requiere confirmación**: activar, eliminar o documentar por qué está pausado. |
| `IBandejaService` / `BandejaService` | Registrado en [DomainServicesExtensions.cs:129](../../WebApiAdmisiones/WebApiAdmisiones/Extensions/DomainServicesExtensions.cs#L129); definido en `Core/Modules/ModBandeja` | Registrado en DI pero **ningún controller del API lo consume** (solo registro + tests). | **Requiere confirmación**: pertenece al módulo `ModBandeja`; puede consumirse por otra app o estar pendiente de cableado. Ver [05](05-di.md). |
| `IGenericRepository` / `GenericRepository` | Registrado en [DomainServicesExtensions.cs:100](../../WebApiAdmisiones/WebApiAdmisiones/Extensions/DomainServicesExtensions.cs#L100) | Los servicios de dominio acceden por `IUnitOfWork`, no por `IGenericRepository`. | **Requiere confirmación**: verificar si algún módulo/repo lo usa antes de quitar el registro. |

## Notas importantes (falsos positivos evitados)

- **No** se marcaron los ~90 repositorios de `DataAccess/DevartDataAccess/DevartRepositories` ni los `*.Generated.cs`: son implementaciones de interfaz generadas/consumidas dinámicamente vía `IUnitOfWork`; su "0 referencias directas" no implica muerte.
- **No** se marcaron los métodos públicos de controllers ni de DTOs (serialización).
- Los métodos de mapeo privados estáticos de `InscripcionesService`/`PersonaService` sí tienen referencias internas y no se listan.
- El conteo de referencias es del repositorio actual; **los servicios detrás de endpoints comentados podrían tener consumidores fuera del repo** (por eso "requiere confirmación", no "eliminable").
