# AppLogic.Scholarships

**Nivel 4 — depende de `AppLogic.Contracts` y `AppLogic.DevartDtos`.**

## Qué resuelve

Dos cosas bastante distintas que conviven acá:

1. **Becas** (`IScholarshipService`) — las inscripciones confirmadas de la persona, que son la base
   del circuito de becas. **Esto sí está en uso.**
2. **Fondo de beca / declaración jurada** (`IScholarshipFundService`) — catálogos y adjuntos de la
   declaración jurada. **Esto NO está conectado a ningún endpoint.**

## ⚠️ Lo primero que hay que saber: `IScholarshipFundService` no lo usa nadie

Está registrado en DI (`services.AddScholarships()`) y tiene tests, pero **ningún controller lo
inyecta**. Sus 13 operaciones (tipos de parentesco, egreso, vivienda, universidades, y subir /
descargar / eliminar los tres tipos de archivo) no son alcanzables por HTTP hoy.

Lo mismo pasa con:

- `AffidavitValidation.ValidateSaveRequest` y `ValidateForConfirmation` — sin consumidores.
  `ValidateAttachment` sí se usa, desde los tres `Upload*File`.
- `AffidavitDetails` + `AffidavitMapper` — sin consumidores.
- `Constants/AffidavitRejection` — el catálogo de errores existe pero solo lo alcanzan los dos
  validadores muertos.

Es un área **a la espera de una decisión de producto**. No la borres asumiendo que es basura, ni la
"arregles" asumiendo que está en producción. Preguntá primero.

## Lo que sí está en uso

`IScholarshipService.GetMyConfirmedEnrollments(personId)` → `GET scholarships/enrollments`.

Lee `VD_INSCRIPCIONES_FRESCO_1Y2`, filtra por `EstadoInscripcion == EnrollmentStatus.Confirmed` y
mapea a `ConfirmedEnrollmentResponse`.

Hasta hace poco este endpoint devolvía la fila cruda `DtoVdInscripcionesFresco1y2Devart` con nombres
en español. Ahora tiene DTO propio + `ConfirmedEnrollmentMapper`.

`ScholarshipSummary` es el otro DTO propio del módulo, ya en inglés.

## Estructura

```
Interfaces/IScholarshipService        1 método — EN USO
Interfaces/IScholarshipFundService   13 métodos — SIN CONSUMIDOR
Services/                             las dos implementaciones
Dtos/ConfirmedEnrollmentResponse      respuesta de scholarships/enrollments
Dtos/ScholarshipSummary               resumen de beca
Dtos/AffidavitDetails                 SIN CONSUMIDOR
Dtos/FileDownload                     archivo listo para descargar
Mapping/                              ConfirmedEnrollmentMapper (en uso), AffidavitMapper (no)
Validators/AffidavitValidation        solo ValidateAttachment está en uso
Constants/AffidavitRejection          catálogo de 10 motivos, alcanzable solo por código muerto
Constants/ScholarshipFundConstants
```

## Trampas

- **`IScholarshipFundService` sigue devolviendo DTOs de Devart** (`DtoTipoParentescoDevart`,
  `DtoEmpresaDevart`, …). No se migraron a DTOs propios porque no salen a ningún endpoint. Si algún
  día se conectan, hay que mapearlos igual que se hizo con el resto.
- Los archivos de la declaración jurada se guardan **en la base** (columnas BLOB de
  `T_INGRESO_MENSUAL_NF_DJ`, `T_EGRESO_MENSUAL_NF_DJ`, `T_DECLARACION_JURADA_WEB`), no en disco ni en
  storage externo.
- Todos los `Download*` / `Delete*` verifican que el registro **pertenezca a la persona autenticada**
  (`BelongsToPerson`). Es una comprobación de autorización, no una validación cosmética: sin ella
  cualquiera bajaría los comprobantes de otro.

## Códigos de error

`FDB_DAI_*` (archivo de ingreso), `FDB_DAE_*` (egreso), `FDB_DAR_*` (reválida), `FDB_GDJ_*`
(declaración jurada), `FDB_UV_*` (universidades).
