# Estructura Tivenos - Piloto

## Rama
refactor/estructura-tivenos-piloto

## Objetivo

Validar, con el módulo más chico y sin dependencias de otros módulos grandes, el patrón `module-first` propuesto en [AUDITORIA-REFACTOR.md §8.2](AUDITORIA-REFACTOR.md#82-propuesta-de-estructura) antes de aplicarlo a `Catalogos`, `Personas`, `Registro`, `Autenticacion` e `Inscripciones`. Rama de solo movimiento: `git mv` + namespace + `using`, sin tocar una sola línea de lógica.

## Archivos movidos

| Archivo anterior | Archivo nuevo | Motivo |
|---|---|---|
| `AppLogic/IServices/Tivenos/ITivenosEnvioService.cs` | `AppLogic/Tivenos/Interfaces/ITivenosEnvioService.cs` | Interfaz del módulo → `Interfaces/` |
| `AppLogic/Services/Tivenos/TivenosEnvioService.cs` | `AppLogic/Tivenos/Services/TivenosEnvioService.cs` | Implementación del módulo → `Services/` |
| `AppLogic/Dtos/Tivenos/DtoTivenosAltaInteresRequest.cs` | `AppLogic/Tivenos/Dtos/DtoTivenosAltaInteresRequest.cs` | DTO del módulo → `Dtos/` |
| `AppLogic/Dtos/Tivenos/DtoTivenosBachilleratoRequest.cs` | `AppLogic/Tivenos/Dtos/DtoTivenosBachilleratoRequest.cs` | DTO del módulo → `Dtos/` |
| `AppLogic/IServices/Tivenos/TivenosAltaInteresOperacion.cs` | `AppLogic/Tivenos/Dtos/TivenosAltaInteresOperacion.cs` | Ver justificación abajo — no es una interfaz ni un service |

Se eliminaron las carpetas vacías resultantes: `AppLogic/IServices/Tivenos/`, `AppLogic/Services/Tivenos/`, `AppLogic/Dtos/Tivenos/`.

### Justificación de `TivenosAltaInteresOperacion` → `Dtos/`

La auditoría (§7) señala que esta clase está "mal ubicada" en `IServices/Tivenos/` porque no es una interfaz: es una clase sellada de datos (3 propiedades `string`/`string?`) con 3 factory methods estáticos que arman las combinaciones válidas de `TipoProcesoLlamador`/`Disparador`/`OrigenLlamador`. Su único uso es ser el valor de `DtoTivenosAltaInteresRequest.Operacion` — es decir, es un objeto de valor anidado dentro de un DTO, exactamente el mismo patrón que la propia auditoría documenta para `DtoResumenInscripcion`, `DtoSeniaMinima`, etc. dentro de sus DTOs padre (§8.3).

La estructura objetivo pedida (`Interfaces/`, `Services/`, `Dtos/`) no tiene una cuarta carpeta para "objetos de valor/dominio sueltos", y crear una (`Models/`, `Domain/`) solo para esta única clase violaría la regla de no inventar carpetas por plantilla. Por eso se movió a `Dtos/` junto a su DTO contenedor. Si en el futuro aparecen más clases de este tipo en otros módulos, ahí sí se justificaría evaluar una carpeta `Domain/` o `Rules/` (ver AUDITORIA-REFACTOR §8.2, que ya prevé `Rules/` para Inscripciones).

## Namespaces actualizados

| Namespace anterior | Namespace nuevo |
|---|---|
| `AppLogic.IServices.Tivenos` (interfaz) | `AppLogic.Tivenos.Interfaces` |
| `AppLogic.IServices.Tivenos` (`TivenosAltaInteresOperacion`) | `AppLogic.Tivenos.Dtos` |
| `AppLogic.Services.Tivenos` | `AppLogic.Tivenos.Services` |
| `AppLogic.Dtos.Tivenos` | `AppLogic.Tivenos.Dtos` |

`using` actualizados (sin otro cambio) en los consumidores fuera del módulo, verificados con búsqueda global antes y después del movimiento:

- `AppLogic/Services/Inscripciones/InscripcionesService.cs` (consume `ITivenosEnvioService` para encolar alta de interés)
- `AppLogic/Services/Inscripciones/Encuesta/EncuestaInicialService.cs` (consume `ITivenosEnvioService` para bachillerato)
- `AppLogic/Helpers/InteresProductoRegistroHelper.cs` (consume `DtoTivenosAltaInteresRequest` y `TivenosAltaInteresOperacion`)
- `WebApiAdmisiones/Extensions/DomainServicesExtensions.cs` (registro DI: `AddScoped<ITivenosEnvioService, TivenosEnvioService>`)
- `UnitTesting/AppLogic/Services/InscripcionesServiceTests.cs`
- `UnitTesting/AppLogic/Services/EncuestaInicialServiceTests.cs`
- `UnitTesting/AppLogic/Services/TivenosEnvioServiceTests.cs`
- `UnitTesting/Extensions/DomainServicesExtensionsTests.cs`

Ninguno de estos archivos pertenece a los módulos `Inscripciones`, `Autenticacion` o `Registro` en el sentido de "reorganizarlos" — solo se les corrigió el `import` roto por el movimiento de Tivenos, tal como permite la consigna ("ajustar referencias rotas por el movimiento"). No se tocó ni una línea de lógica en `InscripcionesService.cs` ni en `EncuestaInicialService.cs`.

## Cambios fuera de alcance detectados

| Hallazgo | Motivo por el que no se tocó |
|---|---|
| `TivenosAltaInteresOperacion` podría, a futuro, ameritar una carpeta `Domain/` propia si el patrón se repite en otros módulos | Con una sola clase de este tipo en todo `Tivenos`, crear la carpeta ahora sería una carpeta-plantilla vacía de intención; se documenta como decisión, no como pendiente |
| `InteresProductoRegistroHelper.cs` (construye `DtoTivenosAltaInteresRequest`) vive en `AppLogic/Helpers/`, junto a helpers de otros módulos | Pertenece al módulo `Inscripciones`/`Registro` (cajón de Helpers ya señalado en auditoría §8.1), explícitamente fuera de alcance de esta rama |
| `InscripcionesService.cs` y `EncuestaInicialService.cs` referencian Tivenos pero son servicios de `Inscripciones` | Fuera de alcance explícito ("No tocar Inscripciones"); solo se actualizó el `using` |
| DI de `ITivenosEnvioService`/`TivenosEnvioService` en `DomainServicesExtensions.cs` | Solo se actualizó el `using`; el registro (`AddScoped<...>`) no cambió, no era necesario tocar DI |

## Riesgos

- **Swagger/schemaIds**: verificado — el proyecto **no** usa `CustomSchemaIds`/`FullName` en la config de `AddSwaggerGen` (`Program.cs`), y además ninguno de los DTOs de Tivenos (`DtoTivenosAltaInteresRequest`, `DtoTivenosBachilleratoRequest`) se expone en ningún controller ni endpoint — son payloads internos hacia la cola de envíos a Tivenos, no contrato HTTP. **Riesgo real: ninguno** para este módulo puntual. Esta verificación específica de Swagger habrá que repetirla igual en cada módulo siguiente, porque los próximos (`Catalogos`, `Personas`) sí tienen DTOs públicos.
- **Serialización JSON**: no aplica — `System.Text.Json` no usa namespace, y estos DTOs no se serializan a un contrato externo (solo se construyen en memoria para `EnvioParaTiveno`, una entidad Devart).
- Sin riesgos pendientes de compilación, DI o tests.

## Tests

- Resultado de compilación: **OK, 0 errores** (298 warnings preexistentes, ninguno nuevo introducido por este cambio).
- Resultado de tests: **826/826 pasan.**

## Conclusión

El patrón `module-first` con `Interfaces/ Services/ Dtos/` funciona limpio para un módulo chico y autocontenido como Tivenos: 5 archivos movidos, 4 archivos externos con `using` actualizado, cero cambios de lógica, cero cambios de contrato. El único punto de juicio fue `TivenosAltaInteresOperacion` (objeto de valor anidado en un DTO, sin interfaz ni lógica de service) — se resolvió ubicándolo en `Dtos/` en vez de inventar una carpeta nueva, siguiendo el mismo criterio que la auditoría aplica a los DTOs anidados de Inscripciones.

Para módulos más grandes (`Inscripciones`, `Autenticacion`) va a aparecer más de este tipo de clase (helpers estáticos, DTOs internos de Redis, mappers) y ahí sí conviene decidir si se justifica una carpeta `Rules/`/`Helpers/`/`Mappers/` — pero solo si el módulo la llena, como ya anticipa AUDITORIA-REFACTOR §8.2 y §8.3.

## Próxima rama recomendada

Seguir con el segundo módulo más chico para seguir afinando el patrón antes de los módulos grandes: `refactor/estructura-becas` o `refactor/estructura-catalogos` (orden sugerido en AUDITORIA-REFACTOR §10, fase 5). Recién después: `estructura-personas` → `estructura-registro` → `estructura-autenticacion` → `estructura-inscripciones` (el más grande, con el submódulo Encuesta).
