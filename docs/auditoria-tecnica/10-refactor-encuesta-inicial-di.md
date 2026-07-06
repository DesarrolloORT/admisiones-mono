# 10 — Refactor: EncuestaInicialService por DI

Rama: `refactor/encuesta-inicial-di`. Resuelve el anti-patrón confirmado en [08](08-validacion-hallazgos.md) §3. Alcance acotado: solo lo necesario para inyectar `EncuestaInicialService` por interfaz.

## Problema original

`InscripcionesService` instanciaba `EncuestaInicialService` con `new` en sus dos métodos de encuesta ([antes] `InscripcionesService.cs` ~L388 y ~L399):

```csharp
public OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona)
{
    var encuestaInicialService = new EncuestaInicialService(
        _uowFactory, _dbConnectionContext, _generalService, _tivenosEnvioService);
    return encuestaInicialService.ObtenerEncuestaInicial(codigoPersona);
}
```

`EncuestaInicialService` era `internal sealed` **sin interfaz**, por lo que no podía inyectarse ni mockearse; se creaba a mano en cada request y su lógica solo se cubría indirectamente desde `InscripcionesServiceTests`.

## Solución aplicada

1. Se creó la interfaz `IEncuestaInicialService` con solo los dos métodos que consume `InscripcionesService`.
2. `EncuestaInicialService` pasó de `internal sealed` a `public sealed` e implementa la interfaz. **La lógica interna no se tocó** (constructor, métodos, transacción, bachillerato legacy y valor `1304` intactos).
3. Se registró en DI como `Scoped` (mismo lifetime que el resto de AppLogic).
4. `InscripcionesService` ahora recibe `IEncuestaInicialService` por constructor y delega; se eliminaron ambos `new`.
5. Al quedar `IGeneralService` sin uso en `InscripcionesService` (solo servía para construir el `new`), se quitó esa dependencia (consecuencia directa del refactor). `_dbConnectionContext` y `_tivenosEnvioService` **se conservan** porque se usan en otros métodos (`RegistrarInteresProducto`).

No cambió comportamiento funcional, ni endpoints, ni request/response, ni el contrato JSON del front.

## Nueva interfaz creada

`WebApiAdmisiones/AppLogic/IServices/Inscripciones/IEncuestaInicialService.cs`

```csharp
public interface IEncuestaInicialService
{
    OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona);
    OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(
        long codigoPersona, DtoGuardarEncuestaInicialRequest request);
}
```

Expone únicamente los métodos públicos usados; no filtra helpers ni privados.

## Registro DI agregado

En `DomainServicesExtensions.cs`, junto al resto de servicios de inscripciones:

```csharp
services.AddScoped<IInscripcionesService, InscripcionesService>();
services.AddScoped<IEncuestaInicialService, EncuestaInicialService>();   // nuevo
```

(+ `using AppLogic.Services.Inscripciones.Encuesta;`)

## Cambios en InscripcionesService

- Constructor: se quitó `IGeneralService generalService`; se agregó `IEncuestaInicialService encuestaInicialService`. (Sigue siendo de 5 parámetros.)
- Los dos métodos delegan directamente:
  ```csharp
  public OperationResult<DtoObtenerEncuestaInicialResponse> ObtenerEncuestaInicial(long codigoPersona)
      => _encuestaInicialService.ObtenerEncuestaInicial(codigoPersona);

  public OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(long codigoPersona, DtoGuardarEncuestaInicialRequest request)
      => _encuestaInicialService.GuardarEncuestaInicial(codigoPersona, request);
  ```
- Se quitaron dos `using` que quedaron huérfanos: `AppLogic.IServices.Catalogos` (solo lo usaba `IGeneralService`) y `AppLogic.Services.Inscripciones.Encuesta` (solo lo usaba el `new`).

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `AppLogic/IServices/Inscripciones/IEncuestaInicialService.cs` | **Nuevo.** Interfaz. |
| `AppLogic/Services/Inscripciones/Encuesta/EncuestaInicialService.cs` | `internal sealed` → `public sealed` + implementa `IEncuestaInicialService` + `using` de la interfaz. Lógica intacta. |
| `WebApiAdmisiones/Extensions/DomainServicesExtensions.cs` | Registro `AddScoped<IEncuestaInicialService, EncuestaInicialService>()` + using. |
| `AppLogic/Services/Inscripciones/InscripcionesService.cs` | Inyecta la interfaz, elimina los dos `new`, quita `IGeneralService` sin uso y 2 usings huérfanos. |
| `UnitTesting/AppLogic/Services/InscripcionesServiceTests.cs` | Ajuste de construcción (ver abajo) + 2 tests de delegación. |
| `UnitTesting/AppLogic/Services/EncuestaInicialServiceTests.cs` | **Nuevo.** Tests dedicados. |

## Tests agregados o ajustados

### Ajuste que preserva cobertura (InscripcionesServiceTests)
Los ~40 tests que ejercitaban la lógica **profunda** de encuesta (temporal/definitiva, bachillerato legacy `1304`, Tivenos alta/modificación/no-duplica, completitud) seguían siendo valiosos. Para no perder esa cobertura, los dos sitios de construcción del servicio ahora inyectan una **instancia real** de `EncuestaInicialService` construida con los mismos mocks (helper `CrearEncuestaInicialServiceReal()`), en lugar del antiguo `new` interno. Resultado: esos tests siguen ejercitando la implementación real, ahora a través de la dependencia inyectada. Se actualizó también la firma de construcción (sin `IGeneralService`, con `IEncuestaInicialService`).

### Tests de delegación (InscripcionesServiceTests, 2 nuevos)
- `ObtenerEncuestaInicial_DelegaEnEncuestaInicialService`
- `GuardarEncuestaInicial_DelegaEnEncuestaInicialService`

Con un mock de `IEncuestaInicialService`, verifican que `InscripcionesService` delega y devuelve el mismo `OperationResult` (prueba la nueva costura DI, imposible antes del refactor).

### Tests dedicados (EncuestaInicialServiceTests, nuevo archivo, 4 tests)
Ejercitan `EncuestaInicialService` como unidad a través de `IEncuestaInicialService`, con harness compacto propio:
- `ObtenerEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound` (404 `GEN_OEI_01`).
- `GuardarEncuestaInicial_PersonaNoEncontrada_DevuelveNotFound` (404 `INS_EI_01`).
- `ObtenerEncuestaInicial_ConDerechoSinEncuesta_DevuelveTieneDerechoSinEncuesta`.
- `GuardarEncuestaInicial_ParcialMinimo_CreaEncuestaTemporal` (crea encuesta `TEMPORAL`, `BeginTransaction`/`Commit` una vez, no encola bachillerato al no ser definitiva).

> Decisión de alcance: los escenarios definitiva/bachillerato-`1304`/Tivenos **no se duplicaron** en el archivo dedicado porque ya están cubiertos —y siguen ejecutándose— en `InscripcionesServiceTests` contra la implementación real inyectada. Duplicar ese harness (~900 líneas) no aportaba cobertura nueva.

### Contrato del front (tarea 8)
`EncuestaInicialContractTests` no se tocó y sigue pasando: no cambió el JSON esperado.

## Resultado de dotnet test

```
Correctas! - Con error: 0, Superado: 817, Omitido: 0, Total: 817 — UnitTesting.dll (net10.0)
```

`dotnet build` correcto. 811 tests base + 6 nuevos (2 delegación + 4 dedicados) = 817. Sin regresiones. Warnings del build preexistentes y ajenos a este cambio.

## Criterios de aceptación

- [x] No queda ningún `new EncuestaInicialService(...)` en `InscripcionesService` (verificado con grep en producción).
- [x] `EncuestaInicialService` se resuelve por DI (`Scoped`).
- [x] `IEncuestaInicialService` registrada en `DomainServicesExtensions`.
- [x] `InscripcionesService` depende de la interfaz, no de la implementación.
- [x] Tests existentes siguen pasando (cobertura profunda preservada).
- [x] Hay tests dedicados de `EncuestaInicialService`.
- [x] No cambió el contrato del endpoint de encuesta inicial.
- [x] Lógica legacy (`1304`, transacción, fórmulas, mapeos Devart) sin alterar.
- [x] `dotnet test` exitoso.

## Riesgos restantes

- `EncuestaInicialService` ahora es `public`: amplía su superficie visible dentro de `AppLogic`, pero solo expone lo que ya usaba `InscripcionesService`. Riesgo bajo.
- El cambio de firma del constructor de `InscripcionesService` (se quitó `IGeneralService`) afecta a cualquier construcción manual fuera de los tests ajustados. Se revisó el repo: solo se construye vía DI y en `InscripcionesServiceTests` (ya actualizado).

## Detectado fuera de alcance (no modificado)

- `InscripcionesyPagosApiClient` sigue inyectándose como **clase concreta** (sin interfaz) en `InscripcionesService` y `CatalogosService`. Es el mismo tipo de acoplamiento que este refactor corrigió para encuesta; queda como candidato a una rama futura análoga. Documentado en [04](04-dependencias-arquitectura.md)/[08](08-validacion-hallazgos.md); **no** se toca en esta rama.
- `EncuestaInicialService` mantiene métodos privados largos (`GuardarEncuestaInicial` ~119 líneas) y lógica de bachillerato legacy; su refactor interno está fuera del alcance de esta rama (solo se cambió la accesibilidad y se agregó la interfaz).
