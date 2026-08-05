# AppLogic.People

**Nivel 4 — depende de `AppLogic.Contracts` y `AppLogic.Identity`.**

## Qué resuelve

Los datos de la **persona ya autenticada**: consulta y edición de sus datos personales, foto,
documento de identidad, cambio de contraseña y el listado de sus inscripciones.

Todo lo que hay acá asume que ya hay sesión. El alta de una persona nueva es de
`AppLogic.Registration`; el login es de `AppLogic.Authentication`.

## Qué expone

9 casos de uso, uno por endpoint de `person/*`. Cada uno es una clase con un solo método `Execute`:

| Caso de uso | Endpoint | Dependencias |
|---|---|---|
| `GetPersonDetails` | `GET person/details` | uow |
| `UpdatePersonDetails` | `PUT person/details` | uow |
| `ValidatePhoneNumber` | `POST person/validate-phone-number` | **ninguna** (función pura) |
| `GetMyEnrollments` | `GET person/enrollments` | uow |
| `ChangePassword` | `POST person/change-password` | ldap, logger |
| `GetPersonPhoto` | `GET person/photo` | uow |
| `UploadPersonPhoto` | `POST person/photo` | uow, dbCtx |
| `GetPersonIdentityDocument` | `GET person/identity-document` | uow |
| `UploadPersonIdentityDocument` | `POST person/identity-document` | uow, dbCtx |

Los contratos están en `Contracts/IPeopleUseCases.cs` (una interfaz por caso de uso). Se registran
con `services.AddPeople()`.

## La regla que hay que entender: identidad restringida

`Rules/PersonIdentityRules`:

- `HasRestrictedIdentity(person, tieneInscripcionActiva)` — si la persona ya tiene una inscripción
  activa, **sus datos de identidad quedan congelados**: documento, nombres, fecha de nacimiento y
  sexo no se pueden cambiar desde Autoservicio.
- `AreIdentityChangesAllowed(...)` — devuelve `bool`, no `OperationResult`: hay un solo estado de
  fallo posible.
- `ApplyIdentityChanges(...)` — aplica los cambios **solo si están permitidos**.

`PersonDetailsResponse.HasRestrictedIdentity` le avisa al front para que muestre esos campos como
solo lectura. Pero el backend **no confía en el front**: aunque manden datos de identidad, si está
restringida se ignoran silenciosamente.

## Estructura

```
Contracts/IPeopleUseCases.cs   las 9 interfaces
UseCases/                      una clase por caso de uso
Rules/PersonIdentityRules      identidad restringida
Rules/PersonAuditStamp         sella usuario/fecha/hora de última modificación
Mapping/PersonMapper           Persona → PersonDetailsResponse
Mapping/MyEnrollmentsMapper    el GroupBy de inscripciones por producto y proceso
Validators/RequiredPersonData  detecta datos faltantes → PersonDataGap
Constants/PersonDataGap        enum + catálogo de errores (10 motivos)
```

## Catálogo de errores en vez de strings sueltos

`PersonDataGap` es un enum de negocio (`BirthCountryMissing`, `NationalityMissing`, …) y su
extensión `ToFailure<T>()` es el **único lugar** donde ese motivo se convierte en código HTTP y
mensaje. Si agregás un motivo nuevo, el `switch` exhaustivo te obliga a mapearlo o tira
`ArgumentOutOfRangeException`.

## ⚠️ Código muerto conocido

`RequiredPersonData.FindMissingRequiredData` **no tiene ningún consumidor en producción**. Es parte
del área de Fondo de Beca, que espera una decisión de producto. No lo borres sin consultar; tampoco
asumas que se ejecuta.

## Trampas

- `ValidatePhoneNumber` no tiene dependencias: es una función pura sobre el DTO `PhoneNumber` que
  arma el front. No consulta la base.
- `ChangePassword` **no toca la base de admisiones**: la contraseña vive en LDAP. Lo único que hace
  contra la base es el sello de auditoría, y eso pasa por otro camino.
- `PersonAuditStamp.Apply` hay que llamarlo en **toda** escritura sobre `Persona`. Si te olvidás, la
  fila queda con el usuario/fecha del cambio anterior.

## Códigos de error

`PER_DAT_*`, `PER_ADP_*`, `DP_ACDP_*` (datos de persona faltantes), `CAM_PAS_*` (cambio de
contraseña), `GEN_FA_*` (foto), `GEN_SFA_*` (subir foto), `GEN_DA_*` / `GEN_SDA_*` (documento).
