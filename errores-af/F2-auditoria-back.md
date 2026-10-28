# F2 — Auditoría back: ¿todo error sale curado y desde el `.resx`?

Solo lectura. Fecha: 2026-10-02. Alcance: tareas 1 a 7 de F2 (la 7 está en §9).

**Base auditada** (D2): `api-admisiones` @ `gabriele/feat/getScolarship` (HEAD `4ac780e2`), submódulo `Core` en `ee5acc0`. Las rutas son relativas a `api-admisiones/WebApiAdmisiones/` (capas `AppLogic/…`, host `WebApiAdmisiones/…`) o a `api-admisiones/Core/…`. Los números de línea son de esta base.

## 0. Resumen

- **Escaneo** (tarea 1): `python errores-af/scripts/scan-backend-messages.py api-admisiones/WebApiAdmisiones` → 222 `IsFailed`: 182 desde `.resx`, **21 literales**, 13 variables, 6 `.Message` de otra capa (19 indirectos). 29 puntos 5xx. Coincide con el diagnóstico del plan.
- **Dónde está el problema real**: no solo en los 21 literales. Hay tres focos que el escaneo no ve porque el texto llega por propagación (`.Failure().As<T>()`, `validation.Message`) y no por un `IsFailed` con literal:
  1. **`Core/AzureService`** (reconocimiento de documento): llega directo al `message` en `RegistrationController.AnalyzeAttachment` y filtra nombres de variables de entorno, host, modelo y versión de API de Azure y `ex.Message`. Es el hallazgo más grave.
  2. **`Core/LdapService`**: `ChangePassword`, `CompletePasswordFlow` y `CompleteNewPerson` propagan su `message`, con `ex.Message` en los 500 y detalle de configuración en los 400.
  3. **`Core/Utilities/Util`** (cédula y contraseña): texto en español pero sin código ni `.resx`; `ValidarPasswordNueva` devuelve "Parámetros incorrectos" para casos que el usuario puede corregir.
- **Un literal más que el escaneo no cuenta** (porque pasa por un parámetro): `REG_CAPTCHA_05` en `Security/Captcha/RequireCaptchaAttribute.cs` (`SetFailureResult`, ~línea 90). Se incluye en la tabla como L22.
- **El escaneo cuenta `ExceptionHandlingMiddleware:198` entre los 19 indirectos**, aunque el texto es un literal ("Error inesperado"). Lo trato como indirecto (I19) para que las cifras cierren con el plan, y propongo clave igual que a un literal.
- **D4 ya está resuelta en esta base** (ver §7): `RecoverPassword` responde 2xx neutro (`OkPasswordRecoveryRequested`) para documento inexistente, datos que no coinciden y excepción.
- **Efecto de que `Localizer` devuelva el código si falta la clave**: cualquier clave nueva debe sumarse al test `*ResourcesTests` del módulo, o el usuario vería el código.

### Convención propuesta para el host

La capa host no tiene `.resx`. Propuesta (sigue el patrón de `AppLogic.*`, ver marcador en `AuthenticationResources.cs`):

- `WebApiAdmisiones/Resources/ApiResources.resx` + `ApiResources.cs` (clase marcadora vacía, mismo nombre y carpeta; namespace `WebApiAdmisiones.Resources`). Se inyecta `IStringLocalizer<ApiResources>`. `AddLocalization()` ya está en `Program.cs:44`.
- Clave = `errorCode`. Si un mismo código sale con textos distintos, sufijo (convención ya usada: `FLOW_03_Document`, `ACT_SES_03_Complete`).
- Para los manejadores estáticos de rate limit (`ServiceCollectionExtensions`) el localizer se resuelve con `context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<ApiResources>>()`.
- Test nuevo `UnitTesting/.../ApiResourcesTests.cs` con cobertura de claves, como los otros módulos.
- Los códigos son contrato opaco con el front: **no se renombran**. Solo cambian los textos.

---

## 1. Tabla de literales (tarea 2) — 21 del escaneo + 1 detectado (L22)

Columnas: dónde, código y HTTP actuales, texto actual, problema, clave propuesta y texto propuesto. "Resx" indica dónde va la clave. Todos los textos usan voseo y evitan términos técnicos.

| # | Archivo:línea | Código · HTTP | Texto actual | Problema | Clave y resx | Texto propuesto |
| --- | --- | --- | --- | --- | --- | --- |
| L1 | `Controllers/ScholarshipsController.cs:330` | `FDB_ADJ_01` · 400 | "No se recibió el archivo adjunto." | Aceptable, pero literal. El módulo ya tiene la familia `FDB_*` en `ScholarshipsResources.resx`. | `FDB_ADJ_01` en `AppLogic.Scholarships/Resources/ScholarshipsResources.resx` (el controller necesita `IStringLocalizer<ScholarshipsResources>`; hoy no tiene localizer) | "No se recibió el archivo adjunto. Seleccioná un archivo e intentá nuevamente." |
| L2 | `Extensions/ServiceCollectionExtensions.cs:270` | `REC_DOC_15` · 429 | "Se superó el límite de solicitudes de reconocimiento de documentos. Intentá nuevamente en unos minutos." | Limpio de datos internos; redacción técnica ("solicitudes"). | `REC_DOC_15` en `ApiResources.resx` | "Hiciste demasiados intentos de reconocer el documento. Esperá unos minutos e intentá nuevamente." |
| L3 | `ServiceCollectionExtensions.cs:349` | `PERS_RL_01` · 429 | "Se superó el límite de validaciones de teléfono. Intentá nuevamente en un minuto." | Apto. | `PERS_RL_01` en `ApiResources.resx` | "Hiciste demasiadas validaciones de teléfono. Esperá un minuto e intentá nuevamente." |
| L4 | `ServiceCollectionExtensions.cs:471` | `AUTH_RL_05` · 429 | `$"…Intentá nuevamente en {(int)window.TotalMinutes} minutos."` | Apto; interpolado. | `AUTH_RL_05` en `ApiResources.resx` (formato `{0}` = minutos, como `AUTH_RL_03`) | "Se superó el límite de solicitudes desde esta red. Intentá nuevamente en {0} minutos." |
| L5 | `ServiceCollectionExtensions.cs:642` | `AUTH_RL_01` · 429 | `$"…desde esta red ({maxIpAttempts} intentos cada {windowMinutes} minutos). Por tu seguridad…"` | **Filtra configuración de seguridad** (umbral y ventana de rate limit). | `AUTH_RL_01` en `ApiResources.resx` (las claves `AUTH_RL_02..04` ya están en `AuthenticationResources.resx`; `01` y `05` son las que faltan de la familia) | "Se superó el límite de intentos de inicio de sesión desde esta red. Por tu seguridad, intentá nuevamente en {0} minutos." (`{0}` = ventana; sin el número de intentos) |
| L6 | `Security/Captcha/RecaptchaService.cs:32` | `AUTH_CAPTCHA_01` · 400 | "El parametro captcha es obligatorio." | Sin tildes; "parámetro". reCAPTCHA v3 es invisible: "falta completar" sugeriría un paso visible que no existe. | `AUTH_CAPTCHA_01` en `ApiResources.resx` | "No pudimos hacer la verificación de seguridad. Recargá la página e intentá nuevamente." |
| L7 | `RecaptchaService.cs:46` | `AUTH_CAPTCHA_02` · 500 | `$"No se encontro la configuracion de reCAPTCHA v3. Defini {SecretKeyEnvironmentVariable}."` | **Filtra el nombre de la variable de entorno** (`RECAPTCHA_SECRET_KEY`) y es una instrucción para el operador. El detalle debe ir al log. | `AUTH_CAPTCHA_02` en `ApiResources.resx` | "No pudimos completar la verificación de seguridad en este momento. Intentá nuevamente más tarde." |
| L8 | `RecaptchaService.cs:66` | `AUTH_CAPTCHA_03` · 502 | `$"Error al validar captcha contra Google: {body}"` | **Filtra el body completo de Google** al usuario. | `AUTH_CAPTCHA_03` en `ApiResources.resx` | "No pudimos completar la verificación de seguridad en este momento. Intentá nuevamente en unos minutos." |
| L9 | `RecaptchaService.cs:83` | `AUTH_CAPTCHA_04` · 400 | `$"Google no valido el captcha. {GetErrorCodes(root)}"` | **Filtra los `error-codes` de Google** ("Codigos: …") y nombra al proveedor. | `AUTH_CAPTCHA_04` en `ApiResources.resx` | "No pudimos validar la verificación de seguridad. Recargá la página e intentá nuevamente." |
| L10 | `RecaptchaService.cs:97` | `AUTH_CAPTCHA_04` · 400 | "Error al obtener el score del captcha." | "score" es término técnico. Mismo código que L9 con otro texto. | `AUTH_CAPTCHA_04` (misma clave que L9: el texto propuesto sirve para ambos casos) | (igual a L9) |
| L11 | `RecaptchaService.cs:115` | `AUTH_CAPTCHA_05` · 400 | "La accion del captcha no coincide con la accion esperada." | Técnico ("acción esperada"). | `AUTH_CAPTCHA_05` en `ApiResources.resx` | "La verificación de seguridad no corresponde a esta operación. Recargá la página e intentá nuevamente." |
| L12 | `RecaptchaService.cs:129` | `AUTH_CAPTCHA_99` · 500 | "Error al validar captcha." | **Mismo código que `AuthController.cs:111`**, que toma otro texto del `.resx` de Authentication ("No se encontro el score de captcha validado."). Un código, dos significados. | `AUTH_CAPTCHA_99`: **una sola clave**, en `ApiResources.resx`, y que `AuthController` deje de usar la de `AuthenticationResources.resx` (o se actualice su valor al mismo texto) | "No pudimos completar la verificación de seguridad. Intentá nuevamente en unos minutos." |
| L13 | `Security/Middleware/ModelBindingErrorLoggingMiddleware.cs:205` | `MBM_DA_01` · 400 | "Solicitud denegada." | Se devuelve en **producción y preproducción** para **todo** 400 que no sea `OperationResult`, incluida la validación automática de ASP.NET. No dice qué corregir (ver §5). | `MBM_DA_01` en `ApiResources.resx` | "Algunos datos de la solicitud no son válidos. Revisá el formulario e intentá nuevamente." |
| L14 | `ModelBindingErrorLoggingMiddleware.cs:238` | `MBM_DA_02` · 400 | `Title` del `ProblemDetails` o "Errores de validación." | **`Title` en inglés** ("One or more validation errors occurred."). Solo en Development/Testing (§5). | `MBM_DA_02` en `ApiResources.resx`; el `message` deja de leer `parsed.Title` | (mismo texto que L13) |
| L15 | `Security/RequestValidation/JsonSchemaValidationFilter.cs:106` | `NOT_ACCEPTABLE` · 406 | "Accept no permite 'application/json'." | Nombre de header HTTP al usuario. | `NOT_ACCEPTABLE` en `ApiResources.resx` | "La solicitud no es válida. Actualizá la página e intentá nuevamente." |
| L16 | `JsonSchemaValidationFilter.cs:128` | `UNSUPPORTED_MEDIA_TYPE` · 415 | "Falta encabezado Content-Type." | Técnico. | `UNSUPPORTED_MEDIA_TYPE` en `ApiResources.resx` | "La solicitud no es válida. Actualizá la página e intentá nuevamente." |
| L17 | `JsonSchemaValidationFilter.cs:146` | `UNSUPPORTED_MEDIA_TYPE` · 415 | `$"Content-Type '{contentType}' no soportado. Sólo 'application/json' o 'multipart/form-data'."` | **Eco del Content-Type enviado por el cliente** y detalle técnico. | `UNSUPPORTED_MEDIA_TYPE` (misma clave que L16: mismo texto) | (igual a L16) |
| L18 | `JsonSchemaValidationFilter.cs:177` | `SCHEMA_EMPTY_BODY` · 400 | "Body vacío donde se esperaba JSON." | Técnico. | `SCHEMA_EMPTY_BODY` en `ApiResources.resx` | "Faltan datos en la solicitud. Revisá el formulario e intentá nuevamente." |
| L19 | `JsonSchemaValidationFilter.cs:198` | `SCHEMA_BAD_JSON` · 400 | "JSON inválido" (+ `data = { jex.Message }`) | Técnico; **`data` lleva el mensaje del parser** en todos los ambientes. | `SCHEMA_BAD_JSON` en `ApiResources.resx` | "La solicitud no es válida. Actualizá la página e intentá nuevamente." (y quitar `jex.Message` de `data`; ver nota 2) |
| L20 | `JsonSchemaValidationFilter.cs:219` | `SCHEMA_ERROR` · 400 | "Error validando esquema" | Técnico; además es un fallo interno devuelto como 400 (ver nota 3). | `SCHEMA_ERROR` en `ApiResources.resx` | "No pudimos validar la solicitud. Intentá nuevamente." |
| L21 | `JsonSchemaValidationFilter.cs:260` | `SCHEMA_INVALID` · 400 | "JSON no cumple esquema" (+ `data` con `Path`, `Kind`, `Property`, línea/columna y `e.ToString()`) | Técnico; **`data` expone detalle del validador** en todos los ambientes. | `SCHEMA_INVALID` en `ApiResources.resx` | "Algunos datos de la solicitud no son válidos. Revisá el formulario e intentá nuevamente." (y sanear `data`; ver nota 2) |
| L22 | `Security/Captcha/RequireCaptchaAttribute.cs` (~90, `SetFailureResult`) | `REG_CAPTCHA_05` · 400 | "Error al validar el captcha. Actividad sospechosa detectada." | **No cuenta el escaneo.** "Actividad sospechosa" le avisa a quien automatiza que fue detectado y acusa al usuario legítimo con score bajo. | `REG_CAPTCHA_05` en `ApiResources.resx` | "No pudimos validar la verificación de seguridad. Intentá nuevamente más tarde." |

### Notas sobre los literales

1. **`REG_CAPTCHA_01..04` y `REG_CAPTCHA_99` no tienen texto propio.** En modo `RequireMinimumScore` el filtro cambia el código (`MapAuthErrorToRegistrationError`) pero reenvía el `Message` de `AUTH_CAPTCHA_*`. Para que cada código tenga entrada en el catálogo (R6), propongo que el filtro resuelva `_localizer[errorCode]` y agregar `REG_CAPTCHA_01..05` y `REG_CAPTCHA_99` a `ApiResources.resx`. Equivalencias del mapeo actual: `AUTH_CAPTCHA_01→REG_CAPTCHA_01`, `02→02`, `03→03`, `04→04`, **`AUTH_CAPTCHA_05→REG_CAPTCHA_04`** (por eso `REG_CAPTCHA_04` recibe tanto los casos de `AUTH_CAPTCHA_04` como el de `05`; su texto es el de `AUTH_CAPTCHA_04`, que sirve para ambos), `99→99`. **`REG_CAPTCHA_05` no sale de ningún `AUTH_CAPTCHA_*`: es el texto propio de L22** (score por debajo del mínimo). Ningún `REG_CAPTCHA_*` está hoy en ningún `.resx`.
2. **`data` de `SCHEMA_BAD_JSON` y `SCHEMA_INVALID`** no es texto de `message`, pero sale a cualquier ambiente y es detalle interno. Recomiendo sacarlo o condicionarlo a `!IsProductionLike()` igual que `ExceptionHandlingMiddleware`. Es corrección de F3 o F5, no cambia las claves.
3. **`SCHEMA_ERROR`** (excepción inesperada al validar) responde 400 aunque es un fallo interno. Correspondería 500. Queda en FUERA DE ALCANCE.
4. **Peso real de `SCHEMA_*`**: `InMemoryJsonSchemaRegistry.LoadSchemas` registra un único esquema de ejemplo (`POST /api/datospersonales`, `{"personId":123}`). Es decir que `SCHEMA_EMPTY_BODY/BAD_JSON/INVALID` casi no se disparan hoy; sí se disparan `NOT_ACCEPTABLE` y `UNSUPPORTED_MEDIA_TYPE` (validan Accept y Content-Type en toda la API). Aun así hay que sanearlos porque el contrato dice "todo error sale curado".
5. **`AUTH_RL_01` y `AUTH_RL_05` interpolan la ventana configurada** (`{0}` = minutos de la ventana), no el tiempo restante real; el tiempo real viaja en el header `Retry-After`. Es el comportamiento actual, lo conservo; ver PREGUNTAS.

---

## 2. Tabla de indirectos (tarea 3) — 19

"Curado" = el texto que llega al usuario sale de un `.resx` o es un texto español legible sin detalle interno. El origen se siguió hasta el archivo.

| # | Archivo:línea (escaneo) | Código | Origen del texto | ¿Curado? | Acción |
| --- | --- | --- | --- | --- | --- |
| I1 | `AppLogic.Authentication/UseCases/AuthenticateWithLdap.cs:58` | `LOGIN_LDAP_02` / `LOGIN_LDAP_03` (vía `ResolveDocumentValidationCode`) | `validation.Message` de `IdentityDocumentRules.ValidateBaseDocument`: tipo inválido → `IdentityResources.resx` `SharedInvalidDocumentType` ("Tipo de documento inválido."); cédula mal formada → **`Core/Utilities/Util.ValidoCI`** | Sí, en el caso del tipo. En el de la cédula el texto es de Core: español legible, sin `.resx` ni código. | Mantener. `LOGIN_LDAP_02/03` no tienen entrada propia en ningún `.resx` (R6): decisión de catálogo para F6 (ver §6.2). |
| I2 | `AppLogic.Authentication/UseCases/CompletePasswordFlow.cs:328` | (`Rejected`) | Helper. Todos los llamadores pasan `_localizer[...]` (`ACT_SES_06`, `ACT_SES_NUP_01_Complete`, `NUP_COMP_01`, `ACT_SES_03_Complete`). | Sí | Ninguna. |
| I3 | `CompletePasswordFlow.cs:342` | (`Failed`) | Helper. Llamadores con `_localizer` (`INI_PAS_01/03/04/99`) **salvo `INI_PAS_02`**, que pasa `Util.ValidarPasswordNueva` (Core). | Parcial | Ver I12. |
| I4 | `AppLogic.Authentication/UseCases/RecoverPassword.cs:61` | `REC_PAS_02` / `REC_PAS_03` | Igual que I1 (`IdentityDocumentRules` + `Util.ValidoCI`) | Sí (misma salvedad que I1) | Mantener. |
| I5 | `AppLogic.Contracts/OperationResultExtensions.cs:31` | (varios) | `Failure()` copia el `Message` de **cualquier** resultado fallido. Origen según llamador: `.resx` de cada módulo, o **Core** en los casos de §6 (Ldap, Azure, FileValidator). | Depende del llamador | No es un punto a corregir: es el canal por el que entran los textos de Core. Los casos peligrosos están en §6. |
| I6 | `OperationResultExtensions.cs:38` | (varios) | Ídem, `As<T>()` | Depende del llamador | Ídem I5. |
| I7 | `AppLogic.Enrollments/Rules/SelectedOfferingsCompatibility.cs:50` | `INS_CPI_17` | `_localizer["INS_CPI_17_SameProduct"]` (existe en `EnrollmentsResources.resx`): "Todas las ofertas seleccionadas deben pertenecer al mismo producto (…)" | Sí (con sufijo por código compartido) | Ninguna. |
| I8 | `AppLogic.Enrollments/Survey/Services/InitialSurveyService.cs:58` | `GEN_OEI_02` | `IdentityResources` (`SharedInvalidDocumentType`) o `Util.ValidoCI` | Sí (misma salvedad que I1) | `GEN_OEI_02` sin entrada propia en `.resx`. |
| I9 | `InitialSurveyService.cs:130` | `INS_EI_55` | Ídem I8 | Sí | `INS_EI_55` sin entrada propia. |
| I10 | `AppLogic.People/UseCases/ChangePassword.cs:52` | `CAM_PAS_02` | **`Core/Utilities/Util.ValidarPassword`**: "Parámetros incorrectos", "La contraseña debe tener entre 12 y 20 caracteres", "Parámetros incorrectos. Caracteres no válidos.", "La nueva contraseña no puede ser igual a la actual." | **No del todo.** "Parámetros incorrectos" (dos veces) no dice qué corregir; el caso de caracteres no válidos no lista cuáles se permiten (`$ % @ _ ! . -`) ni que hace falta mayúscula, minúscula y número. | Ver §6.3. |
| I11 | `AppLogic.Registration/Services/RegistrationFlowService.cs:181` | `FLOW_03` | `_localizer["FLOW_03_Document"]` (existe). Texto sin tildes ("Sesion de registro invalida."). | Sí; **ortografía** | Unificar con `FLOW_03` es decisión de AF (ya anotado en el `.resx`). |
| I12 | `AppLogic.Registration/UseCases/CompleteNewPerson.cs:63` | `INI_PAS_02` | **`Core/Utilities/Util.ValidarPasswordNueva`** (mismos textos que I10, sin la regla "igual a la actual"). También lo usa `CompletePasswordFlow.cs:191`. | No del todo (igual que I10) | Ver §6.3. |
| I13 | `ConfirmRegistrationRequest.cs:61` | `REG_DOC_01` / `REG_DOC_02` | `IdentityDocumentRules` + `Util.ValidoCI` | Sí (misma salvedad que I1) | `REG_DOC_01/02` sin entrada propia. |
| I14 | `EvaluateDocument.cs:53` | `REG_DOC_01` / `REG_DOC_02` | Ídem | Sí | Ídem. |
| I15 | `ValidateNewPerson.cs:52` | `REG_DOC_01` / `REG_DOC_02` | Ídem | Sí | Ídem. |
| I16 | `VerifyIdentity.cs:83` | `REG_DOC_01` / `REG_DOC_02` | Ídem | Sí | Ídem. |
| I17 | `AppLogic.Registration/Validators/RegistrationValidation.cs:44` | `REG_TEL_01` | `localizer[REG_TEL_01, normalized.Error]` (existe): "El teléfono principal no es un celular válido. {0}". El `{0}` lo llena **`Core/Utilities/ValidadorTelefonos`**. | **No del todo.** `{0}` puede ser "Error", "---" (centinelas) o `ex.Message` del parser (líneas 306 y 357 de `ValidadorTelefonos.cs`). Mismo problema en `PER_ADP_07` (`UpdatePersonDetails.cs:~50`). | Ver §6.5. |
| I18 | `Security/Captcha/RequireCaptchaAttribute.cs:115` | `REG_CAPTCHA_*` / `AUTH_CAPTCHA_*` | Recibe el `Message` de `RecaptchaService` (L6..L12) o el literal L22. | No hoy; sí cuando se apliquen L6..L12 y L22. | Ver nota 1 de §1. |
| I19 | `Security/Middleware/ExceptionHandlingMiddleware.cs:198` | `INTERNAL_ERROR` (500), `AUTH_UNAUTHORIZED` (401), `SANITIZATION_ERROR` (409) | Literal "Error inesperado" para **los tres** códigos. | Para el 500, sin fuga (el detalle va a `data` solo fuera de producción). Para 401 y 409 el texto es **engañoso**, y además **el cuerpo lleva `httpCode: 500`** aunque el status de la respuesta sea 401 o 409: `WriteErrorResponseAsync` pasa `StatusCodes.Status500InternalServerError` fijo a `IsFailed`. Un front que lea `httpCode` del cuerpo verá 500. | Ver §3 (E1) y la fila de claves siguiente. Corregir el `httpCode` del cuerpo es parte de F3/F5. |

### Fila de claves propuestas para el literal del middleware (I19)

| Clave | Código · HTTP | Texto propuesto |
| --- | --- | --- |
| `INTERNAL_ERROR` | 500 | "Ocurrió un error inesperado. Intentá nuevamente más tarde." Con D5 (si se aprueba la opción recomendada del plan): "Ocurrió un error inesperado. Código de referencia: {0}." (`{0}` = `correlationId`, que ya existe en el middleware). |
| `AUTH_UNAUTHORIZED` | 401 | "Tu sesión no es válida o venció. Iniciá sesión nuevamente." |
| `SANITIZATION_ERROR` | 409 | "Algún dato ingresado contiene caracteres no permitidos. Revisalo e intentá nuevamente." |

El 409 se dispara cuando `SanitizeAttribute` rechaza el input (`InputSanitizationException`). Confirmar con AF que ese es el mensaje que quieren.

---

## 3. Tabla de 5xx (tarea 4) — 29 puntos

Criterio: **Apto** = se puede mostrar tal cual. **Mejorable** = no filtra nada, pero el texto es poco útil o suena a error interno; se puede mostrar, conviene reescribir. **No apto** = filtra detalle interno o usa jerga que el usuario no entiende; hay que cambiarlo **antes de F5**.

Texto leído en los `.resx` del código; ver "Texto actual".

| # | Archivo:línea | Código · HTTP | Texto actual | Veredicto | Texto propuesto |
| --- | --- | --- | --- | --- | --- |
| E1 | `ExceptionHandlingMiddleware.cs:198` | `INTERNAL_ERROR` · 500 | "Error inesperado" | Mejorable (sin código de referencia, D5) | Ver §2 I19 |
| E2 | `PasswordActivationService.cs:159` | `ACT_NUP_99` · 500 | "Error al enviar link de activación para nueva persona." | Mejorable (jerga: "link", "nueva persona") | "No pudimos enviarte el enlace para activar tu cuenta. Intentá nuevamente más tarde." |
| E3 | `PasswordActivationService.cs:236` | `flow.CodigoErrorGeneral`: `ACT_PAS_99` · 500 | "Error al enviar link de activación de contraseña." | Mejorable | "No pudimos enviarte el enlace para activar tu cuenta. Intentá nuevamente más tarde." |
| E3b | (mismo punto) | `REC_LINK_99` · 500 | "Error al enviar link de recuperación de contraseña." | Mejorable. **Inalcanzable hoy**: `RecoverPassword` descarta el resultado de `SendPasswordRecoveryMailAsync` y responde 2xx neutro (D4). | "No pudimos enviarte el enlace para recuperar tu contraseña. Intentá nuevamente más tarde." |
| E4 | `PasswordActivationService.cs:329` | `ACT_LINK_99` · 500 | "Error al activar link de contraseña." | Mejorable | "No pudimos validar el enlace. Intentá nuevamente más tarde." |
| E5 | `PasswordActivationService.cs:422` | `ACT_LINK_NUP_03` · 500 | "No se pudo recuperar los datos del registro pendiente." | Mejorable. Además es un caso de negocio (datos de registro vencidos o ausentes) servido como 500; ver FUERA DE ALCANCE. | "No pudimos recuperar los datos de tu registro. Volvé a iniciar el registro." |
| E6 | `PasswordActivationService.cs:553` | `ACT_SES_99` · 500 | "Error al validar sesión temporal." | Mejorable ("sesión temporal") | "No pudimos validar tu sesión. Intentá nuevamente desde el enlace que recibiste por correo." |
| E7 | `TwoFactorAuthService.cs:130` | `AUTH_2FA_MAIL_01` · 500 | "No fue posible enviar el código de verificación. Intentá nuevamente." | **Apto** | — |
| E8 | `TwoFactorAuthService.cs:161` | `AUTH_2FA_INIT_99` · 500 | "Error al iniciar la verificación de identidad." | Mejorable | "No pudimos iniciar la verificación de tu identidad. Intentá nuevamente más tarde." |
| E9 | `TwoFactorAuthService.cs:291` | `AUTH_2FA_99` · 500 | "Error al verificar el código." | Mejorable | "No pudimos verificar el código. Intentá nuevamente." |
| E10 | `TwoFactorAuthService.cs:385` | `AUTH_2FA_RESEND_MAIL_01` · 500 | "No fue posible reenviar el código de verificación. Intentá nuevamente." | **Apto** | — |
| E11 | `TwoFactorAuthService.cs:427` | `AUTH_2FA_RESEND_99` · 500 | "Error al reenviar el código." | Mejorable | "No pudimos reenviar el código. Intentá nuevamente." |
| E12 | `AuthenticateWithLdap.cs:114` | `LOGIN_LDAP_99` · 500 | "Error al autenticar usuario." | Mejorable | "No pudimos iniciar tu sesión. Intentá nuevamente más tarde." |
| E13 | `IssueTokensForPerson.cs:92` | `GEN_TOK_99` · 500 | "Error al generar tokens." | **No apto** (jerga: "tokens") | "No pudimos iniciar tu sesión. Intentá nuevamente." |
| E14 | `RefreshTokens.cs:91` | `REFRESH_TOKEN_99` · 500 | "Error al refrescar tokens." | **No apto** (jerga) | "No pudimos mantener tu sesión activa. Iniciá sesión nuevamente." |
| E15 | `EnrollmentMapper.cs:138` | `INS_CPI_13` · 502 | "La API interna no devolvio datos de confirmacion." | **No apto** ("API interna", sin tildes) y **el resultado es incierto**: la confirmación se mandó y no sabemos si quedó registrada; invitar a reintentar arriesga doble inscripción | "No pudimos confirmar si tu preinscripción quedó registrada. Admisiones se va a comunicar con vos." (sin "Intentá nuevamente"; ver §3.1) |
| E16 | `EnrollmentsAndPaymentsApiClient.cs:374` | `API_TIMEOUT` · 504 | "Timeout (2m)" | **No apto** (jerga y expone el límite). En llamadas no idempotentes el resultado es incierto | Genérico, solo para llamadas **de lectura**: "El servicio está demorando más de lo esperado. Intentá nuevamente en unos minutos." Para las no idempotentes: re-mapeo en el caso de uso a clave propia (§3.1) |
| E17 | `EnrollmentsAndPaymentsApiClient.cs:386` | `API_NETWORK` · 503 | "Error de red al comunicarse con el servicio." | Mejorable; incierto en llamadas no idempotentes | Lecturas: "No pudimos comunicarnos con el servicio. Intentá nuevamente en unos minutos." No idempotentes: §3.1 |
| E18 | `EnrollmentsAndPaymentsApiClient.cs:396` | `API_UNEXPECTED` · 500 | "Error inesperado al comunicarse con el servicio." | Mejorable; incierto en llamadas no idempotentes | Lecturas: "No pudimos completar la operación. Intentá nuevamente más tarde." No idempotentes: §3.1 |
| E19 | `ChangePassword.cs:74` | `CAM_PAS_99` · 500 | "Error al cambiar contraseña." | Mejorable | "No pudimos cambiar tu contraseña. Intentá nuevamente más tarde." |
| E20 | `CompleteNewPerson.cs:208` | `REG_PERSONA_99` · 500 | "Error al crear la persona." | Mejorable (jerga: "persona") | "No pudimos completar tu registro. Intentá nuevamente más tarde." |
| E21 | `CompleteNewPerson.cs:242` | `REG_PERSONA_99` · 500 | (ídem) | Mejorable | (ídem) |
| E22 | `CompleteNewPerson.cs:317` | `REG_PERSONA_99` · 500 | (ídem) | Mejorable | (ídem) |
| E23 | `ConfirmRegistrationRequest.cs:132` | `REG_SOLICITUD_99` · 500 | "Error al crear la solicitud de alta." | Mejorable | "No pudimos registrar tu solicitud. Intentá nuevamente más tarde." |
| E24 | `VerifyIdentity.cs:145` | `REG_USUARIO_99` · 500 | "No pudimos completar el registro. Intentá nuevamente más tarde." | **Apto** (ya re-mapea el error de LDAP: bien) | — |
| E25 | `VerifyIdentity.cs:169` | `REG_ADMISIONES_99` · 500 | "Error al registrar la admisión." | Mejorable | "No pudimos completar tu registro de admisión. Intentá nuevamente más tarde." |
| E26 | `AuthController.cs:111` | `AUTH_CAPTCHA_99` · 500 | "No se encontro el score de captcha validado." | **No apto** ("score", sin tildes) | Ver L12 |
| E27 | `RecaptchaService.cs:46` | `AUTH_CAPTCHA_02` · 500 | `$"No se encontro la configuracion de reCAPTCHA v3. Defini {RECAPTCHA_SECRET_KEY}."` | **No apto** (nombre de variable de entorno) | Ver L7 |
| E28 | `RecaptchaService.cs:66` | `AUTH_CAPTCHA_03` · 502 | `$"Error al validar captcha contra Google: {body}"` | **No apto** (body de Google) | Ver L8 |
| E29 | `RecaptchaService.cs:129` | `AUTH_CAPTCHA_99` · 500 | "Error al validar captcha." | Mejorable (colisiona con E26) | Ver L12 |

**Recuento**: 29 puntos (E3b es un segundo código del mismo punto de E3, por eso 29 filas + 1). Apto: 3 (E7, E10, E24). No apto: 7 (E13, E14, E15, E16, E26, E27, E28). Mejorable: el resto. Los 7 "No apto" son los que bloquean F5. E1 es "mejorable" porque no filtra nada, pero no permite cruzar el reporte con el log.

### 5xx que el escaneo no cuenta (llegan por propagación) — **todos relevantes para F5**

| Origen | Códigos · HTTP | Texto actual | Veredicto |
| --- | --- | --- | --- |
| `Core/AzureService/Services/ReconocimientoDocumentoAzureClient.cs` | `REC_DOC_02` (500) | `$"Falta configurar la variable de entorno '{EndpointEnvVar}'."` | **No apto** (nombre de variable) |
| ídem | `REC_DOC_03` (500) | `$"La variable de entorno '{EndpointEnvVar}' no contiene un URI válido."` | **No apto** |
| ídem | `REC_DOC_04` (500) | `$"Falta configurar la variable de entorno '{ApiKeyEnvVar}'."` | **No apto** |
| ídem | `REC_DOC_05` (status de Azure) | "Azure Document Intelligence rechazó el análisis con estado N. Detalle: … Endpoint: {host}. Modelo: {modelId}. ApiVersion: {…}. Ruta: {…}" | **No apto, el peor caso**: host, modelo, versión de API y ruta interna |
| ídem | `REC_DOC_06` (502) | "Azure Document Intelligence no devolvió el encabezado Operation-Location." / "…devolvió un Operation-Location inválido." | **No apto** |
| ídem | `REC_DOC_07` (502) | "…devolvió un error al consultar el estado del análisis. Detalle: …", "No fue posible interpretar la respuesta de Azure…", "…informó que el análisis falló.", "…devolvió un estado inesperado: '…'." | **No apto** |
| ídem | `REC_DOC_99` (500) | `$"Error inesperado al analizar documento: {ex.Message}"` y, en `ReconocimientoDocumento.cs:184`, `$"Ocurrió un error inesperado durante el reconocimiento del documento: {ex.Message}"` | **No apto** (`ex.Message`) |
| `Core/AzureService/Services/ReconocimientoDocumento.cs` | `REC_DOC_10` (504) | "La operación de reconocimiento agotó el tiempo máximo de espera configurado." / "Azure Document Intelligence no devolvió el resultado dentro del tiempo máximo de espera configurado." | **No apto** (nombra el proveedor) |
| `Core/LdapService/Services/Ldap.cs` | `AUTH_LDAP_99/98/97/96/95/94` (500) | `$"Error al … en LDAP: {ex.Message}"` | **No apto** (`ex.Message`); los que llegan al usuario son `97`, `95` y `94` (§6.2) |
| `AppLogic.Integrations.EnrollmentsAndPayments/Services/EnrollmentsAndPaymentsApiClient.cs:~345` | 8 códigos (`CONFIRMAR_PREINSCRIPCION_MULTIPLE_01`, `SENIA_MINIMA_01`, `ALTA_POSTULACION_BECA_01`, `CTACTE_GET_01`, `CURSOS_PAGOS_GET_01`, `CARRITOS_INSCRIPCION_GET_01`, `PAGAR_CARRITOS_01`, `URL_CREAR_FACTURA_01`) · **502** cuando el proveedor responde 5xx/3xx (`ResolveHttpCode`) | "La API rechazó …" / "Error al obtener …" (curado; el cuerpo del proveedor ya va solo al log) | Mejorable (jerga "La API"); no filtra datos. El comentario del código ya avisa que un rechazo de negocio con 5xx del proveedor pierde su razón: es la pregunta abierta para AF. |

### 3.1 Resultado incierto en llamadas no idempotentes (INS_CPI_13 y `API_*`)

`EnrollmentsAndPaymentsApiClient.HandleException` produce `API_TIMEOUT` (504), `API_NETWORK` (503) y `API_UNEXPECTED` (500) para **cualquier** llamada a la API interna. Para las lecturas, "intentá nuevamente" es correcto. Para las llamadas que cambian estado (verificadas en el código) un timeout o un corte **no dicen si el proveedor procesó la operación**, y reintentar puede duplicar la inscripción, el pago o la postulación:

| Caso de uso (llamada) | Dónde | Códigos que hoy llegan | Re-mapeo propuesto (clave · HTTP · `.resx`) | Texto propuesto (sin invitar a reintentar) |
| --- | --- | --- | --- | --- |
| `ConfirmPreEnrollment` → `ConfirmMultiplePreEnrollmentAsync` | `ConfirmPreEnrollment.cs:155` | `API_TIMEOUT/NETWORK/UNEXPECTED`, 5xx del proveedor como `CONFIRMAR_PREINSCRIPCION_MULTIPLE_01` (502), y `INS_CPI_13` | `INS_CPI_13` (se mantiene el código) y `INS_CPI_18` nueva (502, `EnrollmentsResources.resx`) para los errores de transporte y 5xx | "No pudimos confirmar si tu preinscripción quedó registrada. Admisiones se va a comunicar con vos." (ambas) |
| `PayWithPersonalAccount` → `PayCartsByEnrollmentAsync` | `PayWithPersonalAccount.cs:59` | `API_*`, 5xx como `PAGAR_CARRITOS_01` | `INS_PC_03` nueva (502, `EnrollmentsResources.resx`) | "No pudimos confirmar si el pago se realizó. Antes de volver a pagar, revisá tu cuenta corriente o consultá con Admisiones." |
| `CreateApplicationAsync` → `CreateScholarshipApplicationAsync` | `ScholarshipService.cs:~120` | `API_*`, 5xx como `ALTA_POSTULACION_BECA_01` | `BEC_PO_03` nueva (502, `ScholarshipsResources.resx`) | "No pudimos confirmar si tu postulación quedó registrada. Admisiones se va a comunicar con vos." |

Regla: el caso de uso re-mapea solo cuando `!Success` y el error es de transporte o 5xx (`HttpCode >= 500` o `ErrorCode` en `API_TIMEOUT/API_NETWORK/API_UNEXPECTED`); los 4xx del proveedor (rechazo de negocio) siguen pasando con su texto. `GetCreateInvoiceUrlByEnrollmentAsync` es de generación de URL (idempotente en la práctica) y puede quedar con el genérico de lectura. **Pedido a AF**: confirmar que existe el proceso "Admisiones se comunica"; si no, "Comunicate con Admisiones".

### 3.2 Textos 4xx que YA están en los `.resx` y que hoy el usuario ve (revisión con R2)

Barrido de los 319 valores de los 8 `.resx` buscando nombres técnicos ("token", "request", "FlowId", "base de datos", "API", "header", "Id…", nombres de método) y textos sin tildes o "invalido". HTTP leído en el código. Todas las claves de módulo salvo indicación.

**Sesión y tokens (se ven en cualquier pantalla cuando falla el refresh: `http.ts:97-100` re-lanza)**

| Clave (Authentication) | HTTP | Texto actual | Problema | Texto propuesto |
| --- | --- | --- | --- | --- |
| `REFRESH_TOKEN_01` | 401 | "No se encontró refresh token en las cookies." | Jerga ("refresh token", "cookies") | "Tu sesión venció. Iniciá sesión nuevamente." |
| `REFRESH_TOKEN_03` | 401 | "Refresh token inválido o expirado." | Jerga | "Tu sesión venció. Iniciá sesión nuevamente." |
| `REFRESH_TOKEN_04` | 404 | "Usuario no encontrado en la base de datos." | Detalle interno; además es un 404 por una sesión que ya no sirve | "Tu sesión ya no es válida. Iniciá sesión nuevamente." |
| `AUTH_RT_01` (`AuthController.cs:386`) | 401 | "Refresh token no encontrado en las cookies." | Jerga | "Tu sesión venció. Iniciá sesión nuevamente." |
| `GEN_TOK_01` | 404 | "Usuario no encontrado en la base de datos." | Detalle interno | "No pudimos iniciar tu sesión. Intentá nuevamente o comunicate con Admisiones." |
| `INI_PAS_03` | 404 | "Usuario no encontrado en la base de datos." | Detalle interno; se ve en la pantalla de crear contraseña | "No encontramos tu usuario. Volvé a abrir el enlace que recibiste por correo o comunicate con Admisiones." |

**Enlaces de activación y recuperación (se ven en set-password)**

| Clave (Authentication) | HTTP | Texto actual | Problema | Texto propuesto |
| --- | --- | --- | --- | --- |
| `ACT_LINK_01` | 400 | "El token es obligatorio." | Jerga | "El enlace no es válido. Volvé a abrirlo desde el correo que recibiste." |
| `ACT_LINK_02` | 401 | "El token no corresponde al flujo de activación de contraseña." | Jerga | "Este enlace no sirve para activar la contraseña. Solicitá uno nuevo." |
| `ACT_LINK_03` | 401 | "El token no contiene una persona válida." | Jerga | "El enlace no es válido. Solicitá uno nuevo." |
| `ACT_LINK_NUP_01` | 401 | "El token no contiene un FlowId válido." | Jerga ("FlowId") | "El enlace no es válido. Solicitá uno nuevo." |
| `ACT_LINK_NUP_02` | 401 | "El link de activación es inválido, ya fue utilizado o expiró." | "link" | "El enlace de activación no es válido, ya se usó o venció. Solicitá uno nuevo." |
| `ACT_SES_06`, `ACT_SES_03_Complete`, `ACT_SES_NUP_01`, `ACT_SES_NUP_01_Complete` | 401 | "Sesion temporal invalida." / "Sesion temporal sin persona valida." / "Sesión temporal sin FlowId válido." | Sin tildes; "FlowId"; "sesión temporal" es jerga. Los cuatro casos nacen del mismo punto: la sesión que se abre al usar el enlace de activación falta, venció o no tiene persona o flujo (`CompletePasswordFlow.cs:95,114,164`), así que "el enlace" aplica a todos | "El enlace que usaste no es válido o venció. Volvé a abrirlo desde el correo que recibiste." (los cuatro; ya hay decisión de AF pendiente sobre las variantes con y sin tilde) |
| `ACT_PAS_03`, `REC_LINK_03` | — | "No se encontró la persona para guardar el token de …" | Jerga | "No encontramos tu usuario. Solicitá un nuevo enlace." (`REC_LINK_03` hoy no llega al usuario: `RecoverPassword` descarta el resultado) |

**Registro**

| Clave (Registration) | HTTP | Texto actual | Problema | Texto propuesto |
| --- | --- | --- | --- | --- |
| `REG_DOC_03_ConfirmRequest` | 400 | "ConfirmRegistrationRequest solo aplica para documentos distintos a cédula de identidad." | Nombra el método | "Esta opción solo aplica para documentos distintos de la cédula de identidad." |
| `REG_DOC_03_NewPerson` | 400 | "ConfirmNewPerson solo aplica para cédula de identidad." | Nombra el método | "Esta opción solo aplica para cédula de identidad." |
| `REG_DOC_03_VerifyIdentity` | 400 | "VerifyIdentity solo aplica para cédula de identidad." | Nombra el método | "Esta opción solo aplica para cédula de identidad." |
| `FLOW_01` | 400 | "El header X-Flow-Id es obligatorio." | Jerga HTTP | "Tu sesión de registro no es válida. Volvé a empezar el registro." |
| `FLOW_03`, `FLOW_03_Document` | 400 | "Sesión de registro inválida." / "Sesion de registro invalida." | Variante sin tildes | "Tu sesión de registro no es válida o venció. Volvé a empezar el registro." |
| `FLOW_05` | 400 | "El documento del request no coincide con la sesion de registro." | Jerga ("request"), sin tildes | "El documento no coincide con el de tu registro en curso. Volvé a empezar el registro." |
| `REG_TEL_03` (también `PER_ADP_08`, People) | 400 | "No se pudo identificar la característica del país del teléfono." | "característica" es jerga interna | "No pudimos identificar el país del teléfono. Revisalo e intentá nuevamente." |

**Enrollments: "Request invalido" y afines** (los de pagos están en §9.3)

| Clave (Enrollments) | HTTP | Texto actual | Texto propuesto |
| --- | --- | --- | --- |
| `GEN_IP_00`, `INS_CPI_01`, `INS_EI_02` | 400 | "Request invalido." | "La solicitud no es válida. Revisá los datos e intentá nuevamente." |
| `INS_EI_03` | 400 | "El producto indicado es invalido." | "El producto indicado no es válido." |
| `INS_EI_05` | 400 | "Proceso invalido." | "El proceso de admisión indicado no es válido." |
| `INS_EI_06`, `INS_EI_14` | 400 | "Ultimo anio de bachillerato invalido." / "…de secundaria invalido." | "El último año de bachillerato indicado no es válido." / "El último año de secundaria indicado no es válido." |
| `INS_EI_11` | 400 | "Con quien compartio la decision invalido." | "La persona con quien compartiste la decisión no es válida." |
| `INS_EI_15` | 400 | "Nivel de decision invalido." | "El nivel de decisión indicado no es válido." |
| `INS_EI_21`, `INS_EI_22` | 400 | "Titulo invalido." / "El titulo indicado es invalido." | "El título indicado no es válido." |
| `INS_EI_23` | 400 | "Motivo de eleccion invalido." | "El motivo de elección indicado no es válido." |

**Otros**

| Clave | HTTP | Texto actual | Problema | Texto propuesto |
| --- | --- | --- | --- | --- |
| `GEN_SDA_04` (People) | 400 | "Debe enviar el nombre del archivo del documento de identidad ({0})." | Jerga de API | "El archivo del documento de identidad ({0}) no tiene nombre. Volvé a subirlo." |
| `AUTH_CAPTCHA_99` (Authentication; `AuthController.cs:111`) | 500 | "No se encontro el score de captcha validado." | "score" | Ver L12 |

**Familia "La API rechazó…" (`EnrollmentsAndPaymentsResources.resx`).** Es un rechazo de negocio del proveedor (4xx pasan tal cual; los 5xx se re-mapean en §3.1). Hoy dicen "La API":

| Clave | Texto actual | Texto propuesto |
| --- | --- | --- |
| `CONFIRMAR_PREINSCRIPCION_MULTIPLE_01` | "La API rechazó la confirmación múltiple." | "No pudimos confirmar tu preinscripción. Comunicate con Admisiones para continuar." (es un rechazo de negocio del proveedor, `EnrollmentsAndPaymentsApiClient.cs:355-357`: no hay datos que corregir y un reintento puede entrar en bucle) |
| `ALTA_POSTULACION_BECA_01` | "La API rechazó el alta de postulación a beca." | "No pudimos registrar tu postulación a la beca. Comunicate con Admisiones para continuar." |
| `PAGAR_CARRITOS_01` | "La API rechazó el pago." | "No pudimos procesar el pago. Revisá tu cuenta corriente o comunicate con Admisiones." |
| `URL_CREAR_FACTURA_01` | "La API rechazó la creación de URL de factura." | "No pudimos generar el enlace de pago. Intentá nuevamente más tarde." |
| `SENIA_MINIMA_01`, `CTACTE_GET_01`, `CURSOS_PAGOS_GET_01`, `CARRITOS_INSCRIPCION_GET_01` (mismo problema menor: "Error al obtener …") | "Error al obtener la seña mínima." / "…cuenta corriente." / "…cursos a pagar." / "…carritos de la inscripción." | "No pudimos obtener la seña mínima. Intentá nuevamente más tarde." / "…tu cuenta corriente…" / "…tus cursos a pagar…" / "…el detalle de tu inscripción…" |

---

## 4. Contrato frente a F5

Antes de abrir los 5xx (R1) hay que sanear: `AUTH_CAPTCHA_02/03/99` (E26–E28), `GEN_TOK_99`, `REFRESH_TOKEN_99`, `INS_CPI_13`, `API_TIMEOUT`, y **los `REC_DOC_02..07`, `REC_DOC_10` y `REC_DOC_99` de `Core/AzureService`** (ninguno de esos 5xx es apto) junto con los `AUTH_LDAP_9x` de `Core/LdapService`. Azure y LDAP se resuelven sin tocar `Core` si el llamador (controller o caso de uso) re-mapea por `ErrorCode` a un `.resx` y deja el detalle en el log (ver §6).

---

## 5. Validación automática de ASP.NET (`ProblemDetails` de model binding) — tarea 5

**Qué hay**: los controllers llevan `[ApiController]` y `AddControllers` no configura `ApiBehaviorOptions` (no hay `InvalidModelStateResponseFactory`, ni `AddProblemDetails`, ni `UseStatusCodePages`). Por eso ASP.NET responde 400 con `ValidationProblemDetails` estándar (`title: "One or more validation errors occurred."`, `errors: { campo: [mensajes] }`). El middleware `ModelBindingErrorLoggingMiddleware` intercepta **todo 400** y lo transforma según el ambiente:

| Ambiente | Qué le llega al front | Detalle |
| --- | --- | --- |
| **Production / Preproduction** (`IsProductionLike`) | `OperationResult` con `errorCode = MBM_DA_01`, `message = "Solicitud denegada."`, `data = null`, 400 | Si el 400 ya era un `OperationResult` (los de negocio), se deja pasar. **Cualquier otro** 400 (binder, JSON mal formado, tipos que no convierten, campo requerido faltante) se enmascara con este único texto: el usuario no sabe qué corregir. |
| **Development / Testing** | `OperationResult` con `errorCode = MBM_DA_02`, `message = Title` (inglés), `data = errors` (diccionario campo → mensajes) | Solo si el cuerpo tiene `errors` (ProblemDetails de validación); si no, pasa tal cual. |

**Textos de los `errors` (DataAnnotations)**: de ~2630 atributos de validación solo 13 tienen `ErrorMessage` propio en español (`AuthRequests.cs`, `ChangePasswordRequest.cs`). Los DTOs de `RegistrationRequests.cs`, `PhoneNumber.cs` y `UploadRequests.cs` usan `[Required]`, `[EmailAddress]`, `[Range]`, `[RegularExpression]`, `[StringLength]` **sin mensaje**, así que el binder responde con el default en **inglés** ("The Documento field is required."). Los atributos de las entidades `DevartEntities` no entran en el binding de requests.

**Conclusión**:

1. En producción el front hoy no recibe ningún mensaje accionable para estos 400: recibe "Solicitud denegada." (L13). Es coherente con "no filtrar estructura interna" pero pierde el motivo.
2. En desarrollo/testing el `message` es el `Title` en inglés (L14) y los `errors` en inglés. Eso es lo que verían AF y QA en esos ambientes.
3. Propuesta mínima (sin decisión nueva): L13/L14 pasan a un mismo texto del `.resx` ("Algunos datos de la solicitud no son válidos. Revisá el formulario e intentá nuevamente."). Los `errors` por campo (inglés) no se muestran: la validación previa al envío es del front (R5).
4. **Decisión que no me corresponde tomar** (PREGUNTAS): si se quiere que producción también diga *qué campo* está mal, haría falta mapear `errors` a mensajes en español por campo (`ErrorMessage` con `.resx` en los DTOs, o un `InvalidModelStateResponseFactory` propio). Es contrato nuevo, no solo texto.
5. **Otras respuestas sin cuerpo** (no pasan por `OperationResult`): 401 y 403 de `JwtBearer`/`[Authorize]` (sin `OnChallenge` ni `OnForbidden`; solo se configura `OnAuthenticationFailed` para loguear), 404/405 de ruteo, 406 de `ReturnHttpNotAcceptable = true` cuando el filtro no lo atrapa antes, 413 de Kestrel (`MaxRequestBodySize`) y el 429 del rate limiter de cualquier política que no sea una de las cuatro con handler. Ahí el front no tiene `message` que mostrar. Son candidatas a contrato (tarea 7).

---

## 6. Textos que vienen de `Core/` (tarea 6)

`Core` no se toca desde esta auditoría. Para cada caso: si se puede resolver **sin** tocar Core (re-mapeo en el llamador por `ErrorCode`, mensaje original al log) y, si no, el pedido aparte.

### 6.1 `Core/AzureService` (reconocimiento de documentos) — llega a `message` hoy

Único consumidor: `RegistrationController.AnalyzeAttachment` (`POST analyze-attachment`, anónimo + captcha ScoreOnly), que hace `result.Failure().As<DocumentRecognitionResponse>(...)` y devuelve el `Message` de Core tal cual. `REC_DOC_01` ya está en `RegistrationResources.resx`; el resto no.

| Código · HTTP | Origen en Core | Texto actual (resumen) | Veredicto | Texto propuesto (clave en `RegistrationResources.resx`) |
| --- | --- | --- | --- | --- |
| `REC_DOC_01` · 400 | `ReconocimientoDocumento.cs:195` | "No se recibió la solicitud de reconocimiento." | Apto (el controller ya usa el del `.resx`) | — |
| `REC_DOC_02/03/04` · 500 | `…AzureClient.cs:48,57,66` | variable de entorno … | **No apto** | "El reconocimiento de documentos no está disponible en este momento. Intentá nuevamente más tarde." |
| `REC_DOC_05` · status de Azure | `…AzureClient.cs:182,190` | rechazo con host, modelo, versión, ruta | **No apto** | (igual a `REC_DOC_02`) |
| `REC_DOC_06/07` · 502 | `…AzureClient.cs:155,164,233…` | "Azure Document Intelligence …" | **No apto** | (igual a `REC_DOC_02`) |
| `REC_DOC_08` · 422 | `ReconocimientoDocumento.cs:155` | "Azure Document Intelligence no devolvió texto reconocible para el archivo enviado." | **No apto** (proveedor) | "No pudimos leer texto en el archivo. Subí una imagen o un PDF más nítido del documento." |
| `REC_DOC_10` · 504 | `ReconocimientoDocumento.cs:175`; `…AzureClient.cs:283` | "…agotó el tiempo máximo de espera configurado." | **No apto** | "El reconocimiento del documento está demorando más de lo esperado. Intentá nuevamente." |
| `REC_DOC_11` · 422 | `ReconocimientoDocumento.cs:101` | "El tipo de documento detectado no está admitido por este servicio." | Mejorable | "El tipo de documento no está admitido. Subí un documento de identidad válido." (confirmar con AF cuáles se admiten) |
| `REC_DOC_12` · 422 | `ReconocimientoDocumento.cs:106,166` | "No se pudo detectar un tipo de documento admitido a partir de la imagen enviada." | Mejorable | "No pudimos reconocer el documento en la imagen. Subí una foto nítida del documento completo." |
| `REC_DOC_13` · 400 | `ReconocimientoDocumento.cs:206` | `$"El archivo excede el tamaño máximo permitido para reconocimiento de documentos ({N} MB)."` | Mejorable (el número es útil, pero interpolado en Core) | "El archivo es demasiado grande. Subí uno más liviano." |
| `REC_DOC_16` · 422 | `ReconocimientoDocumento.cs:133` | "El documento fue detectado, pero los datos extraídos no cumplen el mínimo de confiabilidad requerido." | Mejorable ("confiabilidad") | "Reconocimos el documento, pero no pudimos leer sus datos con claridad. Subí una foto más nítida." |
| `REC_DOC_17` · 422 | `ReconocimientoDocumento.cs:254` | "La imagen no tiene calidad suficiente para reconocer el documento." | **Apto** | "La imagen no tiene calidad suficiente. Subí una foto más nítida, sin reflejos ni sombras." (opcional) |
| `REC_DOC_99` · 500 | `ReconocimientoDocumento.cs:184`; `…AzureClient.cs:120` | `…: {ex.Message}` | **No apto** | "No pudimos procesar el documento. Intentá nuevamente más tarde." |

**Cómo resolverlo sin tocar Core**: en `AnalyzeAttachment`, si `!result.Success`, resolver `_localizer[result.ErrorCode]` (con respaldo `REC_DOC_99`) y loguear el `Message` original. **Pedido aparte a Core (opcional)**: que Azure devuelva solo código y deje el texto al llamador, y que los textos con `{ex.Message}`/host/modelo no viajen en `Message` (hoy Core mezcla detalle técnico y mensaje de usuario en el mismo campo).

### 6.2 `Core/LdapService/Services/Ldap.cs`

Los textos de Core llegan a `message` en tres casos de uso; el resto se descarta o no se usa:

| Código · HTTP | Texto actual | ¿Llega al usuario? | Veredicto |
| --- | --- | --- | --- |
| `AUTH_LDAP_01/02/03/99` (autenticar) | "No se encontró la configuración de la ruta del servicio LDAP.", "…parámetros del sistema.", "Credenciales inválidas.", `…{ex.Message}` | **No**: `AuthenticateWithLdap` descarta el resultado y devuelve `InvalidCredentials()` (anti-enumeración, correcto) | n/a |
| `AUTH_LDAP_20/21` · 400, `AUTH_LDAP_22` · 400, `AUTH_LDAP_97` · 500 (cambiar contraseña) | "No se encontro la configuracion de la ruta del servicio LDAP." / "…parametros del sistema." / "El servicio LDAP no pudo cambiar la password." / `…{ex.Message}` | **Sí**: `ChangePassword.cs:~65` (`passwordChange.Failure().As<object>`) | **No apto** (config interna, "LDAP", `ex.Message`, sin tildes) |
| `AUTH_LDAP_50/51` · 400, `AUTH_LDAP_52` · 400, `AUTH_LDAP_94` · 500 (forzar cambio) | "…ruta del servicio LDAP.", "…parametros del sistema.", "El servicio LDAP no pudo forzar el cambio de password.", `…{ex.Message}` | **Sí**: `CompletePasswordFlow.cs:~232`, `CompleteNewPerson.cs:~142,~280` | **No apto** |
| `AUTH_LDAP_40/41` · 400, `AUTH_LDAP_42` · 400, `AUTH_LDAP_95` · 500 (crear usuario) | "…ruta…", "…parametros…", "El servicio LDAP no pudo crear el usuario.", `…{ex.Message}` | **Sí** en `CompleteNewPerson.cs:~135` (en `VerifyIdentity` se re-mapea a `REG_USUARIO_99`: correcto) | **No apto** |
| `AUTH_LDAP_10/11/98` (enviar contraseña), `AUTH_LDAP_30/31/32/96` (generar) | ídem; `11` incluye el body del servicio interno | **No**: sin llamadores en `AppLogic` | n/a (código muerto desde la API) |

Aparte: `AUTH_LDAP_03` existe en `AuthenticationResources.resx` pero `AuthenticateWithLdap` no lo emite (descarta el resultado); es clave huérfana.

**Cómo resolverlo sin tocar Core**: en los tres casos de uso, cuando LDAP falla, devolver una clave propia por caso (código a definir por el módulo dueño), con el detalle de Core solo en el log. Propuesta de claves y textos:

| Caso de uso | Clave propuesta | Texto propuesto |
| --- | --- | --- |
| `ChangePassword` (LDAP rechaza, 400) | `CAM_PAS_03` | "No pudimos cambiar tu contraseña. Verificá que la contraseña actual sea correcta e intentá nuevamente." |
| `ChangePassword` (excepción de LDAP, 500) | `CAM_PAS_99` (ya existe) | (ver E19) |
| `CompletePasswordFlow` y `CompleteNewPerson` (forzar contraseña) | `INI_PAS_05` | "No pudimos guardar tu contraseña. Intentá nuevamente más tarde." |
| `CompleteNewPerson` (crear usuario) | `REG_PERSONA_98` (nueva) | "No pudimos completar tu registro. Intentá nuevamente más tarde." |

**Pedido aparte a Core**: que `Ldap.cs` no incluya `ex.Message` en el `Message` de los `*_9x`, y que distinga "credencial actual incorrecta" de "fallo del servicio" (hoy `AUTH_LDAP_22` es 400 para ambos); sin eso el texto de `CAM_PAS_03` es una suposición que AF/ORT debe confirmar.

### 6.3 `Core/Utilities/Util.cs` — contraseñas

`ValidarPasswordNueva` y `ValidarPassword` devuelven `string` (vacío = válido), sin código. Llegan a `message` por `CAM_PAS_02` (`ChangePassword.cs:52`), `INI_PAS_02` (`CompleteNewPerson.cs:63`, `CompletePasswordFlow.cs:191`).

| Texto de Core | Caso | Veredicto |
| --- | --- | --- |
| "Parámetros incorrectos" | contraseña (actual o nueva) vacía | **No apto** (no dice qué corregir) |
| "La contraseña debe tener entre 12 y 20 caracteres" | largo | Apto |
| "Parámetros incorrectos. Caracteres no válidos." | falta mayúscula/minúscula/número/especial, o carácter fuera del conjunto | **No apto**: no dice qué se pide |
| "La nueva contraseña no puede ser igual a la actual." | igual | Apto |

**Sin tocar Core**: en el caso de uso, no reenviar el texto de Core. Propuesta: `INI_PAS_02` y `CAM_PAS_02` con "La contraseña debe tener entre 12 y 20 caracteres, con al menos una mayúscula, una minúscula, un número y un símbolo ($ % @ _ ! . -). No se permiten otros caracteres.", y en `ChangePassword` la verificación "igual a la actual" hecha antes de llamar a Core (clave `CAM_PAS_02_SameAsCurrent`: "La nueva contraseña no puede ser igual a la actual."). Los casos de contraseña vacía ya están cubiertos por los `[Required]` de los DTOs.

### 6.4 `Core/Utilities/Util.ValidoCI` — cédula

Textos (8): "El número de documento no puede estar vacío.", "El número de documento de la persona debe ser numérico (sin puntos) seguido de un guión (-) y el dígito verificador.", "La cédula solo puede contener números y un guión.", "El número de cédula no puede tener más de 7 dígitos antes del guión.", "El dígito verificador no es correcto.", "Error al validar la cédula.". **Veredicto: aptos** (español legible, sin detalle interno). Salvedades: "de la persona" suena a voz de tercera persona; "Error al validar la cédula." no orienta. Sin pedido.

### 6.5 `Core/Utilities/ValidadorTelefonos.cs` — detalle de `REG_TEL_01` y `PER_ADP_07`

`PhoneVerification.Error` puede valer: textos legibles ("El número de celular solo puede contener caracteres numéricos del 0 al 9.", "…no puede comenzar con 0.", "El número no es válido o no corresponde a una línea móvil.", "Formato inválido: doble '+' al inicio."), pero también **centinelas ("Error", "---")** y **`ex.Message`** (líneas 306 y 357). **Sin tocar Core**: en los dos llamadores (`RegistrationValidation.cs:44`, `UpdatePersonDetails.cs`), pasar `{0}` solo si el detalle es uno de los textos legibles conocidos, o quitar `{0}` del `.resx` y dejar el mensaje fijo. **Pedido aparte a Core**: no usar `ex.Message` ni centinelas en `Error`.

### 6.6 `Core/Utilities/FileValidator.cs` (`FILE_VAL_01/02/06/07`)

Textos: "El archivo está vacío o no se pudo leer.", "El nombre del archivo es inválido.", `…Solo se permiten: {extensiones}`, `…Tamaño actual: X MB, máximo: Y MB`. **Aptos** (informan algo accionable, sin detalle interno). Llegan por `.Failure().As<>` desde Identity, People y Scholarships. Salvedad de catálogo (R6): están en Core, no en un `.resx`. F6 deberá leerlos de Core o duplicarlos; **pedido aparte**, no urgente.

### 6.7 Core que no llega a `message`

- `Core/Modules/ModBandeja/.../BandejaService.cs` (`"404"`, "Instancia no encontrada"): no alcanzable; `ConfirmCorporatePreEnrollment` solo usa `AltaTramiteWorkflow`, que no emite `IsFailed`.
- `Core/Modules/ModGenericBase/**` (~35 servicios `*AppService` con `IsFailed`): el host solo referencia su `DataAccess`; ningún caso de uso de `AppLogic` los invoca.
- `Core/MailORT`: sin `IsFailed`.

---

## 7. D4 — Anti-enumeración de recuperar contraseña (opción recomendada)

**Propuesta (la del plan)**: el backend responde 2xx con un mensaje neutro, en `.resx`, para cualquier documento. **Ya está implementada en esta base**: `RecoverPassword.cs` devuelve `OperationResult.IsSuccess(null, …, _localizer["OkPasswordRecoveryRequested"])` cuando la persona no existe, cuando los datos no coinciden y cuando hay excepción. Texto actual: "Si los datos ingresados son correctos, recibirás un mail con instrucciones para recuperar tu contraseña." (apto).

**Confirmado en la tarea 7** (§9.4): los 4xx que quedan en `recover-password` no dependen de que la cuenta exista; FE-AUTH-006 no requiere trabajo de back.

Dos cosas para el cierre de D4, no bloqueantes:
- **Seguirá habiendo 400** `REC_PAS_01/02/03` (solicitud vacía, tipo de documento inválido, cédula mal formada). Eso valida formato, no existencia: no enumera cuentas. Confirmar con quien lleva seguridad (ASVS) que alcanza.
- **El 5xx de envío de mail (`REC_LINK_99`) queda inalcanzable** por diseño. Si falla el envío, el usuario ve el mensaje neutro y no se entera. Es el tradeoff estándar de la anti-enumeración; solo hay que dejarlo escrito. Alcance del front: FE-AUTH-006 se resuelve con este comportamiento (el front ya no necesita ocultar el 404/400).

---

## 8. Claves sin entrada en `.resx` (derivado de las tablas; no depende de F1)

| Código | Dónde se emite | Qué hace falta |
| --- | --- | --- |
| `AUTH_RL_01`, `AUTH_RL_05`, `REC_DOC_15`, `PERS_RL_01`, `AUTH_CAPTCHA_01..05`, `MBM_DA_01`, `MBM_DA_02`, `NOT_ACCEPTABLE`, `UNSUPPORTED_MEDIA_TYPE`, `SCHEMA_*`, `INTERNAL_ERROR`, `AUTH_UNAUTHORIZED`, `SANITIZATION_ERROR` | host | `ApiResources.resx` nuevo (L2..L22 y §2) |
| `REG_CAPTCHA_01..05`, `REG_CAPTCHA_99` | filtro de captcha | `ApiResources.resx` |
| `FDB_ADJ_01` | `ScholarshipsController` | `ScholarshipsResources.resx` |
| `LOGIN_LDAP_02/03`, `REC_PAS_02/03`, `REG_DOC_01/02`, `GEN_OEI_02`, `INS_EI_55`, `CAM_PAS_02`, `INI_PAS_02` | casos de uso que propagan texto de Identity o Core | Decidir en F6 si el catálogo toma el texto de `IdentityResources` (clave `SharedInvalidDocumentType`) o si cada código tiene clave propia que el caso de uso usa. Recomendación: clave propia y texto desde el `.resx` del módulo. |
| `INS_UF_05`, `INS_CPI_18`, `INS_PC_03` | nuevos: URL de pago inválida (§9.2) y resultado incierto en confirmar preinscripción y pagar con cuenta personal (§3.1) | `EnrollmentsResources.resx` |
| `BEC_PO_03` | nuevo: resultado incierto al crear la postulación a beca (§3.1) | `ScholarshipsResources.resx` |
| `AUTH_LDAP_03` | `AuthenticationResources.resx` | Clave huérfana (no se emite). Revisar en F3. |
| `REC_DOC_02..17` | `Core/AzureService` | `RegistrationResources.resx` (ver §6.1) |
| `CAM_PAS_03`, `INI_PAS_05`, `REG_PERSONA_98`, `CAM_PAS_02_SameAsCurrent` | nuevos (propuestos §6.2, §6.3) | `PeopleResources.resx`, `AuthenticationResources.resx`, `RegistrationResources.resx` |

## 9. Contratos faltantes (tarea 7) — cruce con `CONTRATO-BACK` de F1

Fuentes: `F1a-auth-home.md` (6), `F1b-enrollments-catalogs.md` (5 + 3 dependencias), `F1c-scholarships-shared.md` (lista vacía + endpoints a verificar). Verificado leyendo el código de la base `gabriele/feat/getScolarship`. "Cubre" = el back ya cumple y el trabajo es solo del front.

### 9.1 Resumen

| id (F1) | Endpoint | ¿El back lo cubre? | Trabajo de back |
| --- | --- | --- | --- |
| FE-AUTH-006 | `POST auth/recover-password` | **Sí** (D4 ya implementada) | Ninguno |
| FE-AUTH-028 | `POST registration/verify-identity`, `confirm-new-person`, `confirm-registration-request` | **Parcial**: `verify-identity` sí; `confirm-new-person` no (texto solo dentro de `data`); `confirm-registration-request` es otro caso (ver 9.4) | 1 cambio en `RegistrationFlowService` |
| FE-AUTH-018 | `POST registration/evaluate-document` | **Sí**. F1 lo reclasifica: ya no es contrato de back | Ninguno |
| FE-AUTH-019 | `POST registration/evaluate-document` | **Sí** (flowId siempre, o 500 curado). Reclasificado por F1 | Ninguno |
| FE-R5-010 | `POST person/validate-phone-number` | **No** (200 con `data:false`, sin motivo). Reclasificado por F1: es R5 (validación de formato), no contrato de back | Ninguno si es R5; ver 9.4 |
| FE-HOME-009 | `PUT person/details` | **Sí**. Reclasificado por F1 | Ninguno |
| FE-ENR-012 | `POST person/identity-document`, `POST person/photo` | **Sí**. Reclasificado por F1 | Ninguno |
| FE-ENR-003 / FE-ENR-004 | `POST enrollments/start-payment` (Banred/Geopay/Sistarbanc) | **No**: no valida URL ni parámetros cifrados | Un solo cambio, `INS_UF_05` (9.2) |
| FE-ENR-005 | `confirm-pre-enrollment`, `reactivate`, `details`, `start-payment` | **Sí** (el id es nulo solo en "En proceso", que no paga; `start-payment` rechaza ids ajenos con 404). Reclasificado por F1 | Solo textos (9.3) |
| FE-R5-209 | `POST enrollments/start-payment` | **Parcial**: `result` ya es un conjunto estable (`PAGO_CONFIRMADO`, `METODO_GUARDADO`, `URL_GENERADA`) pero es `string` libre en OpenAPI; no existe valor "en proceso" | Tiparlo como enum |
| FE-R5-202 (dep. F1b) | `GET catalogs/banks` | **Sí, como estado vacío**: devuelve 200 con lista vacía y eso es un estado, no un error (404 sería "ausencia" donde no la hay). El front lo muestra como estado vacío R5 | Solo corregir el XML-doc del controller (400/500/502 no coinciden con el código) |
| FE-ENR-013 (dep. F1b) | `POST enrollments/confirm-pre-enrollment` 409 | **Sí** (curado), con textos a corregir | Solo textos |
| `StartPaymentResponse.Messages` (F1b) | `POST enrollments/start-payment` (cuenta personal) | **No** (texto del proveedor sin curar) | Mapear claves o aceptar pass-through |
| F1c uploads/deletes | `scholarships/*` | **Sí** | Ninguno |

### 9.2 Contratos que sí hacen falta (endpoint, caso, código, texto)

| Endpoint | Caso | Código · HTTP propuesto | Clave y resx | Texto propuesto |
| --- | --- | --- | --- | --- |
| `POST registration/confirm-new-person` (FE-AUTH-028) | Registro guardado pero el mail de activación no salió. Hoy: 200 con `data.mailSent=false`, `data.message` con el texto, y **`message` del envelope vacío** (`RegistrationFlowService.cs:~311` usa `OperationResult.Ok(data, method)`, que no setea `Message`). | `OkRegisteredMailNotSent` (éxito 200, sin `errorCode`) | ya existe en `RegistrationResources.resx` | "Tu registro quedó realizado, pero no se envió el mail. Reintentá más tarde desde la opción de recuperación de contraseña." Cambio de back: `IsSuccess(data, method, _localizer["OkRegisteredMailNotSent"])` en esa rama (y el `message` de éxito en la rama normal, ~línea 330, por consistencia). |
| `POST enrollments/start-payment` (FE-ENR-003 y FE-ENR-004) | El proveedor responde 2xx pero la URL de pago no sirve: (1) `Data` nulo o URL vacía (hoy `urlResult.Data!` sin validar: cadena vacía sale 200 con `paymentUrl=""`, `null` sale como NRE y 500 `INTERNAL_ERROR`); (2) **`EncryptedParameters == null`**: `InvoicePaymentUrl.Split` lo devuelve nulo cuando falta el marcador `parametrosEncriptados=` y hoy sale 200 (el front lo termina mostrando como `gatewayMissingData`); (3) URL que no es http(s) absoluta. Validar en `GenerateInvoicePaymentUrl.cs:~104`, tras `Split`. | `INS_UF_05` · 502 (cubre los tres casos; el usuario hace lo mismo en todos: reintentar más tarde o elegir otro medio) | `EnrollmentsResources.resx` | "No pudimos generar el enlace de pago. Intentá nuevamente más tarde o elegí otro medio de pago." Opcional: separar `INS_UF_06` para el caso (3) con "El enlace de pago que generamos no es válido…"; recomiendo **no separarlo**. La exigencia de parámetros cifrados para todas las pasarelas viene de F1b; el back no la verifica por pasarela. |
| `POST enrollments/start-payment` (FE-R5-209) | `result` fuera del conjunto conocido. | sin código: cambio de contrato OpenAPI | `StartPaymentResponse.Result` como enum de string (`PAGO_CONFIRMADO`, `METODO_GUARDADO`, `URL_GENERADA`), con `JsonStringEnumConverter` o filtro de Swagger | Nota para F1b: "en proceso" no existe en `start-payment`; ese estado lo da `confirm-pre-enrollment` (`waiting`). Si el front lo espera de `start-payment`, es una suposición sin respaldo en el back. |
| `POST enrollments/start-payment` (`StartPaymentResponse.Messages`) | Pago con cuenta personal: `Messages[]` viene de la API interna (`CartPaymentMessage` `clave`/`valor`) y se copia sin curar (`EnrollmentMapper.ToPaymentMessages`). | sin código (éxito) | pedido a Análisis Funcional: lista de `clave` que emite la API interna | Recomendada: el back mapea cada `clave` conocida a un `.resx` (`PAGO_MSG_<clave>`) y usa un texto de respaldo para las desconocidas: "El pago se procesó. Revisá el detalle en tu cuenta." Sin la lista de claves no puedo proponer los textos. |

### 9.3 Textos de pagos y confirmación a corregir (FE-ENR-005 y FE-ENR-013)

Estos errores **ya los cubre el back y salen del `.resx`**, pero usan nombres de campos internos y no tienen tildes; con R1 el usuario los va a leer. Todas las claves son de `EnrollmentsResources.resx`.

| Clave | HTTP | Texto actual | Texto propuesto |
| --- | --- | --- | --- |
| `INS_PAG_00`, `INS_UF_00`, `INS_PC_00`, `INS_MP_00`, `INS_REA_00` | 400 | "Request invalido." | "La solicitud no es válida. Revisá los datos e intentá nuevamente." |
| `INS_UF_01`, `INS_PC_01`, `INS_MP_01` | 400 | "IdsInscripcion invalido." | "Elegí al menos una inscripción para continuar." |
| `INS_PAG_01`, `INS_UF_02`, `INS_MP_02` | 400 | "TipoPago invalido." | "Elegí un medio de pago válido." |
| `INS_UF_03` | 400 | "IdBancoSistarbanc requerido para SISTARBANC." | "Elegí un banco para pagar con Sistarbanc." |
| `INS_UF_04`, `INS_PC_02`, `INS_MP_03` | 404 | "No se encontro la inscripcion para la persona." | "No encontramos la inscripción que querés pagar." |
| `INS_MP_04` | 409 | "La reserva minima ya fue registrada para la inscripcion." | "La reserva mínima de esta inscripción ya fue registrada." |
| `INS_REA_01` | 404 | "No se encontro la inscripcion {0} para la persona." | "No encontramos la inscripción {0}." |
| `INS_REA_02` | 409 | "La inscripcion {0} no esta dada de baja." | "La inscripción {0} no está dada de baja, por lo que no se puede reactivar." |
| `INS_CPI_06` | 404 | "No se encontro una encuesta inicial de admision vigente para la persona." | "No encontramos una encuesta inicial de admisión vigente." |
| `INS_CPI_07` | 409 | "La encuesta inicial debe estar completa para confirmar la preinscripcion." | "Completá la encuesta inicial para confirmar tu preinscripción." |
| `INS_CPI_12` | 409 | "La encuesta inicial de admision se encuentra vencida." | "La encuesta inicial de admisión está vencida. Volvé a completarla." |
| `INS_CPI_14` | 409 | "No existe interes activo para la oferta seleccionada." | "No registramos interés activo para la oferta seleccionada." |
| `INS_CPI_16` | 409 | "La oferta seleccionada no se encuentra abierta para inscripcion." | "La oferta seleccionada no está abierta para inscripción." |

**FE-ENR-013** (409 de `confirm-pre-enrollment`): confirmado. El 409 sale con `message` curado desde el `.resx` en `INS_CPI_07`, `INS_CPI_12`, `INS_CPI_14`, `INS_CPI_16` y `INS_REA_02` (en `reactivate`). Lo que falta es ortografía y redacción (tabla anterior). Ninguna ruta emite 409 con texto de otra capa.

### 9.4 Por qué no hay contrato nuevo en el resto

- **FE-AUTH-006 — confirmo lo dicho en §7.** `RecoverPassword.cs:89,94,99` devuelve 2xx con `OkPasswordRecoveryRequested` si la persona no existe, si los datos no coinciden y ante cualquier excepción. Los 4xx que quedan en el endpoint (`REC_PAS_01/02/03` de formato, `AUTH_CAPTCHA_01/04/05` en modo `ScoreOnly`, `AUTH_RL_05` 429 y los de §5) no dependen de que la cuenta exista. El front puede borrar la rama 4xx y mostrar el `message`; los textos de esos 4xx están en §1 (L4, L6..L11, L13).
- **FE-AUTH-028.** `verify-identity` ya lleva `OkRegisteredMailNotSent` en el envelope (`VerifyIdentity.cs:~209`). `confirm-registration-request` con `mailSent=false` **no es un fallo de mail**: es `PendingReview=true` (el alta queda para revisión manual, sin mail por diseño) y su `message` es `OkRequestRegistered`. El front no debe mostrar "no se envió el mail" en ese caso; esto corrige la lectura de F1a, que lo agrupaba con los otros dos.
- **FE-AUTH-018.** `EvaluateDocument` pone exactamente un flag por rama y siempre un `message` curado (`OkDocumentUnderReview`, `OkNoPendingRequest`, `OkPersonNotFound`, `OkPersonAlreadyRegistered`, `OkIdentityVerificationRequired`). No hay camino 2xx "sin flujo". La matriz flags → flujo puede quedar en el front (R4); si no matchea ninguno, que muestre el `message` del envelope en vez de un texto propio. No hace falta campo `flow` nuevo.
- **FE-AUTH-019.** El controller asigna `FlowId` a todo flujo que no sea `UserAlreadyRegistered`. Si Redis falla, `CreateFlowSessionAsync` lanza y el middleware responde 500 `INTERNAL_ERROR` (hoy "Error inesperado"; mejora con D5 y F5). Sin cambio de back.
- **FE-HOME-009.** `UpdatePersonDetails` responde `Ok(true)` o fallo con `message` (`PER_ADP_01..09`). La rama `!success` es muerta. Sin cambio de back; coincido con la nota del auditor F1a.
- **FE-ENR-012.** `UploadPersonPhoto` y `UploadPersonIdentityDocument` solo devuelven `Ok(true)` o fallo con `message` (`SUB_FOT_01`, `SUB_DOC_01`, `GEN_SFA_01..05`, `GEN_SDA_02..04`, `FILE_VAL_*`, 409 en identity-document). Nunca 200 con `false`.
- **FE-ENR-005.** `EnrollmentOffering.EnrollmentId` es `long?` y solo es nulo en estado "En proceso" (`waiting`), que no pasa por pago. `start-payment` rechaza ids inexistentes o ajenos con 404 y texto del `.resx` (textos a corregir en 9.3).
- **FE-R5-010.** F1 lo reclasifica como R5 y coincido: el guardado posterior ya devuelve `PER_ADP_07` con texto del back (con la salvedad de §6.5 sobre el `{0}` que llena Core) y `validate-phone-number` es una ayuda de formato anónima y con rate limit. Solo queda corregir el XML-doc del controller, que dice "400 Telefono invalido" y el código responde 200 con `data:false`.

### 9.5 F1c — endpoints de scholarships cuyo `message` verifiqué

Todos devuelven fallos con `message` del `.resx` de `AppLogic.Scholarships` (con tildes y voseo). Uploads y deletes devuelven `Ok(true)` o fallo, nunca `false` con 200 (`AffidavitFileService`).

| Endpoint | HTTP de fallo | Códigos | Origen del texto | Veredicto |
| --- | --- | --- | --- | --- |
| `GET scholarships/affidavit` | 404 | `BEC_DJ_01` | `.resx` | Curado |
| `PUT scholarships/affidavit` | 400, 404, 409 | `FDB_GDJ_07..50`, `BEC_DJ_01`; 409: `FDB_GDJ_16` (confirmada), `FDB_GDJ_17` (plazo vencido) | `.resx` | Curado |
| `POST scholarships/affidavit/confirm` | 400, 404, 409 | `FDB_GDJ_07,14,27,32..35`, `BEC_DJ_01`; 409 `FDB_GDJ_16/17/36` | `.resx` | Curado salvo `FDB_GDJ_36` (nombra el CRM; es un 409 determinístico, `AffidavitRejection.cs:96`, `SgiInterestAmbiguous`) |
| `POST scholarships/applications` | 400, 404 y propagados | `BEC_PO_01/02`; propaga `ALTA_POSTULACION_BECA_01` y `API_*` de EnrollmentsAndPayments (`ScholarshipService.cs:~120`, `CreateApplicationAsync`) | `.resx` | `BEC_PO_*` curados. El alta es **no idempotente**: ver §3.1 |
| `POST scholarships/applications/accept` | 400, 404, 409 | `BEC_AC_01/02/03` | `.resx` | Curado |
| `GET scholarships/application-options` | 404 | `BEC_PO_01` | `.resx` | Curado |
| `GET scholarships/available` | 400 | falla de integración o validación | `.resx` de EnrollmentsAndPayments | Mejorable (jerga "La API", §3) |
| `POST/GET/DELETE scholarships/affidavit-document` | 400, 404 | `FDB_ADJ_01` (hoy literal, L1), `FDB_SAR_01`, `FDB_DAR_01/03`, `FDB_EAR_01`, `FILE_VAL_*` | `.resx`; `FILE_VAL_*` de Core (aptos, §6.6) | Curado una vez aplicado L1 |
| `POST/GET/DELETE scholarships/income-receipt` | 400, 404 | `FDB_ADJ_01` (L1), `FDB_SAI_01..03`, `FDB_DAI_01..05`, `FDB_EAI_01..03`, `FILE_VAL_*` | ídem | Curado una vez aplicado L1 |

Texto propuesto para `FDB_GDJ_36` (el usuario no necesita saber del CRM, y "intentá más tarde" no lo resuelve porque el 409 es determinístico): "No pudimos registrar tu postulación. Comunicate con Admisiones para continuar." con el detalle al log.

### 9.6 Candidatos propios de F2 (no vienen de F1)

| Endpoint | Caso | Código propuesto | Texto propuesto |
| --- | --- | --- | --- |
| cualquiera con `[Authorize]` | 401 sin token o con token inválido (hoy cuerpo vacío; no hay `OnChallenge`) | `AUTH_UNAUTHORIZED` (ya existe para excepción) | "Tu sesión no es válida o venció. Iniciá sesión nuevamente." |
| cualquiera con `[Authorize]` | 403 (hoy cuerpo vacío) | `AUTH_FORBIDDEN` (nuevo) | "No tenés permiso para realizar esta acción." |
| cualquiera | 404/405 de ruteo (hoy cuerpo vacío) | `NOT_FOUND` / `METHOD_NOT_ALLOWED` (nuevos) | "No encontramos lo que buscabas." / "La operación no está disponible." |
| uploads | 413 por `MaxRequestBodySize` de Kestrel (hoy cuerpo vacío, antes del pipeline) | `PAYLOAD_TOO_LARGE` (nuevo) | "El archivo es demasiado grande. Subí uno más liviano." |
| `POST registration/analyze-attachment` | todos los `REC_DOC_*` de Core | ver §6.1 | ver §6.1 |

---

## PREGUNTAS (con opción recomendada)

1. **Dónde viven los textos del host**: `WebApiAdmisiones/Resources/ApiResources.resx` (recomendada, patrón de `AppLogic.*`). Alternativa: repartir las claves en el `.resx` del módulo dueño (Authentication para `AUTH_*`, etc.). Mi recomendación se apoya en que el host no pertenece a ningún módulo.
2. **Ventana en `AUTH_RL_01/05`**: hoy se dice la ventana configurada, no el tiempo restante. Recomendada: conservarla ({0} = minutos de la ventana) y no revelar el número de intentos. Alternativa: pasar solo "más tarde" y dejar el tiempo real al header `Retry-After`.
3. **Mensaje por campo en producción** (§5.4): hoy `MBM_DA_01` es un único texto. ¿Se quiere el detalle por campo? Recomendada: no en esta iniciativa (la validación previa al envío es del front, R5); anotar como mejora posterior.
4. **D5** (código de referencia en el 500): propuse el texto con `{0}` y el sin código. Recomendada: con código.
5. **Textos de `CAM_PAS_03`, `REC_DOC_11` y `SANITIZATION_ERROR`** dependen de reglas de negocio (qué documentos se admiten, qué rechaza LDAP, qué caracteres se prohíben). Recomendada: que AF los confirme.
6. **¿Aprobás pedidos aparte a `Core`?** Recomendada: sí, por tres pedidos: (a) Azure y LDAP no mezclan detalle técnico en `Message`; (b) `ValidadorTelefonos.Error` sin `ex.Message` ni centinelas; (c) opcional, `FILE_VAL_*`/`Util.*` a `.resx`. Todo lo de §6 tiene solución sin Core; los pedidos son para dejar la causa de raíz.

7. **Llamadas no idempotentes (§3.1)**: lo que dije en los textos "incierto" supone que el front muestra la pantalla de espera ante ese error (dato de F1, no verificable desde el back). Lo que sí verifiqué en el back: `ConfirmPreEnrollment.cs:155`, `PayWithPersonalAccount.cs:59` y `ScholarshipService.cs:~120` llaman a la API interna sin clave de idempotencia y el timeout es de 2 minutos; el error no dice si el proveedor procesó. Recomendada: re-mapear en los tres casos de uso. La promesa "Admisiones se va a comunicar con vos" necesita que exista ese proceso: confirmar con AF; si no existe, usar "Comunicate con Admisiones".
8. **Voseo**: el texto sugerido por CP1 decía "contigo"; lo escribí "con vos" por el voseo de los `.resx`. Si prefieren "contigo", es un cambio de texto, no de clave.

## FUERA DE ALCANCE VISTO

- `SCHEMA_ERROR` responde 400 por un fallo interno (debería ser 500). `ExceptionHandlingMiddleware` usa 409 para `SANITIZATION_ERROR` y 401 para `AUTH_UNAUTHORIZED` con el texto "Error inesperado".
- `ACT_LINK_NUP_03` es un caso de negocio (datos de registro vencidos) con status 500.
- `EnrollmentsAndPayments`: un rechazo de negocio del proveedor con 5xx pierde su razón (comentario en el código; pendiente de AF).
- Ortografía inconsistente en `.resx` ya existentes: `AUTH_CAPTCHA_99`, `FLOW_03_Document`, `INS_CPI_13`, `INS_CPI_17_SameProduct`, `API_TIMEOUT`, etc. sin tildes; `ACT_SES_03` / `ACT_SES_NUP_01` con y sin tildes (ya anotado como decisión de AF).
- `Ldap.cs`: `AUTH_LDAP_10/11/98`, `30/31/32/96` y `EnviarContrasenia`/`GenerarPasswordAsync` no tienen llamadores en `AppLogic`: código muerto desde la API.
- `InMemoryJsonSchemaRegistry` registra un solo esquema de ejemplo; el filtro `JsonSchemaValidationFilter` valida casi solo Accept y Content-Type.
- El `git status` del monorepo sigue mostrando `api-admisiones` modificado: no lo toqué.
