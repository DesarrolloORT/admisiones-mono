# Mapa de módulos

Índice de la documentación por módulo. **Cada proyecto tiene su propio `README.md` adentro**: éste
solo dice cuál leer y cómo se relacionan.

## Cómo leer esto

Si no entendés algo, el orden que menos tiempo pierde es:

1. **El README del módulo donde está el código** que estás mirando.
2. [GLOSARIO-DOMINIO.md](GLOSARIO-DOMINIO.md) — si el problema es una palabra (*qué es un "comienzo",
   por qué "seña" es `MinimumDeposit`, qué se traduce y qué no*).
3. [MATRIZ-TRAZABILIDAD.md](MATRIZ-TRAZABILIDAD.md) — si el problema es *por qué esto está así*
   (historia del refactor, breaking changes, decisiones y sus motivos).
4. [GUIA-ESTILO-CODIGO.md](GUIA-ESTILO-CODIGO.md) — cómo escribir código nuevo acá.

## Los niveles

Un módulo solo puede referenciar módulos de un nivel **estrictamente menor**. No hay ciclos, y no
puede volver a haberlos.

```
nivel 6   Catalogs
nivel 5   Registration        Enrollments
nivel 4   People    Authentication    Scholarships
nivel 3   Identity
nivel 2   Platform    Integrations.Tivenos    Integrations.EnrollmentsAndPayments
nivel 1   Contracts    DevartDtos
```

## Índice

| Módulo | Nivel | Qué resuelve | README |
|---|---|---|---|
| `AppLogic.Contracts` | 1 | lo poquísimo que comparten todos: `EnrollmentStatus`, `ComboOption`, `TextNormalization`, `[Redact]` | [→](../WebApiAdmisiones/AppLogic.Contracts/README.md) |
| `AppLogic.DevartDtos` | 1 | **código generado, no se edita** | [→](../WebApiAdmisiones/AppLogic.DevartDtos/README.md) |
| `AppLogic.Platform` | 2 | infraestructura sin dominio: mail, rate limiting, serialización | [→](../WebApiAdmisiones/AppLogic.Platform/README.md) |
| `AppLogic.Integrations.Tivenos` | 2 | encola avisos al CRM (por tabla, no HTTP) | [→](../WebApiAdmisiones/AppLogic.Integrations.Tivenos/README.md) |
| `AppLogic.Integrations.EnrollmentsAndPayments` | 2 | cliente HTTP de la API interna de Inscripciones y Pagos | [→](../WebApiAdmisiones/AppLogic.Integrations.EnrollmentsAndPayments/README.md) |
| `AppLogic.Identity` | 3 | documento de identidad y stores de Redis del registro | [→](../WebApiAdmisiones/AppLogic.Identity/README.md) |
| `AppLogic.People` | 4 | datos de la persona ya autenticada | [→](../WebApiAdmisiones/AppLogic.People/README.md) |
| `AppLogic.Authentication` | 4 | login, 2FA, tokens, contraseña inicial, recupero | [→](../WebApiAdmisiones/AppLogic.Authentication/README.md) |
| `AppLogic.Scholarships` | 4 | becas (⚠️ fondo de beca sin conectar) | [→](../WebApiAdmisiones/AppLogic.Scholarships/README.md) |
| `AppLogic.Registration` | 5 | onboarding: documento → persona con usuario | [→](../WebApiAdmisiones/AppLogic.Registration/README.md) |
| `AppLogic.Enrollments` | 5 | interés, preinscripción, pagos, encuesta inicial | [→](../WebApiAdmisiones/AppLogic.Enrollments/README.md) |
| `AppLogic.Catalogs` | 6 | combos de solo lectura para el front | [→](../WebApiAdmisiones/AppLogic.Catalogs/README.md) |
| `WebApiAdmisiones` | host | controllers, DI, seguridad HTTP | [→](../WebApiAdmisiones/WebApiAdmisiones/README.md) |

## Convenciones que valen para todos

- **`Contracts/`** — interfaces de caso de uso (una por caso de uso, con `Execute`/`ExecuteAsync`).
- **`UseCases/`** — una clase por caso de uso, una por endpoint.
- **`Services/`** — colaboradores con responsabilidad cohesiva que usan varios casos de uso.
- **`Rules/`** y **`Validators/`** — lógica pura de negocio, sin I/O.
- **`Mapping/`** — **toda** traducción entre formas. Los servicios y casos de uso no tienen métodos
  de mapeo privados.
- **`Dtos/`** — DTOs propios, en inglés, con `<summary>` en español por propiedad.
- **`Constants/`** — enums de negocio y sus catálogos de error.
- **`DependencyInjection/`** — la extensión `AddX()` del módulo.

## Reglas que no se rompen

1. **`Core/`, `BusinessLogic`, `DataAccess` y `AppLogic.DevartDtos` no se tocan.** Son submódulo
   compartido o código generado.
2. **Los DTOs de `Integrations.*` quedan en español.** Modelan formatos ajenos; traducirlos rompe la
   deserialización sin dar error.
3. **Nada de Devart ni de `Core` sale al front.** Cada módulo mapea a su DTO propio. Ese es el límite
   anticorrupción.
4. **Si dos módulos del mismo nivel se necesitan, es inversión de dependencia, no una referencia**
   (ver `IPendingRegistrationCompletion` entre Authentication y Registration).
5. **Los mensajes de error van en español**; los identificadores, en inglés.
6. **Los códigos de error (`GEN_IP_01`) son contrato opaco**: no se renombran.

## Áreas con deuda conocida

- **Fondo de beca** (`Scholarships`): `IScholarshipFundService` y sus validadores están registrados y
  testeados pero **ningún controller los consume**. Espera decisión de producto. Detalle en el README
  de Scholarships.
- **`RequiredPersonData.FindMissingRequiredData`** (`People`): sin consumidores, del mismo área.
