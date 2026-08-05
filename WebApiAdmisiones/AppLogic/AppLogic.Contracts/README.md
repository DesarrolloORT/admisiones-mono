# AppLogic.Contracts

**Nivel 1 — no depende de ningún otro módulo de AppLogic.** Es la base sobre la que se apoyan todos
los demás.

## Qué resuelve

Guarda las poquísimas cosas que *de verdad* comparten todos los módulos. Es deliberadamente chico
(6 archivos, ~180 líneas): cada tipo que entra acá hay que justificarlo, porque todo el resto del
sistema lo va a ver.

La regla para decidir si algo va acá: **¿lo usan dos módulos que no se conocen entre sí?** Si lo usa
uno solo, va en ese módulo. Si lo usan dos que ya se referencian, va en el de más abajo.

## Qué expone

| Tipo | Para qué |
|---|---|
| `EnrollmentStatus` | Constantes de estado de inscripción del esquema Oracle (`Confirmed`, …). Las usan Enrollments, Scholarships y People, que no se conocen entre sí. |
| `SchemaConstants` | Valores fijos del esquema de admisiones. |
| `ComboOption` | Par valor/etiqueta para combos del front. Lo **publica** Catalogs y lo **consume** la validación de la encuesta inicial en Enrollments. |
| `OperationResultExtensions` | `.Failure()` y `.As<T>()`, para propagar un error de un `OperationResult<A>` a un `OperationResult<B>` sin repetir código, y `ErrorCarrier`. |
| `RedactAttribute` | Marca propiedades que **no** deben aparecer en logs (contraseñas, tokens). Lo lee el middleware de logging del host. |
| `TextNormalization` | Normalización de texto sin dominio: `Trim`, `ToUpperWithoutAccents`, `ToTitleCase`, `IsYes`. |

## De qué depende

Solo de `Core/Utilities` (por `OperationResult<T>`). Nada más, a propósito.

## Trampas

- **`OperationResult<T>` no vive acá**, vive en `Core/Utilities` porque lo comparten otros proyectos
  ORT. Acá están solo las extensiones.
- **`TextNormalization` no sabe de documentos de identidad.** Las reglas de cédula/pasaporte están en
  `AppLogic.Identity.IdentityDocumentRules`. Si vas a agregar algo que "normaliza un documento",
  fijate primero cuál de los dos es.
- Agregar un tipo acá acopla todo el sistema. Antes de hacerlo, revisá si no puede vivir en un módulo
  de más abajo o resolverse con inversión de dependencia (como hizo Authentication con
  `IPendingRegistrationCompletion`).

## Dónde tocar si…

- …necesitás un estado de inscripción nuevo → `Constants/EnrollmentStatus.cs`, y revisá que el valor
  coincida con lo que hay en Oracle.
- …querés que un campo nuevo no se loguee → ponele `[Redact]`.
