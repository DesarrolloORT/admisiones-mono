# Glosario de dominio — español → inglés

Fuente única de verdad para el refactor `refactor/modular-app-logic`. Todo identificador de C# en
`AppLogic.*` y en `WebApiAdmisiones` usa la columna **Inglés**. Un término español = un único término
inglés: si necesitás una traducción que no está acá, agregala primero a esta tabla.

## Qué se traduce y qué no

| Elemento | Idioma | Motivo |
|---|---|---|
| Proyectos, namespaces, carpetas | **Inglés** | |
| Clases, interfaces, métodos, parámetros, variables | **Inglés** | |
| Tipos y propiedades de DTO | **Inglés** | el JSON viaja en inglés (ver abajo) |
| Enums y constantes | **Inglés** | |
| Nombres de tests | **Inglés** | |
| Mensajes de error (`Message`) | Español | Texto de producto visible al usuario final |
| Comentarios y XML docs | Español | Decisión de equipo: sin churn extra |
| Rutas HTTP | **Inglés, kebab-case** | Se tradujeron en la fase 4g: `auth/login`, `registration/evaluate-document`. Breaking change coordinado con el front |
| Nombres JSON en el cable | **Inglés** | **Breaking change acordado**: los payloads pasan de `{"estado":…}` a `{"status":…}`. Requiere despliegue coordinado con el front |
| Códigos de error (`GEN_IP_01`) | Sin cambio | Opacos, son contrato |
| `BusinessLogic`, `DataAccess`, `Core/` | Español | Código Devart generado y submódulo compartido con otros proyectos ORT |

`AppLogic` es la capa anticorrupción: las entidades entran en español desde `BusinessLogic`
(`Inscripto`, `Persona`, `Oferta`) y salen mapeadas a tipos en inglés.

### Única excepción: DTOs que deserializan sistemas ajenos

Los DTOs de `AppLogic.Integrations.*` que **reciben** respuestas de la API interna de Inscripciones y
Pagos conservan `[JsonPropertyName]` con los nombres originales (`clave`, `valor`, `senia`). No es
una preferencia de nomenclatura: describen el formato que emite otro sistema. Si se quitan, la
deserialización deja de mapear.

Esos tipos **no salen al front**: cada módulo tiene su propio DTO de respuesta en inglés y mapea
entre ambos (por ejemplo `CartPaymentMessage` → `PaymentMessage`). Ese mapeo es el límite
anticorrupción, y está cubierto por `RenamedDtoJsonContractTests`.

### Documentación de propiedades

Los DTOs llevan `<summary>` en español por propiedad explicando qué es cada atributo: el nombre
queda en inglés y la explicación en el idioma del equipo.

## Términos

| Español | Inglés | Nota |
|---|---|---|
| Inscripción, Inscripto | `Enrollment` | `IdInscripto` → `EnrollmentId` |
| Preinscripción | `PreEnrollment` | |
| Interés por producto | `ProductInterest` | |
| Producto | `Product` | `IdNivelProducto` → `ProductLevelId` |
| Proceso | `AdmissionProcess` | "Process" a secas es ambiguo |
| Oferta | `Offering` | |
| Supraoferta | `ParentOffering` | |
| Comienzo | `Intake` | cohorte de inicio, no `Start` |
| Turno | `Shift` | |
| Paquete | `Package` | |
| Carrera | `DegreeProgram` | no `Career` |
| Encuesta inicial | `InitialSurvey` | |
| Persona | `Person` | |
| Documento, Cédula | `IdentityDocument` | |
| Beca | `Scholarship` | |
| Fondo de beca | `ScholarshipFund` | |
| Declaración jurada | `Affidavit` | |
| Registro (onboarding) | `Registration` | |
| Seña, Seña mínima, Reserva mínima | `MinimumDeposit` | el código actual usa **tres** términos para un solo concepto: se unifica |
| Pago | `Payment` | |
| Carrito | `Cart` | |
| Factura | `Invoice` | |
| Cuenta personal | `PersonalAccount` | |
| Cuenta corriente | `CurrentAccount` | |
| Reglamento estudiantil | `StudentRegulations` | |
| Baja | `Cancellation` | |
| Reactivar | `Reactivate` | |
| Corporativa | `Corporate` | |
| Instancia de workflow | `WorkflowInstance` | |
| Trámite | `Case` | |
| Bandeja | `Inbox` | módulo de `Core`: **no se renombra** |
| Estado | `Status` | |
| Bachillerato | `HighSchool` | |
| Institución | `Institution` | |
| Banco | `Bank` | |
| País, Estado (geo), Ciudad | `Country`, `State`, `City` | `Estado` geográfico ≠ `Status` |
| Foto | `Photo` | |
| Archivo | `File` | |
| Ingreso / Egreso (declaración jurada) | `Income` / `Expense` | |
| Revalida | `Revalidation` | |
| Parentesco | `Kinship` | |
| Vivienda | `Housing` | |
| Universidad | `University` | |
| Fecha de vencimiento | `DueDate` | |
| Vigente | `Active` | |
| Orientación (de bachillerato) | `Track` | no `Orientation`; ya lo usaba `HighSchoolTrackId` |
| Valoración (escala 1-5) | `Rating` | |
| Publicidad | `Advertising` / `Advertisement` | |
| Motivo de elección | `ChoiceReason` | |
| Apoyo en la decisión | `DecisionSupport` | |
| Nivel de formación | `EducationLevel` | padre y madre en la encuesta |
| Educación media superior | `UpperSecondary` | |
| Propuesta académica | `AcademicOffer` | el enum, no `AcademicProposal` |
| Solicitud de alta | `RegistrationRequest` | |
| Prueba (de beca) | `Test` | |
| Centro de costos | `CostCenter` | |
| Subestado | `SubStatus` | |
| Opciones Sí/No | `YesNoOptions` | |

## Verbos

| Español | Inglés |
|---|---|
| Obtener | `Get` |
| Guardar | `Save` |
| Registrar | `Register` |
| Confirmar | `Confirm` |
| Validar | `Validate` |
| Actualizar | `Update` |
| Eliminar | `Delete` |
| Subir | `Upload` |
| Descargar | `Download` |
| Enviar | `Send` |
| Encolar | `Enqueue` |
| Calcular | `Calculate` |
| Armar, Construir | `Build` |
| Mapear | `Map` |
| Separar | `Split` |
| Completar | `Complete` |
| Verificar | `Verify` |
| Evaluar | `Evaluate` |
| Recuperar (password) | `Recover` |
| Refrescar (token) | `Refresh` |
| Reenviar | `Resend` |
| Analizar | `Analyze` |

## Nombres propios (no se traducen)

`Tivenos` · `LogicaORT` · `ORT` · `Devart` · `Sistarbanc` · `Banred` · `Geopay` · `Abitab` ·
`Paganza` · `Santander` · `LDAP` · `Redis` · `Fresco` (vistas `Vd*Fresco*`)

## Dónde va el mapeo

Cada módulo tiene una carpeta `Mapping/` con clases `*Mapper`. Ahí va **toda** traducción entre
formas: entidad → DTO, request → entidad, respuesta de API externa → DTO propio.

Reglas:

- Un mapper es una **transformación pura**: sin `IUnitOfWork`, sin I/O, sin decisiones de negocio.
  Si necesita consultar la base o elegir entre caminos, no es un mapper: es una regla o un paso del
  caso de uso.
- **No existe `*EntityFactory`.** Las clases que se llamaban así construían entidades desde un
  request, que es exactamente mapear: `RegistrationEntityFactory` → `RegistrationMapper`,
  `InteresProductoEntityFactory` → `ProductInterestMapper`. Vivían en `Rules/`, mezcladas con
  reglas de negocio reales.
- Los servicios y casos de uso **no declaran métodos de mapeo privados**. Si aparece un
  `Build*`/`Map*` privado que solo arma un objeto, va al mapper del módulo.

## Convención de DTOs

Se elimina el prefijo `Dto`. Sufijo `Request` / `Response` estándar .NET.

| Antes | Después |
|---|---|
| `DtoInteresProductoRequest` | `ProductInterestRequest` |
| `DtoDetalleInscripcionResponse` | `EnrollmentDetailsResponse` |
| `DtoPagarRequest` | `StartPaymentRequest` |
| `DtoGuardarEncuestaInicialResponse` | `SaveInitialSurveyResponse` |

Los DTOs que no son request ni response de un endpoint llevan un sufijo que describe qué son
(`EnrollmentPaymentRow`, `OfferingConfirmationData`), nunca `Dto` a secas.
