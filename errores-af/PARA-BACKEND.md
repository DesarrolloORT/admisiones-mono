# Errores visibles: qué hizo el front y qué le queda al backend

> Documento de traspaso para el equipo de `api-admisiones`. Foto al **2026-10-05**.
> Explica qué cambió en el front (`admisiones`), qué asume ahora del backend y dónde conviene
> que el backend siga con un análisis a fondo. El plan completo y la bitácora están en
> `errores-af/PLAN.md`; el detalle por punto, en `F1a`, `F1b`, `F1c` y `F2-auditoria-back.md`.

## 1. En una página

**Objetivo de la iniciativa**: que Análisis Funcional (AF) pueda ver, revisar y corregir todo
texto de error que ve el usuario, desde un único origen editable por texto.

**Contrato que adoptó el front** (PLAN §1). Lo que le importa al backend está en negrita:

| Regla | Qué significa |
| ----- | ------------- |
| R1 | **Si hay respuesta del backend con `message`, el usuario ve ese `message`. Sin excepciones por status, 5xx incluidos (a partir de F5).** |
| R2 | **Si el backend no quiere contar qué pasó, lo re-mapea él. El front no "protege" al usuario de un `message` del backend.** |
| R3 | El front no se come errores: nunca los convierte en lista vacía, `null`, `false` ni en un dato de negocio inventado. |
| R4 | El front decide flujo, no texto. Puede ramificar por `status` (navegar, "404 = todavía no existe"), pero si muestra algo, es el `message`. |
| R5 | Textos propios del front, solo estos: validación antes de enviar, sin conexión/timeout (status 0), error que no es `NormalizedApiError` y estados vacíos. |
| R6 | Un solo origen editable por texto: `.resx` en backend (clave = `errorCode`) o `*-es.json` en front. |

**La consecuencia que hay que tener presente**: cuando termine F5, **cualquier `message` que el
backend mande, en cualquier status, le llega tal cual al usuario**. Hoy los 5xx todavía no se
muestran (los tapa la librería `ngx-utils`), pero el front ya está preparado para abrirlos. Todo
lo que el backend ponga en `message` tiene que estar curado, en español y sin detalle interno.

### Estado de las fases

| Lado | Hecho | Pendiente |
| ---- | ----- | --------- |
| Front (`admisiones` @ `v1.0.0/fix/errores-af`) | Auditoría completa (F1a/b/c: 89 puntos). **F4a** (auth + home) y **F4b** (enrollments + catalogs) aplicadas: 26 puntos resueltos. Tests 1158 → 1201, `tsc` OK, lint 0 errores. | F4c (scholarships + shared, 16 puntos), F4d (consumir los contratos nuevos de F3c), F5a-bis (migrar `ngx-utils` 3.x → 4.x), F5b (activar 5xx). |
| Librería (`ngx-utils`, `angular-utils` @ `fix/server-error-message`) | F5a: opción `serverErrorMessage: 'generic' \| 'backend'`, default `generic`. 4.1.0, tests 344 → 370. Sin commit; la publicación la hace el usuario. | Publicar 4.1.0. |
| Back (`api-admisiones` @ `rubino-f/fix/errores-af`, commit `df025a20`) | **F3a** (capa host al `.resx`, 21 literales + captcha + middleware) y **F3b** (textos indirectos, 5xx no aptos, resultado incierto). Tests 1783 → 1902, 0 literales en el escaneo. | **F3c** (contratos faltantes, §5 de este documento). |

> El back salió de `gabriele/feat/getScolarship` (decisión D2). Esa serie de `.resx` **no está en
> `develop` ni en `testing`**: solo en `origin/gabriele/feat/getScolarship` y
> `origin/rubino-f/feat/email-integration`. Hay que decidir cómo y cuándo llega a `develop`
> (rebase pendiente; `develop` tiene 32 commits que esta rama no tiene).

---

## 2. Qué se hizo en el front

### 2.1 Auditoría (solo lectura)

Se revisaron los tres bloques de `src/app` y se clasificó cada punto donde el usuario no ve el
`message` del backend:

| Lote | Alcance | Puntos | A arreglar |
| ---- | ------- | ------ | ---------- |
| F1a | `features/auth`, `features/home`, `core` | 34 | 12 |
| F1b | `features/enrollments`, `features/catalogs` | 34 | 18 |
| F1c | `features/scholarships`, `shared` | 21 | 16 |

| Categoría | Qué es | Regla que viola |
| --------- | ------ | --------------- |
| `REMAPEO` | Había un `message` del back y se mostraba otro texto | R1 |
| `SILENCIO` | El error no se muestra (`of([])`, `of(null)`, `EMPTY`, `error: () => undefined`) | R3 |
| `ESTADO-FALSO` | El error se convierte en dato de negocio (p. ej. "no aceptó el reglamento", lista de bancos inventada) | R3 (grave) |
| `CONTRATO-BACK` | El front inventa el texto porque el backend no lo manda | se arregla en F3c |
| `OK-FLUJO` / `OK-RESPALDO` | Decisión de flujo sin texto / texto propio solo sin `NormalizedApiError` | R4 / R5 |

**Hallazgo de contexto que explica por qué había tanto silencio**: `ApiHttpClient` suprime por
defecto el snackbar global (`shared/api/core/api-http-client.ts:193`). Si una pantalla no maneja el
error de su `subscribe`, el usuario no se entera de nada. Por eso la decisión D9 agrega después
(F9a) una regla ESLint que exige manejo de error en cada `subscribe` a la API.

### 2.2 Cambios aplicados (F4a y F4b)

El patrón único es `getApiErrorMessage(error, <respaldo del *-es.json>)`
(`src/app/shared/errors/api-error-message.ts`): devuelve `error.message` si es un
`NormalizedApiError` y el respaldo si no. Cuando la pantalla puede seguir degradada, degrada
**y** muestra el mensaje en el estado de error que ya renderiza.

| Zona | Qué cambió |
| ---- | ---------- |
| Auth | Recuperar contraseña muestra el `message` en vez de convertir cualquier 4xx en éxito (FE-AUTH-006). Cierre de sesión y verificación de sesión informan el error salvo 401 (que sigue siendo flujo). Registro: handlers que no leían el error ahora lo muestran (FE-R5-001/002/005/006/007/008/009, FE-AUTH-018). Copy duplicado de teléfono unificado en `shared/forms/phone-es.json`. |
| Home | El dashboard ya no esconde el fallo de becas o inscripciones con una lista vacía (FE-HOME-003/009/013). |
| Enrollments | **Bancos**: se eliminó la lista hardcodeada de respaldo; si falla, se muestra el error y se puede reintentar (FE-ENR-006). **Reglamento**: un fallo de consulta ya no se interpreta como "no aceptó" (FE-ENR-017). **Detalle de inscripción**: si falla, pantalla de error con reintento (FE-R5-201/203). **Documento y foto de identidad**, países, propuestas, guardado de encuesta: muestran el `message` (FE-R5-204/205/207, FE-ENR-007, FE-R5-206/210). **`confirm-pre-enrollment`**: un 409 ya no se manda a la pantalla "en proceso" (FE-ENR-013). |
| Shared | `AutoSaveOptions.onError` nuevo en `shared/process-flow/auto-save.ts` para que el autoguardado también informe (FE-R5-210; F4c lo conecta en becas, FE-R5-305). |

Lo que **no** se tocó a propósito (decisiones de flujo, R4): ver §3.

---

## 3. Qué asume el front del backend (acoplamientos que el back no debe romper)

El front ya consume estos comportamientos. Si el backend los cambia sin avisar, se rompe una
pantalla. Verificado con `grep` sobre `src/app` (sin specs ni `generated/`): **el front no
ramifica por `errorCode` en ningún lado**; solo por `status`.

| Dónde en el front | Qué asume | Qué no debe hacer el back |
| ----------------- | --------- | ------------------------- |
| `core/interceptors/http.ts:85,111` y `auth-session.ts:96` | 401 = sesión vencida: refresca el token o redirige **sin mostrar texto**. Cualquier otro error de la verificación de sesión se informa con su `message`. | Responder 401 por causas que no sean sesión inválida o vencida. Hoy `AUTH_UNAUTHORIZED` del middleware sale con 401 y texto genérico; ver §4.3. |
| `features/enrollments/services/enrollments.ts:61` y `enrollment-initial-survey.resolver.ts:28` | **404 = "todavía no existe"** (encuesta inicial; precarga de documento y foto de identidad). Es flujo, no error. | Usar 404 para otra cosa en `GET enrollments/initial-survey`, `GET person/identity-document`, `GET person/photo`. |
| F1c `FE-R5-315/316` | 404 en `GET scholarships/affidavit-document` y `GET scholarships/income-receipt` = "no hay documento". | Cambiar ese 404. |
| `enrollment-survey.ts:474,704` (`isAmbiguousConfirmFailure`) | En `confirm-pre-enrollment`, **status 0 o ≥ 500 = resultado incierto**: el front cierra en la pantalla de espera ("Admisiones se va a comunicar con vos") y **no deja reintentar**, porque el endpoint no tiene clave de idempotencia. | **Convertir `INS_CPI_13` / `INS_CPI_18` (hoy 502) en 4xx**: el front pasaría a ofrecer reintento y se arriesga doble inscripción. Los 409 (`INS_CPI_07/12/14/16`) deben seguir siendo 409 con `message`: se muestran como error de la pantalla. |
| `GET catalogs/banks` | Lista vacía con 200 = estado vacío (R5), no error. | Cambiar a 404 una lista vacía. |
| `GET person/enrollments`, `GET person/scholarships` | Siempre 200 con lista (vacía incluida). El front ya **no** trata un 404 como ausencia: lo muestra como error. | Emitir 404 sin documentarlo (F1a `FE-HOME-003`). |
| `POST registration/evaluate-document` | Siempre un flag por rama y siempre un `message` curado; `flowId` en todo flujo salvo `UserAlreadyRegistered`. La matriz flags → flujo se resuelve en el front (R4). | Agregar ramas 2xx sin flag ni `message`. |

Los códigos (`errorCode`) siguen siendo contrato opaco: **no se renombran**, solo cambian los
textos. El front no los lee, pero los inventarios, AF y los tests sí.

---

## 4. Lo que ya hizo el backend (F3a y F3b) y lo que el front espera de eso

### 4.1 Hecho (commit `df025a20`, tests 1783 → 1902)

- `WebApiAdmisiones/Resources/ApiResources.resx` nuevo (host): captcha, rate limit, esquema
  JSON, model binding, `INTERNAL_ERROR`, `AUTH_UNAUTHORIZED`, `SANITIZATION_ERROR`. El detalle
  técnico (variable de entorno, body de Google, `Content-Type`, `jex.Message`) va al log.
- Re-mapeo en los casos de uso de lo que venía de `Core` (LDAP, Azure, `Util`, teléfonos) sin
  tocar `Core`: el original va al log, el usuario ve una clave del `.resx`.
- Resultado incierto en llamadas no idempotentes: `INS_CPI_18`, `INS_PC_03`, `BEC_PO_03`
  (502) con texto que no invita a reintentar; `ApiOutcomeExtensions.IsUncertainOutcome()`.
- Textos con "token", "base de datos", "Request invalido", "link" y similares reescritos
  (lista en `docs/MATRIZ-TRAZABILIDAD.md`).
- Escaneo: `python errores-af/scripts/scan-backend-messages.py api-admisiones/WebApiAdmisiones` →
  0 literales; 6 falsos positivos documentados (4 `Localizer(...)` en
  `ServiceCollectionExtensions.cs`, 2 en `OperationResultExtensions.cs`).
- `Core` intacto: el puntero del submódulo no se movió (`ee5acc01`).

### 4.2 Cambios de código visibles para el front (F4d los consume)

| Endpoint | Antes | Ahora |
| -------- | ----- | ----- |
| `POST person/change-password` | LDAP rechaza: `AUTH_LDAP_20/21/22` (400) | `CAM_PAS_03` (400) |
| `POST person/change-password` | LDAP falla: `AUTH_LDAP_97` (500) | `CAM_PAS_99` (500) |
| `POST auth/complete-initial-password` | `AUTH_LDAP_50/51/52` (400) o `94` (500) | `INI_PAS_05` (conserva 400/500) |
| `POST auth/complete-initial-password` (persona nueva) | `AUTH_LDAP_40/41/42` (400) o `95` (500) | `REG_PERSONA_98` (conserva 400/500) |
| `POST enrollments/confirm-pre-enrollment` | `API_TIMEOUT` / `API_NETWORK` / `API_UNEXPECTED` / `CONFIRMAR_PREINSCRIPCION_MULTIPLE_01` | `INS_CPI_18` (502); `INS_CPI_13` conserva código |
| `POST enrollments/start-payment` (cuenta personal) | ídem con `PAGAR_CARRITOS_01` | `INS_PC_03` (502) |
| `POST scholarships/applications` | ídem con `ALTA_POSTULACION_BECA_01` | `BEC_PO_03` (502) |
| `POST registration/analyze-attachment` | `message` de `Core/AzureService` | mismo `errorCode` y status, texto de `REC_DOC_<n>` del `.resx`; un código de Core sin clave sale como `REC_DOC_99` |

**El front no debe ramificar por `API_*` en esos endpoints.** Hoy no lo hace.

### 4.3 Deuda que F3a/F3b dejaron anotada (candidata a análisis del back)

- `ExceptionHandlingMiddleware` escribe `httpCode: 500` fijo en el cuerpo aunque el status real
  sea 401 o 409 (`WriteErrorResponseAsync`). Un cliente que lea `httpCode` del cuerpo ve 500.
- `AUTH_UNAUTHORIZED` (401) y `SANITIZATION_ERROR` (409) salen del middleware de excepciones;
  el 401 de `[Authorize]` sin token **no** tiene cuerpo (§5.2).
- `SCHEMA_ERROR` responde **400** por un fallo interno (debería ser 5xx).
- `data` de `SCHEMA_BAD_JSON` y `SCHEMA_INVALID` expone detalle del validador en todos los
  ambientes (F2 §1 nota 2). Verificar si F3a lo saneó.
- `ACT_LINK_NUP_03` es un caso de negocio (datos de registro vencidos) servido como **500**.
- `GEN_SFA_02/05` interpola un texto de `Core`.
- Ortografía sin tildes que sigue en `EnrollmentsResources.resx`: `GEN_IP_*`,
  `INS_CPI_02/03/15` y la mayoría de `INS_EI_*`.
- `INI_PAS_02` e `INI_PAS_05` duplicadas en intención (F6 las unifica).
- `AUTH_LDAP_03` no es huérfana (corrección de F3b sobre F2).
- `Ldap.cs`: `AUTH_LDAP_10/11/98` y `30/31/32/96` no tienen llamadores en `AppLogic` (código muerto).

---

## 5. Pendiente del backend

### 5.1 F3c: contratos que el front necesita para cerrar F4d

| # | Endpoint | Qué falta | Código · HTTP · `.resx` | Texto propuesto | Front que lo espera |
| - | -------- | --------- | ----------------------- | --------------- | ------------------- |
| 1 | `POST registration/confirm-new-person` | Si el registro se guardó pero el mail no salió, el `message` del envelope va **vacío**; el texto solo viaja en `data.message`. Poner `IsSuccess(data, method, _localizer["OkRegisteredMailNotSent"])` en esa rama (y un `message` de éxito en la rama normal, ~línea 330 de `RegistrationFlowService.cs`). `verify-identity` ya lo hace (`VerifyIdentity.cs:~209`). | `OkRegisteredMailNotSent` · 200 · `RegistrationResources.resx` (ya existe) | "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña." | FE-AUTH-028: el front deja de inventar el texto (`activationEmailError`/`Hint`). **Ojo**: `confirm-registration-request` con `mailSent:false` es `pendingReview` (alta para revisión manual, sin mail por diseño), **no** es un fallo de mail. |
| 2 | `POST enrollments/start-payment` | Validar tras `Split` en `GenerateInvoicePaymentUrl.cs:~104`: `Data` nulo o URL vacía (hoy sale 200 con `paymentUrl=""`, o NRE → 500), `EncryptedParameters == null` (hoy 200), URL que no es http(s) absoluta. | `INS_UF_05` · 502 · `EnrollmentsResources.resx` (**no existe aún**: `grep` sin resultados) | "No pudimos generar el enlace de pago. Intentá nuevamente más tarde o elegí otro medio de pago." | FE-ENR-003/004 (H5): hoy el front valida la respuesta de la pasarela con `payment.gatewayMissingData` y `gatewayInvalidUrl`. Si se hace esto, esos textos se borran. |
| 3 | `POST enrollments/start-payment` | `StartPaymentResponse.Result` como **enum de string cerrado** en OpenAPI: `PAGO_CONFIRMADO`, `METODO_GUARDADO`, `URL_GENERADA`. Hoy es `string` libre. No existe valor "en proceso" en este endpoint (ese estado lo da `confirm-pre-enrollment`). | cambio de contrato OpenAPI | n/a | FE-R5-209: hoy el front interpreta con `includes()` y toma `confirmed` por defecto. |
| 4 | `POST enrollments/start-payment` (cuenta personal) | `StartPaymentResponse.Messages[]` viene de la API interna (`CartPaymentMessage` `clave`/`valor`) y se copia **sin curar** (`EnrollmentMapper.ToPaymentMessages`). | mapear cada `clave` a `PAGO_MSG_<clave>`; respaldo "El pago se procesó. Revisá el detalle en tu cuenta." | requiere de AF la lista de `clave` que emite la API interna | Hoy ninguna pantalla muestra `Messages`. Decidir si se cura o se descarta antes de que alguna lo muestre. |

Cada cambio de contrato se anota en `docs/MATRIZ-TRAZABILIDAD.md`.

### 5.2 Respuestas sin cuerpo (no son parte de F3c; hay que decidir si entran)

Hoy el front no recibe `message` en estos casos y cae al texto genérico de la librería. Con R1
convendría que el back mande uno (F2 §5 y §9.6):

| Caso | Hoy | Propuesta de F2 |
| ---- | --- | --------------- |
| 401 sin token o token inválido (`[Authorize]`, sin `OnChallenge`) | cuerpo vacío | `AUTH_UNAUTHORIZED` · "Tu sesión no es válida o venció. Iniciá sesión nuevamente." |
| 403 | cuerpo vacío | `AUTH_FORBIDDEN` (nuevo) · "No tenés permiso para realizar esta acción." |
| 404 / 405 de ruteo | cuerpo vacío | `NOT_FOUND` / `METHOD_NOT_ALLOWED` (nuevos) |
| 413 de Kestrel (`MaxRequestBodySize`) | cuerpo vacío, antes del pipeline | `PAYLOAD_TOO_LARGE` (nuevo) |
| 429 de políticas sin handler | cuerpo vacío | texto propio |
| 400 de model binding en producción | `MBM_DA_01` único: el usuario no sabe qué corregir | decisión (§6, punto 6) |

---

## 6. Dónde conviene profundizar (análisis a fondo)

Ordenado por riesgo si se abre el 5xx sin resolverlo.

1. **Auditar de punta a punta qué sale en `message` en cualquier status.** F2 cubrió lo que
   se ve por `IsFailed` y los focos de `Core`. Conviene un barrido independiente por **respuesta
   real** (no por código fuente), incluyendo los caminos de propagación
   (`.Failure().As<T>()`, `OperationResultExtensions`) y los de `Core` que F2 marcó como
   "no alcanzable" (§6.7: `BandejaService`, `ModGenericBase`). Un test que recorra cada clave de
   los `.resx` ya existe por módulo; falta uno que pruebe que **ningún `message` de ningún
   endpoint** contiene `ex.Message`, nombres de variables, hosts o rutas. Es el guardarraíl F9b.
2. **Resultado incierto e idempotencia.** `confirm-pre-enrollment`, `start-payment` (cuenta
   personal) y `scholarships/applications` llaman a la API interna **sin clave de idempotencia**,
   con timeout de 2 minutos. Hoy el front resuelve el caso con una pantalla de espera
   (§3). La solución de raíz es del back: clave de idempotencia o consulta de estado antes de
   reintentar. También verificar si `HttpClient` reintenta solo (Polly u otro) en estas llamadas.
3. **Rechazo de negocio del proveedor con 5xx.** `EnrollmentsAndPaymentsApiClient` pasa a 502
   cualquier 5xx/3xx del proveedor y **pierde la razón** (el comentario del código ya lo
   advierte). El texto actual es genérico. Preguntar al dueño de la API interna si un rechazo
   de negocio puede salir como 5xx.
4. **Anti-enumeración (ASVS).** `recover-password` ya responde 2xx neutro. Quedan 400 de
   formato (`REC_PAS_01/02/03`), captcha y 429 que no dependen de la cuenta; confirmar con
   seguridad. **Hallazgo aparte**: `POST registration/evaluate-document` (anónimo, captcha
   `ScoreOnly`) responde `UserAlreadyRegistered`, lo que revela si una cédula tiene cuenta.
   Decidirlo junto con D4. Además, si falla el envío del mail de recuperación el usuario ve el
   mensaje neutro y no se entera (`REC_LINK_99` inalcanzable por diseño): dejarlo escrito.
5. **Código de referencia en el 500 (D5, diferida).** El 500 genérico sale sin código. El
   `correlationId` ya existe en el middleware. El usuario respondió "que el back decida luego":
   es una decisión pendiente del equipo de backend. Con R1 el usuario verá este texto, así que
   cruzar el reporte de AF o soporte con el log depende de esto.
6. **Mensaje por campo en 400 de producción.** `MBM_DA_01` ("Algunos datos de la solicitud no
   son válidos…") reemplaza todo 400 que no sea `OperationResult`. De ~2630 atributos de
   validación solo 13 tienen `ErrorMessage` propio; el resto responde en inglés. Si se quiere
   decir *qué campo* falla en producción hace falta `InvalidModelStateResponseFactory` o
   `ErrorMessage` en `.resx` por DTO: es contrato nuevo, no solo texto. Recomendación de F2:
   no en esta iniciativa (la validación previa al envío es del front).
7. **Textos a confirmar con AF** (cambian el valor del `.resx`, no la clave): "Admisiones se va
   a comunicar con vos" (`INS_CPI_13/18`, `BEC_PO_03`; confirmar que ese proceso existe, si no,
   "Comunicate con Admisiones"), `CAM_PAS_03` (supone que `AUTH_LDAP_22` es contraseña actual
   incorrecta, pero Core responde 400 también ante falla del servicio), `REC_DOC_11`,
   `SANITIZATION_ERROR`, y los textos de F3b que no estaban en F2 (`LOGIN_LDAP_02/03`,
   `REC_PAS_02/03`, `REG_DOC_01/02`, `GEN_OEI_02`/`INS_EI_55`, `INI_PAS_99`, `FLOW_02/04`,
   variantes de link/token, mensajes de éxito). `REG_TEL_01` y `PER_ADP_07` quedaron con texto
   fijo en vez de lista blanca del detalle del validador.
8. **`FDB_GDJ_36`** nombra al CRM en un 409 determinístico (`SgiInterestAmbiguous`,
   `AffidavitRejection.cs:96`). Propuesta: "No pudimos registrar tu postulación. Comunicate con
   Admisiones para continuar." con el detalle al log. Verificar si se aplicó.
9. **Familia "La API rechazó…"** (`EnrollmentsAndPaymentsResources.resx`): ver los textos
   propuestos en F2 §3.2 (`CONFIRMAR_PREINSCRIPCION_MULTIPLE_01`, `PAGAR_CARRITOS_01`,
   `ALTA_POSTULACION_BECA_01`, `URL_CREAR_FACTURA_01`, `SENIA_MINIMA_01`, `CTACTE_GET_01`, …).
   Confirmar cuáles quedaron aplicados en F3b.
10. **Pedidos aparte a `Core`** (F2 PREGUNTA 6; recomendada: sí). (a) Azure y LDAP no mezclan
    detalle técnico en `Message`; (b) `ValidadorTelefonos.Error` sin `ex.Message` ni centinelas
    ("Error", "---"); (c) opcional, `FILE_VAL_*` y `Util.*` a `.resx`. Hoy todo se resuelve sin
    tocar `Core` re-mapeando en el llamador, pero eso deja la causa de raíz intacta, y cualquier
    caso de uso nuevo que propague `.Failure()` de `Core` vuelve a filtrar.
11. **Documentación del controller desactualizada.** `GET catalogs/banks` declara 400/500/502
    que ya no existen; `POST person/validate-phone-number` documenta "400 Telefono invalido" y el
    código responde 200 con `data:false` (sin motivo).
12. **Dónde viven los textos del host.** Se creó `ApiResources.resx` (recomendación de F2,
    PREGUNTA 1). Si el equipo prefiere repartirlos por módulo, es un movimiento de claves.
13. **Ventana en `AUTH_RL_01/05`.** Dicen la ventana configurada, no el tiempo restante; el
    tiempo real viaja en `Retry-After`. Verificar que el front podría usarlo (hoy no lo lee).

---

## 7. Preguntas del front al backend

- **FE-R5-201 (Detalle de inscripción)**: ¿`GET enrollments/details` puede devolver 404 con
  el significado "sin inscripción"? Hoy el front lo muestra como error con reintento. Si el
  404 es un estado válido, hay que documentarlo.
- **FE-ENR-011 (opcional)**: `GET enrollments/initial-survey` podría responder 200 con
  `survey: null` en vez de 404 y así el front no remapea por status.
- **Imágenes**: las imágenes estáticas no pasan por `HttpClient`; la única imagen del back
  (foto de identidad) atrapa su 404 en `Enrollments.preloadOrThrow`. Si el back agrega otro
  recurso binario, su 404 lo maneja quien llama.
- **Autosave**: si el guardado automático falla ahora se informa (`AutoSaveOptions.onError`).
  Cualquier error de esos endpoints (incluido un eventual 429) lo va a ver el usuario.

---

## 8. Cómo reproducir y verificar

Desde `api-admisiones/WebApiAdmisiones`:

```text
dotnet build WebApiAdmisiones.sln
dotnet test WebApiAdmisiones.sln                      # 1902 tests al cierre de F3b
python ../../errores-af/scripts/scan-backend-messages.py .   # 0 literales; 6 falsos positivos
```

Todo `errorCode` nuevo tiene que sumarse al test `*ResourcesTests` de su módulo: `Localizer`
devuelve **el propio código** si falta la clave, y el usuario vería `INS_UF_05` en pantalla.

Desde `admisiones/`: `npx tsc --noEmit -p tsconfig.app.json`, `npm test` (1201 al cierre de
F4b), `npm run lint:check`. El inventario de textos está en
`admisiones/docs/inventarios/errores-visibles.csv` (461 líneas; 277 filas de backend copiadas del
`.resx` en el commit `98a3c5be`; **van a divergir**, por eso F6 lo genera desde las fuentes).

---

## 9. Dónde está cada cosa

| Qué | Dónde |
| --- | ----- |
| Contrato R1–R6, decisiones D1–D9, bitácora | `errores-af/PLAN.md` |
| Auditoría front por lote (tabla con id, archivo:línea, categoría, fix) | `errores-af/F1a-auth-home.md`, `F1b-enrollments-catalogs.md`, `F1c-scholarships-shared.md` |
| Auditoría back: literales, indirectos, 5xx, `Core`, contratos faltantes | `errores-af/F2-auditoria-back.md` (§1 literales, §2 indirectos, §3 5xx, §5 model binding, §6 `Core`, §9 contratos) |
| Contratos de endpoint que cambiaron | `api-admisiones/docs/MATRIZ-TRAZABILIDAD.md` |
| Textos del host | `WebApiAdmisiones/Resources/ApiResources.resx` |
| Lector único de errores del front | `admisiones/src/app/shared/errors/api-error-message.ts` |
| Política de status que muestran el `message` | `admisiones/src/app/core/services/api-error-policy.ts` (hoy 400, 401, 403, 404, 409, 422, 429; 5xx en F5b) |
| Escaneo del back | `errores-af/scripts/scan-backend-messages.py` |
