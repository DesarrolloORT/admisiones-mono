# Matriz de trazabilidad — refactor modular de AppLogic

Rama `refactor/modular-app-logic`. Ninguna funcionalidad se elimina: cada fila se cierra como
`Migrado` o queda documentada como excepción. Estados: `Pendiente` · `Migrado` · `Dividido` ·
`Eliminado (pasamanos)`.

Baseline capturado en el commit de partida `6fb15bec`:

| Métrica | Valor |
|---|---|
| Build | 0 errores, 293 warnings |
| Tests | 1005 verdes, 0 fallos |
| Endpoints HTTP | 39 en 7 controllers |
| Propiedades públicas en AppLogic (sin `DevartDTOs`) | 451 |

---

## Estado de avance

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Rama, baseline, glosario, matriz | **Hecho** |
| 1 | `AppLogic.Contracts`, `AppLogic.Platform`, `AppLogic.DevartDtos`; ciclos 2 y 4 | **Hecho** |
| 2 | `AppLogic.Integrations.Tivenos`, `AppLogic.Integrations.EnrollmentsAndPayments`; ciclo 1 | **Hecho** |
| 3 + 3b | `AppLogic.Identity`; ciclo 3 cerrado por inversión de dependencia | **Hecho** |
| 5 | Extraer `People`, `Authentication`, `Scholarships`, `Registration`, `Enrollments`, `Catalogs` | **Hecho** |
| 6a | Borrar el proyecto `AppLogic` monolítico | **Hecho** |
| 4a-4b | `InscripcionesService` dividido en **10 casos de uso** + reglas compartidas; el servicio ya no existe | **Hecho** |
| 4c | Enums de negocio en `InteresProductoValidation` + catálogo de errores con tests | **Hecho** |
| 6b | DI por módulo (`AddEnrollments()`, `AddPeople()`, …) | **Hecho** |
| 4c-bis | Enums en `PersonaValidation`, `FondoDeBecaValidation`, `EncuestaInicialCatalogValidation`; `bool` en `RegistroValidation` | **Hecho** |
| 4c-ter | Validadores hoja de `ConfirmarPreInscripcionRules` y `PersonaIdentityRules` | **Hecho** |
| 4d-1 | `Scholarships` renombrado a inglés (tipos, métodos y propiedades) | **Hecho** |
| 4d-2 | `Catalogs`: tipos y métodos en inglés | **Hecho** |
| 4d-3a | Propiedades de los DTOs de `Catalogs` en inglés + contrato JSON actualizado | **Hecho** |
| 4d-3b/c | DTOs de `Enrollments` y encuesta en inglés + los 3 contratos JSON | **Hecho** |
| 4d-3c | DTOs de la encuesta inicial + `encuesta-inicial.contract.json`; carpeta `Encuesta/` renombrada a `Survey/` | **Hecho** |
| 4d-4 | `People`, `Authentication`, `Registration` en inglés, incluidos los DTOs de Redis | **Hecho** |
| 4d-5 | Mappers fuera de los servicios, consolidados en `Mapping/`; los `*EntityFactory` pasaron a `*Mapper` | **Hecho** |
| 4e-1 | `PersonService` dividido en **9 casos de uso** + `Mapping/MyEnrollmentsMapper`; el servicio ya no existe | **Hecho** |
| 4e-2 | `RegistrationService` dividido en **5 casos de uso** + `LdapUserDirectory` y `AdmissionRecords`; el servicio ya no existe | **Hecho** |
| 4f | Auditoría de español remanente: DTOs, métodos, mappers en servicios, params de query y variables locales | **Hecho** |
| 4g | Rutas y controllers en inglés kebab-case; loadtest, docs y contratos actualizados | **Hecho** |
| 4h | `Becas/Inscripciones` y `Registro/AnalizarAdjunto` con DTO propio; `ProducesResponseType` de Catalogs corregidos | **Hecho** |
| 4i | Summaries en las 431 propiedades de DTO propias | **Hecho** |
| 4e-3 | `AuthService` dividido en **5 casos de uso** + `SessionTokenIssuer`; el servicio ya no existe | **Hecho** |

### Cambio de decisión sobre los nombres JSON

La decisión inicial fue preservar los nombres JSON en español con `[JsonPropertyName]`. **Se revirtió**:
el payload viaja en inglés. Los DTOs llevan `<summary>` en español por propiedad.

Esto es un **breaking change** con el front y requiere despliegue coordinado. Lo migrado:

| DTO | Antes | Ahora |
|---|---|---|
| `ScholarshipSummary` | `{"idBeca":…, "nombre":…}` | `{"scholarshipId":…, "name":…}` |
| `IdentityDocumentFile` | `{"nombreArchivo":…, "archivo":…}` | `{"fileName":…, "content":…}` |
| Encuesta inicial | `{"idEncuestaIni":…, "estado":…}` | `{"surveyId":…, "status":…}` |
| Detalle de inscripción | `{"estado":…, "confirmada":{"codigoPersona":…}}` | `{"status":…, "confirmed":{"personId":…}}` |
| `StartPaymentResponse` (ex `DtoPagarResponse`) | `{"resultado":…, "mensajes":…}` | `{"result":…, "messages":…}` |
| Catálogo de carreras | `{"idEscuela":…, "productos":…}` | `{"schoolId":…, "products":…}` |
| Países/estados/ciudades | `{"codigoPais":…, "estado":…}` | `{"countryId":…, "states":…}` |
| `AuthenticationResponse` | `{"persona":{"codigoPersona":…}}` | `{"person":{"personId":…}}` |
| Registro de persona | `{"tipoDocumento":…, "primerApellido":…}` | `{"documentType":…, "firstSurname":…}` |
| Datos de persona | `{"telefono1":…, "identidadRestringida":…}` | `{"primaryPhone":…, "hasRestrictedIdentity":…}` |
| Catálogos de encuesta inicial | `{"educacion":{"opcionesSiNo":…, "aniosBachillerato":[{"orientaciones":…}]}}` | `{"education":{"yesNoOptions":…, "highSchoolYears":[{"tracks":…}]}}` |
| Evaluación de documento | `{"usuarioExistente":…, "requiereAltaPersona":…}` | `{"userAlreadyRegistered":…, "requiresPersonRegistration":…}` |
| Confirmación de registro | `{"mailEnviado":…}` | `{"mailSent":…}` |
| Subida de archivos | `{"archivoAdjunto":{"nombreArchivo":…, "archivo":…}}` | `{"file":{"fileName":…, "content":…}}` |
| Subir documento de identidad | `{"fecha":…, "frente":…, "dorso":…}` | `{"expirationDate":…, "front":…, "back":…}` |
| Verificación 2FA | `{"codigo":…}` | `{"code":…}` |
| Cambio de contraseña | `{"passwordActual":…, "passwordNueva":…}` | `{"currentPassword":…, "newPassword":…}` |
| `catalogs/shifts` | fila cruda de la API interna: `{"idOferta":…, "turno":{"idTurno":…}}` | `{"offeringId":…, "shift":{"shiftId":…}}` |
| `catalogs/banks` | `DtoBancoDevart`: `{"idBanco":…, "nombreBanco":…}` | `{"id":…, "name":…, "code":…, "sistarbancBankId":…}` |
| `catalogs/institutions` | `DtoEmpresaDevart`: fila completa de T_EMPRESA (~38 columnas) | `{"id":…, "name":…}` |

### Query params que también cambian

Además del body, cambian las claves de query string (el front tiene que redesplegarse igual):

| Endpoint | Antes | Ahora |
|---|---|---|
| `catalogs/degree-programs` | `?propuestaAcademica=` | `?academicOffer=` |
| `catalogs/intakes` | `?idCarrera=` | `?degreeProgramId=` |
| `catalogs/shifts` | `?idCarrera=&idProceso=` | `?degreeProgramId=&admissionProcessId=` |
| `catalogs/institutions` | `?codigoPais=&codigoEstado=` | `?countryId=&stateId=` |
| `enrollments/details` | `?idProducto=&idProceso=` | `?productId=&admissionProcessId=` |
| `person/validate-phone-number` | `?telefono1=` | `?isPrimaryPhone=` |

### Rutas: pasan a inglés en kebab-case

Decisión revisada en la fase 4g: las rutas también se traducen. Los controllers pasan de
`[Route("[controller]")]` a una ruta explícita en minúscula, y las clases se renombran
(`BecasController` → `ScholarshipsController`, etc.).

| Antes | Ahora |
|---|---|
| `POST Auth/Login` | `POST auth/login` |
| `POST Auth/VerificarCodigo2FA` | `POST auth/verify-two-factor-code` |
| `POST Auth/ReenviarCodigo2FA` | `POST auth/resend-two-factor-code` |
| `POST Auth/ActivarLinkPassword` | `POST auth/activate-password-link` |
| `POST Auth/CompletarPassword` | `POST auth/complete-initial-password` |
| `POST Auth/Logout` | `POST auth/logout` |
| `POST Auth/RefreshToken` | `POST auth/refresh-token` |
| `POST Auth/RecuperarPassword` | `POST auth/recover-password` |
| `GET Becas/Inscripciones` | `GET scholarships/enrollments` |
| `GET Catalogos/PaisesEstadosCiudades` | `GET catalogs/countries-states-cities` |
| `GET Catalogos/EncuestaInicial` | `GET catalogs/initial-survey` |
| `GET Catalogos/Carreras` | `GET catalogs/degree-programs` |
| `GET Catalogos/Comienzos` | `GET catalogs/intakes` |
| `GET Catalogos/Turnos` | `GET catalogs/shifts` |
| `GET Catalogos/Bancos` | `GET catalogs/banks` |
| `GET Catalogos/Instituciones` | `GET catalogs/institutions` |
| `POST Inscripciones/InteresProducto` | `POST enrollments/product-interest` |
| `GET/POST Inscripciones/EncuestaInicial` | `GET/POST enrollments/initial-survey` |
| `POST Inscripciones/ConfirmarPreInscripcion` | `POST enrollments/confirm-pre-enrollment` |
| `POST Inscripciones/Reactivar` | `POST enrollments/reactivate` |
| `POST Inscripciones/Pagar` | `POST enrollments/start-payment` |
| `GET Inscripciones/ReglamentoEstudiantil` | `GET enrollments/student-regulations` |
| `GET Inscripciones/Detalle` | `GET enrollments/details` |
| `GET/PUT Persona/DatosPersona` | `GET/PUT person/details` |
| `POST Persona/ValidarTelefono` | `POST person/validate-phone-number` |
| `GET Persona/Inscripciones` | `GET person/enrollments` |
| `GET Persona/Becas` | `GET person/scholarships` |
| `POST Persona/CambiarPassword` | `POST person/change-password` |
| `GET Persona/Foto` + `POST Persona/SubirFoto` | `GET/POST person/photo` |
| `GET Persona/Documento` + `POST Persona/SubirDocumento` | `GET/POST person/identity-document` |
| `POST Registro/EvaluarDocumento` | `POST registration/evaluate-document` |
| `POST Registro/VerificarIdentidad` | `POST registration/verify-identity` |
| `POST Registro/AnalizarAdjunto` | `POST registration/analyze-attachment` |
| `POST Registro/ConfirmarNuevaPersona` | `POST registration/confirm-new-person` |
| `POST Registro/ConfirmarSolicitudAlta` | `POST registration/confirm-registration-request` |

Foto y documento pasan a un par GET/POST sobre la misma ruta, en vez de `Foto` + `SubirFoto`.
Siguen siendo 39 endpoints.

Verificado que **no hay configuración acoplada a paths**: rate limiting, cache y captcha se declaran
por atributo, no por ruta. Se actualizaron en el mismo cambio `loadtest/get-endpoints.k6.js`, los
`Docs/*.md`, los `Docs/contracts/*.json` y `LoggingHelperClientTelemetryTests`.

Lo que **no** cambia: los códigos de error, los mensajes, los valores de `CaptchaActions` (viajan a
reCAPTCHA) y las claves de query de la API interna de Inscripciones y Pagos (`idProducto`,
`idProceso`, `tipoPago`, `banco`).

**Única excepción**: los DTOs de `AppLogic.Integrations.*` que **deserializan** respuestas de la API
interna conservan `[JsonPropertyName]` (`clave`, `valor`, `senia`). Modelan el formato de un sistema
ajeno; quitarlos rompe la deserialización. No salen al front: `CartPaymentMessage` se mapea a
`PaymentMessage`, que es el DTO propio en inglés. Cubierto por `RenamedDtoJsonContractTests`.

`Docs/contracts/carreras.contract.json` se actualizó con los nombres nuevos: es el archivo que lee
el front y el que verifica `CarrerasContractTests`.

### Qué NO se convierte a enum, y por qué

`ObtenerDatosConfirmacionOferta`, `AsegurarAceptacionReglamentoEstudiantil`,
`InteresProductoRegistroRules.RegistrarInteresProducto`, `SelectedOfferingsCompatibility.Resolve` e
`InscripcionesMapper.MapearResultadoApiMultiple` conservan `OperationResult`.

No son validadores hoja: cargan datos, persisten o propagan el resultado de la API interna, y algunos
interpolan datos en el mensaje (`"No se encontro la oferta seleccionada: {id}"`), que un catálogo de
enums no puede expresar. Convertirlos exigiría un enum por operación cuyo único consumidor lo mapea
1:1 de vuelta: mueve el acoplamiento en vez de eliminarlo. Son pasos de caso de uso, no helpers.

### Estado de `OperationResult` fuera del borde

Estado final medido:

| Carpeta | Archivos con `OperationResult` |
|---|---|
| `*/Validation/` | **0** |
| `*/Validators/` | 1 — `AffidavitValidation.ValidateAttachment` (2), que delega en `FileValidator` de `Core` (submódulo, fuera de alcance) |
| `*/Rules/` | 3 — `PreEnrollmentConfirmationRules` (16), `ProductInterestRegistration` (5), `SelectedOfferingsCompatibility` (4) |
| `*/Mapping/` | 1 — `EnrollmentMapper` (4) |

`PersonIdentityRules` quedó en **0**: devuelve `bool`. Los 3 de `Rules/` que conservan
`OperationResult` son los documentados arriba como excepción justificada (cargan datos, persisten o
propagan resultados de la API interna).

Catálogos de errores creados, cada uno con test exhaustivo de mapeo enum → (código, HTTP, mensaje):
`ProductInterestRejection` (12), `InitialSurveyRejection` (32), `PersonDataGap` (10),
`AffidavitRejection` (10), `PreEnrollmentRejection` (3). Antes esos códigos eran literales sueltos
dentro de los validadores.

Verificado en cada corte: `dotnet build` 0 errores · `dotnet test` verde · 39 rutas idénticas al
baseline · ninguna propiedad pública perdida.

**Tests: 1005 → 1003.** Se eliminaron dos tests (`ObtenerEncuestaInicial_DelegaEnEncuestaInicialService`
y `GuardarEncuestaInicial_DelegaEnEncuestaInicialService`) porque verificaban el pasamanos de
`InscripcionesService` hacia `IEncuestaInicialService`, que se eliminó: el controller ahora llama al
servicio de encuesta directamente. El comportamiento real sigue cubierto por los 20 tests de encuesta
y por `EncuestaInicialServiceTests`.

## Mapa de proyectos

| Carpeta actual | Proyecto destino | Nivel |
|---|---|---|
| `Common/Security`, `Common/Serialization` (parcial), `Common/Constants` (parcial) | `AppLogic.Contracts` | 1 |
| `DevartDTOs/` (generado) | `AppLogic.DevartDtos` | 1 |
| `Common/Email`, `Common/Serialization`, `Infrastructure/RateLimiting` | `AppLogic.Platform` | 2 |
| `ApiClients/` | `AppLogic.Integrations.EnrollmentsAndPayments` | 2 |
| `Tivenos/` | `AppLogic.Integrations.Tivenos` | 2 |
| `Common/Validation/DocumentUtils`, `Personas/Services/DocumentoIdentidadPersonaService`, `Registro/Interfaces/IPendingPersonaStore`, imágenes temporales | `AppLogic.Identity` | 3 |
| `Personas/` | `AppLogic.People` | 4 |
| `Autenticacion/` (+ `Common/Security`) | `AppLogic.Authentication` | 4 |
| `Becas/` | `AppLogic.Scholarships` | 4 |
| `Registro/` | `AppLogic.Registration` | 5 |
| `Inscripciones/` | `AppLogic.Enrollments` | 5 |
| `Catalogos/` | `AppLogic.Catalogs` | 6 |
| `Helpers/OperationResultExtensions` | `AppLogic.Contracts` (o se elimina, ver §Cierre) | 1 |

Regla: un proyecto solo referencia niveles estrictamente inferiores.

**Niveles corregidos contra el grafo real** (el plan inicial ponía Catalogs en 4 y Scholarships en 6;
al romper los ciclos quedó al revés). Grafo verificado, sin ciclos:

```
Autenticacion  -> (nada)
Becas          -> (nada)
Personas       -> (nada)
Inscripciones  -> Personas
Registro       -> Autenticacion, Personas
Catalogos      -> Inscripciones
```

`AppLogic/Common/`, `AppLogic/Helpers/` y `AppLogic/Infrastructure/` **ya no existen**: cada tipo
quedó con su dueño real.

---

## Ciclos de dependencia a romper

| # | Ciclo | Causa exacta | Resolución | Estado |
|---|---|---|---|---|
| 1 | `ApiClients` ↔ `Inscripciones` | `DtoMensajePagoCarrito` definido en `Inscripciones/Dtos/DtoPagarResponse.cs:18` y usado por el cliente HTTP | El tipo pasó a `Integrations.EnrollmentsAndPayments` como `CartPaymentMessage` con `[JsonPropertyName]` | **Roto** |
| 2 | `Catalogos` ↔ `Inscripciones` | `CatalogosService` usa `EncuestaInicialOpciones`; `EncuestaInicialOpciones` usa `DtoComboOption`; además `EncuestaInicialService` usa `IGeneralService` | `DtoComboOption` → `Contracts.ComboOption`. `GeneralService` (cálculo de fecha de vencimiento con días hábiles) no era un catálogo: pasó a `Inscripciones` como `AdmissionDueDateCalculator`. Queda `Catalogs → Enrollments` unidireccional | **Roto** |
| 3 | `Autenticacion` ↔ `Personas` ↔ `Registro` | `DocumentoIdentidadPersonaService` (estático) usado por Auth y Registro; `IPendingPersonaStore` y `DtoRegistroPendingPersona` usados por Auth; `Personas` usa `Autenticacion.Dtos`; `AuthService` usa `IRegistroFlowService` | Todo lo compartido bajó a `AppLogic.Identity`. `DtoCambiarPasswordRequest` pasó a `Personas` (era suyo). La última arista se resolvió por **inversión de dependencia**: `IPendingRegistrationCompletion` se define en Authentication y la implementa `IRegistroFlowService` | **Roto** |
| 4 | `Becas`/`Registro` → `Inscripciones.Constants` | Constantes de estado del esquema Oracle | `EstadoInscripcion` → `Contracts.EnrollmentStatus`; las constantes SGI de alta de persona → `Registro.SgiPersonRecordConstants` (eran suyas, estaban mal ubicadas) | **Roto** |

### Cómo se cerró el ciclo 3

`AuthService.CompletarPasswordFlowAsync` despacha dos flujos: persona nueva (necesita Registration)
y persona existente (autenticación pura). Mover el método entero a Registration habría llevado
lógica de autenticación al módulo equivocado y tocado 29 tests.

Se aplicó **inversión de dependencia**: `AppLogic.Autenticacion.Interfaces.IPendingRegistrationCompletion`
declara las 4 operaciones que Authentication necesita del registro
(`GetPendingPersonAsync`, `CreatePersonFromPendingAsync`, `DeletePendingPersonAsync`,
`DeleteFlowSessionAsync`). `IRegistroFlowService` la extiende y `RegistroFlowService` la implementa
sin código nuevo. En DI: `services.AddScoped<IPendingRegistrationCompletion>(sp => sp.GetRequiredService<IRegistroFlowService>())`.

Justificación de la interfaz con un solo implementador: es un **límite arquitectónico** — el criterio
que AGENTS.md admite explícitamente.

---

## Enrollments (ex Inscripciones) — módulo piloto

### `InscripcionesService` → casos de uso

| # | Funcionalidad | Origen (`InscripcionesService.cs`) | Destino | Tests | Estado |
|---|---|---|---|---|---|
| E-01 | Registrar interés por producto | `RegistrarInteresProducto` L204-239 | `UseCases/RegisterProductInterest.Execute` | `InteresProductoValidationTests`, `InscripcionesServiceTests` | **Migrado** |
| E-02 | Validar y cargar ofertas del interés | `ValidarYObtenerOfertas` L242-260 | `Rules/InteresProductoValidation.ObtenerOfertaValidaParaInteres` (devuelve enum) + `RegisterProductInterest.ResolverOfertas` | `InteresProductoValidationTests` | **Migrado** |
| E-03 | Registrar interés + encolar Tivenos (1 transacción) | `RegistrarInteresYEncolarTivenos` L266-304 | interno de `RegisterProductInterest` — **no se divide**: unidad transaccional | `InscripcionesServiceTests` | **Migrado** |
| E-04 | Obtener encuesta inicial | `ObtenerEncuestaInicial` L310-313 (pasamanos) | **Pasamanos eliminado**: el controller llama a `IEncuestaInicialService` directo | `EncuestaInicialServiceTests`, `EncuestaInicialContractTests` | **Migrado** |
| E-05 | Guardar encuesta inicial | `GuardarEncuestaInicial` L315-318 (pasamanos) | **Pasamanos eliminado**: el controller llama a `IEncuestaInicialService` directo | idem | **Migrado** |
| E-06 | Confirmar preinscripción (orquestación) | `ConfirmarPreInscripcion` L325-373 | `UseCases/ConfirmPreEnrollment` | `InscripcionesServiceTests` | **Migrado** |
| E-07 | Confirmación corporativa (workflow + bandeja) | `ConfirmarInscripcionCorporativa` L376-427 | `UseCases/ConfirmCorporatePreEnrollment` | idem | **Migrado** |
| E-08 | Confirmación online (API interna) | `ConfirmarInscripcionOnlineAsync` L429-444 | interno de `ConfirmPreEnrollment` | idem | **Migrado** |
| E-09 | Compatibilidad entre ofertas (nivel 1-2 vs 3-4) | `ValidarOfertasCompatibles` + `EsOfertaCompatibleConSeleccion` + `RequiereMismoTurnoEntreOfertas` + `RequiereMismoComienzoEntreOfertas` L451-507 | `Rules/SelectedOfferingsCompatibility` | idem | **Migrado** |
| E-10 | Reactivar inscripción dada de baja | `ReactivarInscripcion` L509-536 | `UseCases/ReactivateEnrollment` | idem | **Migrado** |
| E-11 | Despacho de pago por tipo | `Pagar` L542-599 | `UseCases/Payments/StartEnrollmentPayment.ExecuteAsync` | idem | **Migrado** |
| E-12 | Pago con cuenta personal | `PagarCuentaPersonal` L655-674 | `UseCases/Payments/PayWithPersonalAccount.ExecuteAsync` | idem | **Migrado** |
| E-13 | Guardar método de pago externo (Abitab/Paganza) | `GuardarMetodoPago` L676-726 | `UseCases/Payments/RegisterExternalPaymentMethod.Execute` | idem | **Migrado** |
| E-14 | URL de factura (Banred/Geopay/Sistarbanc) | `ObtenerUrlFactura` L601-638 | `UseCases/Payments/GenerateInvoicePaymentUrl.ExecuteAsync` | idem | **Migrado** |
| E-15 | Separar URL de parámetros encriptados | `SepararUrlYParametrosEncriptados` L642-653 | `Rules/InvoicePaymentUrl.Split` | idem | **Migrado** |
| E-16 | Detalle de inscripción por estado | `ObtenerDetalleInscripcion` L45-109 | `UseCases/GetEnrollmentDetails` | `InscripcionDetalleContractTests` | **Migrado** |
| E-17 | Detalle "pago pendiente" | `ArmarPagoPendiente` L117-161 | interno de `GetEnrollmentDetails` | idem | **Migrado** |
| E-18 | Detalle "confirmada" | `ConstruirDetalleConfirmada` L169-186 | `Rules/ConfirmedEnrollmentDetails` (lo usan E-11 y E-16) | idem | **Migrado** |
| E-19 | Aceptación del reglamento estudiantil | `ObtenerAceptacionReglamentoEstudiantil` L188-201 | `UseCases/GetStudentRegulationsAcceptance` | **sin cobertura** → test de caracterización previo | **Migrado** |
| E-20 | Validar request/ids de inscripción | `ValidarRequestInscripcion` L728-743 | `Validation/RequestedEnrollments.HasValidIds` → **`bool`** | idem | **Migrado** |
| E-21 | Validar pertenencia de inscripciones a la persona | `ValidarPertenenciaInscripciones` L745-755 | `Validation/RequestedEnrollments.AllBelongToPerson` → **`bool`** | idem | **Migrado** |
| E-22 | Proyección de filas de vistas fresco 1y2 / 3y4 | `ToFilaInscripcionPago` L163-167 | `Mapping/EnrollmentPaymentRowMapper` | idem | **Migrado** |

`IInscripcionesService` desaparece: el controller pasa a inyectar los casos de uso.

### Encuesta inicial

| # | Funcionalidad | Origen | Destino | Estado |
|---|---|---|---|---|
| E-23 | Lectura de encuesta inicial | `EncuestaInicialService.ObtenerEncuestaInicial` | `InitialSurveyService.ObtenerEncuestaInicial` (caso de uso vía `IInitialSurveyService`) | **Migrado** |
| E-24 | Guardado parcial/completo | `EncuestaInicialService.GuardarEncuestaInicial` | `InitialSurveyService.GuardarEncuestaInicial` | **Migrado** |
| E-25 | Tablas hijas de la encuesta | `EncuestaInicialChildTablesService` | `InitialSurveyChildRecords` | **Migrado** |
| E-26 | Estado/completitud de la encuesta | `EncuestaInicialState` | `Rules/InitialSurveyState` | **Migrado** |
| E-27 | Opciones de combo de la encuesta | `EncuestaInicialOpciones` | `Rules/InitialSurveyOptions` | **Migrado** |
| E-28 | Validación de la encuesta | `EncuestaInicialValidation`, `EncuestaInicialCatalogValidation` | `Validation/InitialSurveyValidation`, `InitialSurveyCatalogValidation` | **Migrado** |

### Reglas y mapeo

| # | Origen | Destino | Estado |
|---|---|---|---|
| E-29 | `ConfirmarPreInscripcionRules` (413 líneas) | `Rules/PreEnrollmentConfirmationRules` — se divide según lo que muestre la lectura completa | **Migrado** |
| E-30 | `InteresProductoEntityFactory` | `Rules/ProductInterestFactory` | **Migrado** |
| E-31 | `InteresProductoRegistroRules` | `Rules/ProductInterestRegistration` | **Migrado** |
| E-32 | `InscripcionesMapper` | `Mapping/EnrollmentMapper` | **Migrado** |
| E-33 | `EncuestaInicialMapper` | `Mapping/InitialSurveyMapper` | **Migrado** |
| E-34 | `InscripcionesConstants` | `Constants/EnrollmentConstants` (+ `EnrollmentErrors`) | **Migrado** |

---

## Otros módulos — superficie pública a preservar

Una fila por método público de interfaz. **Todos estos módulos ya son proyectos independientes**
(namespaces y assemblies en inglés); lo que sigue pendiente es el renombrado de sus **tipos y
métodos internos**, que es lo que registran estas tablas.

Excepciones ya renombradas: `AppLogic.Identity` (tipos), `ITivenosQueueService` con sus `Enqueue*`,
`IEnrollmentsAndPaymentsApiClient`, `OrtEmailSender`, `JsonSerialization`, `JwtConfiguration`,
`TokenHashing`, `AdmissionDueDateCalculator`, `IPendingRegistrationCompletion` y todo `Enrollments`
excepto sus DTOs.

### People (ex Personas) — `IPersonaService`

| Origen | Destino | Estado |
|---|---|---|
| `ObtenerDatosPersona` | `GetPersonDetails` | **Migrado** |
| `ActualizarDatosPersona` | `UpdatePersonDetails` | **Migrado** |
| `EsTelefonoValidoFront` | `ValidatePhoneNumber` | **Migrado** |
| `ObtenerMisInscripciones` | `GetMyEnrollments` | **Migrado** |
| `CambiarPasswordAsync` | `ChangePasswordAsync` | **Migrado** |
| `ObtenerDocumentoPersona` | `GetPersonIdentityDocument` | **Migrado** |
| `ObtenerFotoPersona` | `GetPersonPhoto` | **Migrado** |
| `SubirFotoPersona` | `UploadPersonPhoto` | **Migrado** |
| `SubirDocumentoPersona` | `UploadPersonIdentityDocument` | **Migrado** |

### Identity (nuevo) — extraído de `Personas` y `Registro`

| Origen | Destino | Estado |
|---|---|---|
| `DocumentoIdentidadPersonaService.ValidarImagenesDocumentoReconocido` | `IIdentityDocumentService.ValidateRecognizedDocumentImages` | **Migrado** |
| `DocumentoIdentidadPersonaService.ObtenerImagenesTemporalesSeguroAsync` | `.GetTemporaryImagesSafeAsync` | **Migrado** |
| `DocumentoIdentidadPersonaService.EliminarImagenesTemporalesSeguroAsync` | `.DeleteTemporaryImagesSafeAsync` | **Migrado** |
| `DocumentoIdentidadPersonaService.GuardarImagenesDocumentoReconocido` | `.SaveRecognizedDocumentImages` | **Migrado** |
| `DocumentoIdentidadPersonaService.ResolverCodigoValidacionDocumento` | `.ResolveDocumentValidationCode` | **Migrado** |
| `DocumentoIdentidadPersonaService.ValidarDocumentosIdentidadParaConfirmacion` | `.ValidateIdentityDocumentsForConfirmation` → devuelve enum | **Migrado** |
| `IPendingPersonaStore` (5 métodos) | `IPendingPersonStore` | **Migrado** |
| `IRegistroDocumentoImagenCacheService` (4 métodos) | `IIdentityDocumentImageCache` | **Migrado** |
| `Common/Validation/DocumentUtils` | `IdentityDocumentRules` | **Migrado** |
| `DtoPersonaAuth` | `AuthenticatedPerson` | **Migrado** |

### Authentication (ex Autenticacion)

| Origen | Destino | Estado |
|---|---|---|
`AuthService` (712 líneas, 5 métodos públicos + 14 privados) se disolvió en 5 casos de uso.

| Origen | Destino | Estado |
|---|---|---|
| `IAuthService.AutenticarUsuarioLDAPAsync` | `UseCases/AuthenticateWithLdap` | **Migrado** |
| `IAuthService.GenerarTokensParaPersonaAsync` | `UseCases/IssueTokensForPerson` | **Migrado** |
| `IAuthService.RefrescarTokensAsync` | `UseCases/RefreshTokens` | **Migrado** |
| `IAuthService.RecuperarPassword` | `UseCases/RecoverPassword` | **Migrado** |
| `IAuthService.CompletarPasswordFlowAsync` | `UseCases/CompletePasswordFlow` | **Migrado** |
| `GenerarYPersistirTokensAsync` + `SISTEMA` + `GetRefreshTokenExpirationDays` | `Services/SessionTokenIssuer` (compartido: 3 casos de uso) | **Migrado** |
| `CoincidePersonaRecupero` | `Rules/PasswordRecoveryValidation.MatchesPerson` | **Migrado** |
| `GetDocumentValidationCodeForLogin` / `...ForPasswordRecovery` | inline en su único caso de uso | **Migrado** |
| `CompletePasswordForExistingPersonAsync`, `CompleteNewPersonFlowAsync`, `CompleteExistingPersonFlowAsync`, `GetTemporaryImagesAsync`, `DeleteTemporaryImagesAsync`, `SaveRecognizedDocumentImages` | privados de `CompletePasswordFlow` (único llamador) | **Migrado** |
| `ErrorInesperadoLog` | `AuthenticationLogs.UnexpectedError` | **Migrado** |
| `IDosFactoresAuthService` (3 métodos) | `ITwoFactorAuthService` | **Migrado** |
| `IPasswordActivationService` (6 métodos) | `IPasswordActivationService` (nombres traducidos) | **Migrado** |
| `ITokenService`, `ITokenServiceInternalApi`, `IHashTokenStore`, `ITwoFactorSessionStore`, `ILoginFlowService` | idem, métodos traducidos | **Migrado** |
| `Common/Security/JwtConfigurationHelper` | `Authentication/JwtConfiguration` (deja de ser "Helper") | **Migrado** |
| `Common/Security/TokenHashHelper` | `Authentication/TokenHashing` | **Migrado** |

### Registration (ex Registro)

| Origen | Destino | Estado |
|---|---|---|
`RegistrationService` (682 líneas, 5 métodos públicos + 17 privados) se disolvió en 5 casos de uso.
Lo único que quedó compartido son los dos colaboradores que usa más de un caso de uso.

| Origen | Destino | Estado |
|---|---|---|
| `IRegistroService.EvaluarDocumentoAsync` | `UseCases/EvaluateDocument` | **Migrado** |
| `IRegistroService.VerificarIdentidadAsync` | `UseCases/VerifyIdentity` | **Migrado** |
| `IRegistroService.ConfirmarSolicitudAltaAsync` | `UseCases/ConfirmRegistrationRequest` | **Migrado** |
| `IRegistroService.ValidarNuevaPersonaAsync` | `UseCases/ValidateNewPerson` | **Migrado** |
| `IRegistroService.CompletarNuevaPersonaAsync` | `UseCases/CompleteNewPerson` | **Migrado** |
| `RegistrationService.{LdapUserExistsAsync, CreateLdapUserAsync, CambiarPasswordLdapAsync}` | `Services/LdapUserDirectory` (compartido: 3 casos de uso) | **Migrado** |
| `RegistrationService.{RegisterAdmissionByPerson, RegisterAdmissionByRegistrationRequest}` | `Services/AdmissionRecords` (compartido: 3 casos de uso) | **Migrado** |
| `RegistrationService.GetDocumentValidationCode` | `RegistrationValidation.ResolveDocumentValidationCode` | **Migrado** |
| `requestErrorCode` / `documentTypeErrorCode` sueltos en el servicio | `Constants/RegistrationErrorCodes` | **Migrado** |
| `CreateUserRegisterAdmissionAndSendPasswordMailAsync`, `SendPasswordLinkMailAsync` | privados de `VerifyIdentity` (único llamador) | **Migrado** |
| `CreatePersonInDb`, `PersistMetadataAndAdmission`, `SaveRecognizedDocumentImages`, `CompletePasswordForPendingExistingPersonAsync`, `IsSamePendingPerson`, `UpdatePasswordMetadata` | privados de `CompleteNewPerson` (único llamador) | **Migrado** |
| `CreateRegistrationRequestAsync` | cuerpo de `ConfirmRegistrationRequest` (único llamador) | **Migrado** |
| `IRegistroFlowService` (9 métodos) | `IRegistrationFlowService` | **Migrado** |
| `RegistroEntityFactory` | `RegistrationMapper` | **Migrado** |

### Catalogs (ex Catalogos)

| Origen | Destino | Estado |
|---|---|---|
| `ObtenerPaisesEstadosCiudadesAsync` | `GetCountriesStatesCitiesAsync` | **Migrado** |
| `ObtenerEncuestaInicialAsync` | `GetInitialSurveyCatalogsAsync` | **Migrado** |
| `ObtenerCarreras` | `GetDegreePrograms` | **Migrado** |
| `ObtenerComienzos` | `GetIntakes` | **Migrado** |
| `ObtenerTurnos` | `GetShifts` | **Migrado** |
| `ObtenerBancosAsync` | `GetBanksAsync` | **Migrado** |
| `ObtenerInstituciones` | `GetInstitutions` | **Migrado** |
| `ObtenerFondosDeBecaPorProducto` | — | **Eliminado**: no tenía ningún consumidor (ni controller ni servicio). Se borró junto con sus 2 tests |
| `IGeneralService.CalcularFechaVencimientoAdmisiones` | `IAdmissionCalendar.CalculateAdmissionDueDate` (deja de ser "General") | **Migrado** |
| `PropuestaAcademica` (enum + miembros) | `AcademicOffer.{UniversityDegree, TechnicalDegree, ProfessionalUpdate}` | **Migrado** |
| Armado inline de la encuesta dentro de `CatalogService` (60 líneas) | `Mapping/InitialSurveyCatalogMapper` | **Migrado** |
| `CarreraCatalogoItem` + `ToCarreraCatalogoItem` + `MapProductos` + el GroupBy, todos privados del servicio | `Mapping/DegreeProgramMapper.{DegreeProgramRow, ToRow, ToGroupedResponse}` | **Migrado** |
| `DtoBancoDevart` saliendo al front | `Dtos/BankResponse` + `Mapping/BankMapper` | **Migrado** |
| `DtoEmpresaDevart` saliendo al front | `Dtos/InstitutionResponse` + `Mapping/InstitutionMapper` | **Migrado** |
| `OfertaInscripcionDto` (DTO de integración) saliendo al front | `Dtos/OfferingResponse` + `Mapping/OfferingMapper` | **Migrado** |
| `Combo()` privado duplicado en 2 clases | `ComboOption.Of(value, label)` | **Migrado** |

### Scholarships (ex Becas)

| Origen | Destino | Estado |
|---|---|---|
| `IBecasService.ObtenerMisInscripcionesConfirmadas` | `GetMyConfirmedEnrollments` | **Migrado** |
| `IFondoDeBecaService` (13 métodos: tipos + subir/descargar/eliminar archivos) | `IScholarshipFundService` con nombres `Get*`, `Upload*`, `Download*`, `Delete*` | **Migrado** |
| `FondoDeBecaValidation` | `ScholarshipFundValidation` | **Migrado** |

### Integrations

| Origen | Destino | Estado |
|---|---|---|
| `IInscripcionesyPagosApiClient` (8 métodos) | `IEnrollmentsAndPaymentsApiClient` | **Migrado** |
| `ITivenosEnvioService` (3 métodos `Encolar*`) | `ITivenosQueueService` con `Enqueue*` | **Migrado** |

### Platform

| Origen | Destino | Estado |
|---|---|---|
| `IRateLimiterService` + `RedisRateLimiterService` | igual, en `AppLogic.Platform` | **Migrado** |
| `IEmailSender` + `EnvioMailEmailSender` | `IEmailSender` + `OrtEmailSender` | **Migrado** |
| `JsonSerializationHelper`, `JsonSerializationDefaults` | `JsonSerialization` (deja de ser "Helper") | **Migrado** |

---

## Política de `OperationResult`

`OperationResult<T>` **se conserva** en la firma de los casos de uso: es el body JSON serializado en
`ApiBaseController.ValidateResponse` y el front lo consume sin versionado de API.

Se **elimina** de reglas, validadores, mappers y factories. Cada módulo define un enum de negocio y
un catálogo que traduce enum → (`ErrorCode`, `HttpCode`, `Message`) en un único punto. Los códigos y
mensajes actuales se preservan literalmente.

| Archivo | Devuelve hoy | Devuelve después |
|---|---|---|
| `InteresProductoValidation.ValidarOfertasSolicitadas` | `OperationResult<bool>` | `ProductInterestRejection` |
| `InteresProductoValidation.ValidarRegistroInteresProducto` | `OperationResult<bool>` | `ProductInterestRejection` |
| `InteresProductoValidation.ObtenerOfertaValidaParaInteres` | `OperationResult<Oferta>` | `(Oferta?, ProductInterestRejection)` |
| `ConfirmarPreInscripcionRules.ValidarRequest` | `OperationResult<bool>` | `PreEnrollmentRejection` |
| `InscripcionesService.ValidarRequestInscripcion` | `OperationResult<List<long>>` | `bool` |
| `InscripcionesService.ValidarPertenenciaInscripciones` | `OperationResult<bool>` | `bool` |
| `InscripcionesMapper.MapearResultadoApiMultiple` | `OperationResult<Dto>` | DTO + enum |
| `DocumentoIdentidadPersonaService.ValidarDocumentosIdentidadParaConfirmacion` | `OperationResult<bool>` | `IdentityDocumentStatus` |
| `PersonaValidation`, `RegistroValidation`, `FondoDeBecaValidation`, `EncuestaInicialCatalogValidation` | `OperationResult<T>` | enum por módulo |

---

## Cierre

`OperationResultExtensions` se mide al final: si sobreviven menos de 5 usos se inlinean y el helper
se elimina; si sobreviven más (propagación entre casos de uso e integraciones) se muda a
`AppLogic.Contracts`.
