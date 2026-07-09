# Dead code sweep

## Rama
chore/dead-code-sweep

> Base: `develop` (5f509a49). Alcance: grupos A y B de [AUDITORIA-REFACTOR.md §5](AUDITORIA-REFACTOR.md). Cada eliminación fue verificada con búsqueda global (producción + UnitTesting + DI + serialización + reflection) antes de tocarla.

## Cambios realizados

| Archivo | Cambio | Motivo | Riesgo |
|---|---|---|---|
| `WebApiAdmisiones/Helpers/FormFileHelper.cs` | Eliminado (archivo y carpeta `Helpers/`) | 0 referencias en toda la solución | 🟢 |
| `Controllers/AuthController.cs` | Eliminado segundo bloque `if (!result.Success)` duplicado e inalcanzable en `RefreshToken` | Copia exacta del bloque anterior; código muerto | 🟢 |
| `Controllers/CatalogosController.cs` | Eliminado endpoint comentado `ObtenerFondosDeBecaPorProducto` (con su XML doc huérfano) | Código comentado sin uso | 🟢 |
| `Controllers/PersonaController.cs` | Eliminado `using AppLogic.Services.Inscripciones;` | Import sin uso | 🟢 |
| `AppLogic/Services/Registro/RegistroService.cs` | Eliminados: parámetro `ICatalogosService` del constructor (nunca asignado), `ConfirmarNuevaPersonaAsync` y su helper privado `CrearPersonaUsuarioAsync`, `using` de Catalogos | Dependencia nunca usada; método muerto en producción (el flujo real va por `RegistroFlowService.ConfirmarNuevaPersonaAsync`, que sigue intacto) | 🟢 |
| `AppLogic/IServices/Registro/IRegistroService.cs` | Eliminada la entrada `ConfirmarNuevaPersonaAsync` | Contrato interno sin consumidores en producción | 🟢 |
| `AppLogic/Services/Autenticacion/PasswordActivationService.cs` | Eliminado `ObtenerSecretKeyPublic()` | 0 referencias (era `internal static`, ni los tests lo usaban) | 🟢 |
| `AppLogic/Dtos/Personas/DtoPersonaRequests.cs` | Eliminados `DtoEmpresaEncuestaRequest`, `DtoPublicidadEncuestaRequest`, `DtoMotivoEncuestaRequest` | 0 referencias, tests incluidos | 🟢 |
| `AppLogic/ApiClients/InscripcionesyPagosApiClient.cs` | Eliminados `CrearFacturaResponse` (el método de factura devuelve `string`) y `ObtenerOfertasParaInscripcionAdmisionesAsync` (variante sin proceso) | 0 usos en producción; la variante sin proceso solo la invocaba su test y además ignoraba el parámetro `idTurno`. Producción usa `ConProceso` | 🟢 |
| `AppLogic/Services/Catalogos/GeneralService.cs` + `IGeneralService.cs` | Eliminada sobrecarga `CalcularFechaVencimientoAdmisiones(long, long)` sin `uow` | Muerta en producción (solo tests); la variante con `uow` queda como única | 🟢 |
| `AppLogic/Services/Catalogos/CatalogosService.cs` + `ICatalogosService.cs` | `ObtenerPaisesEstadosCiudades()` síncrona: fuera de la interfaz, ahora `private` en el service | Producción solo consume `ObtenerPaisesEstadosCiudadesAsync()`. El `Method` del `OperationResult` no cambia (mismo nombre interno) | 🟢 |
| `AppLogic/Constants/CommonConstants.cs` | Eliminada clase `Sexo` completa | 0 usos | 🟢 |
| `AppLogic/Constants/PersonaConstants.cs` | Eliminados `TipoPersonaSgi`, `Parametros.FechaMinimaNacimiento`, `Parametros.ExteriorInstitucionOrt`, `Parametros.TituloGenericoSextoExterior` | 0 usos (el `TipoPersonaSgi` usado es el de `InscripcionesConstants`) | 🟢 |
| `AppLogic/Constants/InscripcionesConstants.cs` | Eliminadas 9 constantes de `InteresProducto`: `UsuarioAdmisiones`, `TipoInteresComun`, `TipoActividadMonoAccion`, `EstadoAccionRealizada`, `ResultadoAccionRealizada`, `GradoPurezaPuro`, `GradoInteresDesinteresado`, `GradoInteresRegistro`, `MotivoDesinteresEleccionAdmisiones` | 0 usos verificados por grep global; quedan las 9 con uso real | 🟢 |

## Cambios descartados

| Candidato | Motivo por el que no se tocó |
|---|---|
| `FondoDeBecaConstants.Declaracion` (5 constantes muertas) | Todo el clúster Fondo de Beca está congelado hasta la decisión de producto (fuera de alcance explícito de esta rama) |
| `FondoDeBecaService` / `IFondoDeBecaServices` / DTOs / validators / uploads / registro DI | Fuera de alcance explícito — decisión de producto pendiente (grupo D de la auditoría) |
| `IBandejaService` / `BandejaService` (registro DI sin consumidores) | Fuera de alcance; solo se reporta: sigue registrado en `DomainServicesExtensions.cs:131` sin ningún consumidor real |
| `ObtenerSeniaMinimaAsync`, `ObtenerCtaCteAsync`, `ObtenerCursosPagosAsync` (+DTOs) del ApiClient | Grupo C de la auditoría: hay commits recientes de SeniaMinima; confirmar con el equipo si se van a conectar antes de borrar (rama `chore/apiclient-dead-methods`) |
| `ICatalogosService.ObtenerFondosDeBecaPorProducto` + implementación + test | Quedó muerto en producción al borrar el endpoint comentado que era su único consumidor potencial, pero pertenece al clúster Fondo de Beca — sigue la suerte del grupo D |
| `DtoBecaPersona` | Vivo pero solo lo usa el mock de `PersonaController.ObtenerMisBecas` — atado a la decisión de Becas |
| Capa DevartDTO / converters / template | Código generado; decisión a nivel template (§4.4 de la auditoría), riesgo alto |

## Tests

- Resultado de compilación: **OK, 0 errores** (solo warnings preexistentes NU1903/NU1510 de paquetes).
- Resultado de tests: **825/826 pasan**. El único que falla, `InscripcionesServiceTests.ObtenerDetalleInscripcion_WhenPagoPendiente_ReturnsDetallePago`, **ya fallaba en `develop` antes de esta rama** (verificado con stash): `NullReferenceException` en `InscripcionesService.cs:139` porque el test no mockea `uow.InscriptoSeniaMinima`. Es de los commits recientes de SeniaMinima, fuera del alcance de esta limpieza.
- Tests eliminados/modificados:
  - Eliminado `InscripcionesyPagosApiClientTests.ObtenerOfertasParaInscripcionAdmisionesAsync_WithSuccess_MapsResponseAndBuildsUrl` (testeaba el método eliminado).
  - Eliminado `RegistroServiceTests.ConfirmarNuevaPersona_InvalidDocument_ReturnsFailure` (testeaba el método eliminado) + mock de `ICatalogosService` del setup.
  - Eliminado bloque comentado de tests de `ShouldRefreshToken` en `AuthenticationExtensionsTests` (el propio archivo decía que la API fue eliminada).
  - Eliminado bloque comentado de tests viejos en `RegistroControllerTests` (usaban un constructor de controller que ya no existe).
  - `GeneralServiceTests`: los 2 tests ahora llaman la sobrecarga con `uow` (misma lógica testeada).
  - `CatalogosServiceTests.ObtenerPaisesEstadosCiudades_ReturnsSlimPaisesEstadosCiudades`: ahora llama la versión async (que delega en la misma lógica).
  - `InscripcionesServiceTests` / `CatalogosControllerTests`: ajustes de mocks a las firmas que quedaron (sin cambio de lo que se asserta).

## Riesgos pendientes

- Test preexistente roto en `develop` (`ObtenerDetalleInscripcion_WhenPagoPendiente_ReturnsDetallePago`) — arreglarlo en la rama de SeniaMinima, no acá.
- `IBandejaService` y `IFondoDeBecaServices` siguen registrados en DI sin consumidores (decisión de producto pendiente, fase 0 de la auditoría).
- `ObtenerFondosDeBecaPorProducto` quedó sin ningún consumidor potencial tras borrar el endpoint comentado.
- Los métodos del grupo C del ApiClient (`SeniaMinima`, `CtaCte`, `CursosPagos`) siguen ahí con 0 llamadas.

## Próxima rama recomendada

`chore/apiclient-dead-methods` (grupo C, condicionada a la respuesta del equipo sobre SeniaMinima) o, si la decisión de producto ya está, `chore/remove-fondo-beca` / `feat/conectar-becas`. Si se prefiere seguir sin depender de decisiones: `refactor/shared-crypto-config` (fase 3, con los tests previos de §9 de la auditoría).
