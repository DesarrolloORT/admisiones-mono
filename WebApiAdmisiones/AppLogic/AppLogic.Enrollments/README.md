# AppLogic.Enrollments

**Nivel 5 — depende de `Contracts`, `DevartDtos`, `Identity`, `People`, las dos integraciones y
`Core/ModBandeja`.** Es el módulo más grande en archivos (54, ~4.300 líneas) y el que fue **piloto**
del refactor.

## Qué resuelve

El corazón del negocio: desde que una persona muestra interés por una carrera hasta que paga la
reserva. Más la **encuesta inicial**, que vive en su propio subárbol.

## El flujo de inscripción

```
1. POST enrollments/product-interest        IRegisterProductInterest
      registra interés por producto + proceso + ofertas, y encola el aviso a Tivenos

2. GET/POST enrollments/initial-survey      IInitialSurveyService
      encuesta inicial (ver más abajo)

3. POST enrollments/confirm-pre-enrollment  IConfirmPreEnrollment
      ├─ request.IsCorporateEnrollment → ConfirmCorporatePreEnrollment (camino separado)
      └─ normal → API de Inscripciones y Pagos

4. POST enrollments/start-payment           IStartEnrollmentPayment
      despacha según StartPaymentRequest.PaymentType
```

## Los cuatro caminos de pago

`StartEnrollmentPayment` es un despachador. Según `PaymentType` (`EnrollmentConstants.PaymentType`):

| Tipo | Caso de uso | Resultado (`PaymentResult`) |
|---|---|---|
| `CUENTA_PERSONAL` | `IPayWithPersonalAccount` | `PAGO_CONFIRMADO` |
| `ABITAB`, `PAGANZA` | `IRegisterExternalPaymentMethod` | `METODO_GUARDADO` |
| `BANRED`, `GEOPAY`, `SISTARBANC` | `IGenerateInvoicePaymentUrl` | `URL_GENERADA` |

Los tres devuelven el mismo `StartPaymentResponse`; lo que cambia es el campo `Result` y si viene
`PaymentUrl` o `Messages`.

## La preinscripción corporativa

`ConfirmCorporatePreEnrollment` es un camino **completamente distinto** al normal: en vez de llamar a
la API de Inscripciones y Pagos, crea un **trámite en la bandeja** (`Core/ModBandeja`) con instancia
de workflow, para que alguien lo procese a mano.

`IdProceso = 89` e `IdGrupoResponsable = 48` están fijos en `EnrollmentConstants.CorporateInbox`: son
valores del esquema. Los ids de estado de proceso **sí** cambian por ambiente (desa 59643/59644,
testing 69993/69994), así que salen de `appsettings` bajo `CorporateInbox:IdEstadoProcesoInicio` y
`CorporateInbox:IdEstadoProcesoSolicitud`. No tienen fallback: si falta la clave, la confirmación
corporativa tira `InvalidOperationException` en vez de crear bandejas con el estado de otro ambiente.

Se registra como clase concreta (`services.AddScoped<ConfirmCorporatePreEnrollment>()`) porque solo
lo usa `ConfirmPreEnrollment`, no el controller.

## La encuesta inicial (`Survey/`)

Subárbol propio con su propia estructura completa:

```
Survey/Dtos/          GetInitialSurveyResponse, SaveInitialSurveyRequest (35 campos), …
Survey/Rules/         InitialSurveyOptions  → los combos FIJOS (Sí/No, valoraciones 1-5, …)
                      InitialSurveyState    → en qué estado quedó la encuesta
Survey/Validators/    InitialSurveyValidation        → validación de campos
                      InitialSurveyCatalogValidation → que los valores estén en los catálogos
Survey/Mapping/       InitialSurveyMapper
Survey/Services/      InitialSurveyService, InitialSurveyChildRecords
```

Cosas a saber:

- La encuesta tiene **guardado parcial** y **finalización definitiva**. `FinalizeDefinitiveSurvey`
  aplica validaciones que el guardado parcial no aplica.
- `InitialSurveyOptions` son los combos que **no** salen de la base (opciones fijas). Los dinámicos
  (universidades, motivos, publicidades, años de bachillerato) los publica `AppLogic.Catalogs`. Por
  eso Catalogs referencia a Enrollments y no al revés.
- `InitialSurveyRejection` es el catálogo de errores más grande del sistema: **32 motivos**, con test
  exhaustivo.
- Los campos "Sí/No" se persisten como `"S"`/`"N"` en Oracle: ver `BoolToYesNo` / `YesNoToBool`.

## Catálogos de error en vez de strings sueltos

Tres enums de negocio con su `ToFailure<T>()`, que es el único lugar donde el motivo se convierte en
código HTTP y mensaje:

- `ProductInterestRejection` — 12 motivos
- `InitialSurveyRejection` — 32 motivos
- `PreEnrollmentRejection` — 3 motivos

El `switch` es exhaustivo: si agregás un motivo y no lo mapeás, tira `ArgumentOutOfRangeException`.

## Qué NO se convirtió a enum, y por qué

`GetOfferingConfirmationData`, `EnsureStudentRegulationsAcceptance`,
`ProductInterestRegistration.RegisterProductInterestRecord`, `SelectedOfferingsCompatibility.Resolve`
y `EnrollmentMapper.MapMultipleApiResult` **conservan `OperationResult`**.

No son validadores hoja: cargan datos, persisten o propagan el resultado de la API interna, y algunos
interpolan datos en el mensaje (`"No se encontro la oferta seleccionada: {id}"`), que un catálogo de
enums no puede expresar. Convertirlos exigiría un enum por operación cuyo único consumidor lo mapea
1:1 de vuelta.

## Reglas que conviene conocer (`Rules/`)

| Clase | Qué decide |
|---|---|
| `SelectedOfferingsCompatibility` | si las ofertas elegidas son combinables entre sí (mismo comienzo, mismo turno) |
| `PreEnrollmentConfirmationRules` | precondiciones de la confirmación |
| `ProductInterestValidation` | validación del interés por producto → `ProductInterestRejection` |
| `ProductInterestRegistration` | el alta del interés, con persistencia |
| `ConfirmedEnrollmentDetails` | arma el detalle de lo confirmado |
| `EnrollmentPaymentRows` | proyección de las filas de pago |
| `InvoicePaymentUrl` | armado de la URL de factura |
| `AdmissionDueDateCalculator` | fecha de vencimiento en **días hábiles**, salteando feriados |

`AdmissionDueDateCalculator` era `GeneralService` y estaba en Catalogs. No es un catálogo: es una
regla de negocio del proceso de admisión, por eso vive acá.

## Trampas

- **`ConfirmPreEnrollment` no hace todo el trabajo**: para el caso corporativo delega y devuelve. Si
  agregás lógica post-confirmación, fijate que aplique a los dos caminos.
- La API de Inscripciones y Pagos puede devolver éxito parcial. `MapMultipleApiResult` propaga eso
  tal cual; no lo simplifiques a un booleano.
- `EnrollmentMapper.ToPaymentMessages` es el límite anticorrupción de los mensajes de pago:
  `CartPaymentMessage` (formato ajeno, en español) → `PaymentMessage` (propio, en inglés).

## Códigos de error

`GEN_IP_*` (interés por producto), `INS_EI_*` (encuesta inicial), `INS_CPI_*` (confirmar
preinscripción), `INS_REA_*` (reactivar), `INS_PAG_*` / `INS_MP_*` / `INS_PC_*` / `INS_UF_*` (pagos),
`INS_DET_*` (detalle), `GEN_OEI_*`, `GEN_FVA_*`.
