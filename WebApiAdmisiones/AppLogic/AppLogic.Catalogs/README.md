# AppLogic.Catalogs

**Nivel 6 — el más alto. Depende de `Contracts`, `DevartDtos`, `Enrollments` y
`Integrations.EnrollmentsAndPayments`.**

## Qué resuelve

Los combos que el front necesita para armar los formularios: países/estados/ciudades, catálogos de la
encuesta inicial, carreras, comienzos, turnos, bancos e instituciones.

Es **solo lectura**. Ningún endpoint de este módulo escribe nada.

## Por qué está en el nivel más alto

Porque referencia a `Enrollments`: los combos **fijos** de la encuesta inicial (Sí/No, valoraciones,
apoyos en la decisión…) viven en `Enrollments.Survey.Rules.InitialSurveyOptions`, y este módulo los
combina con los **dinámicos** que salen de la base (universidades, motivos de elección,
publicidades, años de bachillerato).

La dirección es Catalogs → Enrollments, nunca al revés.

## Qué expone

`ICatalogService`, 7 operaciones, una por endpoint de `catalogs/*`:

| Método | Endpoint | Fuente |
|---|---|---|
| `GetCountriesStatesCitiesAsync` | `countries-states-cities` | base |
| `GetInitialSurveyCatalogsAsync` | `initial-survey` | base + `InitialSurveyOptions` |
| `GetDegreePrograms` | `degree-programs` | vistas `VdProductosDisponibles1y2` / `VdOfertasDisponibles3y4` |
| `GetIntakes` | `intakes` | vista `VdProcesosDisponibles1y2` |
| `GetShifts` | `shifts` | vista **o** API de Inscripciones y Pagos, según el nivel |
| `GetBanksAsync` | `banks` | base |
| `GetInstitutions` | `institutions` | base |

## Las dos fuentes de ofertas

`GetShifts` decide por el nivel del producto:

- **Niveles 3 y 4** (actualización profesional) → vista Devart `VdOfertasDisponibles3y4`.
- **Niveles 1 y 2** (grado y tecnicatura) → API de Inscripciones y Pagos.

`Mapping/OfferingMapper` tiene una sobrecarga por fuente y las unifica en `OfferingResponse`, así el
front ve una sola forma. Cualquier otro nivel devuelve `CAT_TURNOS_04`.

Lo mismo pasa en `GetDegreePrograms`: `DegreeProgramMapper.ToRow` tiene una sobrecarga por vista y
las aplana en `DegreeProgramRow`, que es sobre lo que corre el agrupado.

## `AcademicOffer` manda el agrupado

El enum (`UniversityDegree = 1`, `TechnicalDegree = 2`, `ProfessionalUpdate = 3`) decide dos cosas:
de qué vista se lee y **cómo se agrupa la respuesta**.

`ProfessionalUpdate` agrega un nivel extra de agrupación por "con seminarios" / "sin seminarios"
(`groupBySeminar: true`), y **puede devolver dos entradas** en el array externo (niveles 3 y 4 reales
de la base). El front tiene que iterar todo el array, nunca asumir una sola entrada.

Está documentado en `Docs/contracts/carreras.contract.json` y verificado por `CarrerasContractTests`.

## ⚠️ El cache no está acá

`CatalogCacheDecorator` vive en el **host** (`WebApiAdmisiones/Security/Cache/`), no en este módulo.
Es un decorador de `ICatalogService` registrado en `DomainServicesExtensions`:

```csharp
services.AddScoped<ICatalogService>(sp => new CatalogCacheDecorator(...));
```

Cachea en Redis solo tres catálogos, los que casi no cambian:

| Key | Método |
|---|---|
| `catalogos:paises-estados-ciudades` | `GetCountriesStatesCitiesAsync` |
| `catalogos:encuesta-inicial` | `GetInitialSurveyCatalogsAsync` |
| `catalogos:bancos` | `GetBanksAsync` |

TTL configurable con `Cache:CatalogosTTLHours` (default 24). El resto pasa derecho.

Si cambiás la firma de alguno de esos tres métodos, **hay que tocar el decorador también**: implementa
la interfaz completa.

## Trampas

- **Los métodos `Async` que no son async de verdad.** `GetCountriesStatesCitiesAsync`,
  `GetInitialSurveyCatalogsAsync` y `GetBanksAsync` devuelven `Task.FromResult(...)`: la firma es
  async por compatibilidad con el decorador de cache, que sí necesita `await`.
- **`catalogs/institutions` se angostó a propósito.** Antes devolvía la fila completa de `T_EMPRESA`
  (~38 columnas internas: facturación, RUC, voucher, PSIG). Ahora devuelve `{id, name}`, que es lo
  que el front usa (los manda de vuelta en `SaveInitialSurveyRequest.SecondaryInstitutionId` /
  `SecondaryInstitutionName`).
- **El servicio no tiene mappers privados.** Todo el mapeo está en `Mapping/`. Si te aparece un
  `Map*` o un `To*` privado dentro de `CatalogService`, va al mapper del módulo.

## Estructura

```
Interfaces/ICatalogService
Services/CatalogService              solo orquesta: lee y delega el armado
Dtos/                                todos propios, en inglés
Mapping/LocationMapper               países/estados/ciudades
Mapping/InitialSurveyCatalogMapper   combina combos fijos + dinámicos
Mapping/DegreeProgramMapper          DegreeProgramRow + el agrupado por nivel/escuela/seminario
Mapping/IntakeMapper, OfferingMapper, BankMapper, InstitutionMapper
```

## Códigos de error

`CAT_CARRERAS_01` (propuesta académica inválida), `CAT_TURNOS_02` (producto no encontrado),
`CAT_TURNOS_04` (nivel de producto no soportado).
