# Avance ASVS 5.0.0

Actualizado: 2026-09-17 (v5.0.0-6.2.2). Fase: análisis y documentación exclusivamente L1; fixes no autorizados.

## Checkpoint actual

- Requisito en curso: ninguno.
- Siguiente requisito: v5.0.0-6.2.3 (L1).
- Último requisito documentado: v5.0.0-6.2.2 (L1), NEEDS_REVIEW.
- Selección vigente: solo L1, en orden oficial; L2 reservado para una fase posterior con pedido explícito del usuario.
- Progreso L1: 29 DOCUMENTADO, 0 EN_CURSO, 41 POR_REVISAR (total 70).
- L2 reservado: 2 DOCUMENTADO y 181 POR_REVISAR (total 183); se preservan sus estados y fichas.
- Progreso general L1/L2 conservado: 31 DOCUMENTADO, 0 EN_CURSO, 222 POR_REVISAR.
- L3: 92 FUERA_L2; no se consideran NOT_APPLICABLE.
- Próxima acción: con un nuevo pedido de seguir, revisar únicamente v5.0.0-6.2.3 (L1); verificar Git/fuente y fijar refs antes de investigar.
- v5.0.0-6.2.2 DOCUMENTADO / NEEDS_REVIEW: menú y ruta de cambio en frontend; POST autenticado /person/change-password toma identidad del token, valida y llama a Core; Core envía la operación SOAP y sólo reporta éxito si el proveedor devuelve true. Tests existentes leídos, no ejecutados; falta comprobar el cambio y login posterior contra LDAP en el ambiente desplegado. Evidencia y pendientes en [ficha](./revisiones/v5.0.0-6.2.2.md). HEAD 313b57929eaa6eea438e543850a4f27f7f54ca9a; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre blob. Sin commit: AVANCE.md y ficha 6.2.2. Sin fixes, tests, builds, scans, commits ni pushs. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-6.2.3».
- v5.0.0-6.2.1 DOCUMENTADO / NEEDS_REVIEW: Core exige 12-20 caracteres mediante ValidarPasswordNueva y los tres caminos de contraseña de API lo invocan antes de LDAP. El mínimo obligatorio de 8 está cubierto por el código propio para contraseñas ASCII; no se verificó el servidor LDAP ni runtime. El mínimo recomendado de 15 no se alcanza. Tests existentes leídos, no ejecutados. Evidencia y pendientes en [ficha](./revisiones/v5.0.0-6.2.1.md). HEAD 06e0a7912e06ad5abc46a250d6c0469bc82a8211; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre blob. Sin commit: AVANCE.md y ficha 6.2.1. Sin fixes, tests, builds, scans, commits ni pushs. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-6.2.2».
- v5.0.0-6.1.1 DOCUMENTADO / NEEDS_REVIEW: guía de dos límites de login, pero código usa además contadores de fallos, uno global por documento que podría causar bloqueo temporal dirigido; 2FA adaptativo y política ante falla Redis tampoco están documentados por completo. Evidencia y pendientes en [ficha](./revisiones/v5.0.0-6.1.1.md). HEAD b4362cede0a3a4f38bdc8c8691838cc716da9b23; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre blob. Sin commit: AVANCE.md y ficha 6.1.1. Sin fixes, tests, builds, scans, commits ni pushs. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-6.2.1».
- Aviso de método: en checkouts Windows con `core.autocrlf=true`, el hash del JSON fuente sobre el archivo en disco NO coincide con el oficial. Verificar sobre el blob: `git cat-file -p HEAD:security/asvs/source/OWASP_Application_Security_Verification_Standard_5.0.0_en.json | sha256sum`. Ver hallazgo 3 de la ficha 2.2.1.
- Aviso de ubicación: `security/asvs/planning/` sólo existe en la rama `fix/owasp`; en `main` no está. Verificar la rama antes de retomar.
- v5.0.0-1.1.1 documentado por revisión estática de frontend/API/Core; faltan equivalencia esquema/binding/sanitización, runtime y contrato de pagos. Evidencia y métodos pendientes en su ficha. No se aplicaron fixes.

- v5.0.0-1.1.2 documentado por revisión estática: codificación/serialización observada en DOM, HTTP/JSON y correo; pendientes receptor de pagos, correo entregado, runtime y cobertura de otros sinks. Ver ficha; no hay cumplimiento global demostrado.
- Checkpoint 1.2.2 sin commit por instrucción del usuario: AVANCE.md y revisiones/v5.0.0-1.2.2.md modificados/creados; copia Markdown API en worktree temporal fix/owasp (ref en ficha). No hacer commits ni pushs. Staging previo preservado; HEAD cambió externamente de 75b80a958b3a57d94625cd93461a72b90fa9fff8 a cd3f71e70e2a748afc2e405ce4054d72b4c1c9e6 incorporando solo documentación 1.2.1; el agente no hizo commits. Inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.2.md`.

- v5.0.0-1.2.1 documentado por revisión estática de contextos HTML/HTTP y proxy SOAP/XML. Pendientes: DOM y librería SVG, correo entregado, envelope SOAP, headers efectivos y otros consumidores Core; ver ficha. HEAD evaluado: 75b80a958b3a57d94625cd93461a72b90fa9fff8; Core consumido verificado: 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Estado inicial: solo graph.json sin seguimiento, staging vacío; cambios de esta iteración limitados a dos Markdown, sin fixes/commits/pushs.

- v5.0.0-1.2.2 documentado por revisión estática de URLs en frontend/API/Core: paths/query con escape y formulario de pago limitado a http/https. Pendientes: configuración de activación/fragmentos, proveedor de pagos, mailto, router/framework y protocolos/polling Azure. Resultado NEEDS_REVIEW; evidencia, límites y propuesta distribuida API en ficha. Sin fixes ni ejecución de tests/runtime.

- v5.0.0-1.2.3 documentado: serialización JavaScript/JSON en frontend/API/Core; pendientes bytes/round trip, parser externo de pagos, consumidores de SerializeJsonLog y visor de logs. Resultado NEEDS_REVIEW. HEAD evaluado f41cba032b800ba3cd17525e6608274644ea7e55; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Checkpoint sin commit por instrucción del usuario: AVANCE.md, ficha 1.2.3 y dos copias Markdown en worktrees API/Core fix/owasp (rutas/refs en ficha). Staging vacío preservado. Sin fixes, builds, tests, commits ni pushs. Para inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.3.md`.

- v5.0.0-1.2.4 documentado por revisión estática de consultas EF/LINQ, secuencias SQL y Redis API/Core; callers string encontrados usan constantes, pero las variantes genéricas no validan localmente el identificador. Resultado NEEDS_REVIEW; pendientes binding Devart, esquema/procedimientos/triggers Oracle, otros consumidores Core y persistencia externa de pagos. HEAD evaluado 7fea09ee286b932b579ff720d90726e9e0de72d2; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Archivos sin commit: AVANCE.md, revisiones/v5.0.0-1.2.4.md y dos propuestas docs/security/ASVS-v5.0.0-1.2.4-propuesta.md en worktrees API/Core fix/owasp (rutas/refs en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y cambios ajenos preservados. Inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.4.md`. Continuar solo con un nuevo pedido: «Seguí security/asvs/planning/ITERACION.md y revisá únicamente v5.0.0-1.2.5». No hacer commits ni pushs.

- v5.0.0-1.2.5 DOCUMENTADO / NEEDS_REVIEW: no se localizaron sinks OS directos en C# API/Core; herramientas frontend leídas pasan arrays sin shell. Pendientes wrappers externos, selección de agentes con claves heredadas, runtime/hosting y servicios externos. HEAD evaluado 6c7c865482f1d5f1f401d6c5f23d43b1c1a036c1; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Sin commit: AVANCE.md, revisiones/v5.0.0-1.2.5.md y propuesta frontend temporal fix/owasp (ruta/ref en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.5.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-1.3.1». No hacer commits ni pushs.

- v5.0.0-1.3.1 DOCUMENTADO / NEEDS_REVIEW: sin editor HTML enriquecido ni sinks HTML explícitos localizados en frontend propio; API registra HtmlSanitizer global pero el filtro no recorre explícitamente elementos de colecciones. Listas reales de texto de encuesta llegan a entidades con normalización; falta demostrar un receptor HTML para confirmar impacto/aplicabilidad. Tests existentes leídos, no ejecutados; tests de registro solo comprueban DI no vacío. Pendientes componentes externos, versión/política resuelta, pipeline/runtime y otros consumidores Core/correo. HEAD 11a3319b053bde4744a47552af70596d22ea767c; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Sin commit: AVANCE.md, revisiones/v5.0.0-1.3.1.md y propuesta API temporal fix/owasp (ruta/ref en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.3.1.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-1.3.2. No apliques fixes, commits ni pushs».

- v5.0.0-1.3.2 DOCUMENTADO / NEEDS_REVIEW: no se localizaron evaluadores directos propios de strings en frontend/API/Core; callbacks, imports literales, reflexión sobre propiedades y operaciones Redis leídos manipulan datos. Pendientes bundle/compilación efectiva, paquetes privados/transitivos, reCAPTCHA, assemblies/hosting, servicios externos y otros consumidores Core. HEAD 77e9fcdddf38b50fc5b81831582e30770a9a8803; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado. Sin commit: AVANCE.md y revisiones/v5.0.0-1.3.2.md; sin propuesta distribuida porque no hay fix concreto. Sin fixes/builds/tests/scans/commits/pushs; staging vacío, graph.json ajeno y .claude/ aparecido durante la sesión preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.3.2.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-1.5.1. No apliques fixes, commits ni pushs».

- v5.0.0-1.5.1 DOCUMENTADO / NEEDS_REVIEW: XML SOAP en LDAP/correo Core; callers pasan bindings propios, sin atribuirles las cuotas máximas de factories generadas. Pendientes parser/assemblies efectivos, pruebas aisladas de DTD/entidades sin acceso externo, formatters API, SVG privado y otros consumidores/servicios. HEAD 448932a10d93cdcc1929839a32ae634dcd5508ee; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado. Sin commit: AVANCE.md y revisiones/v5.0.0-1.5.1.md; no hay fix concreto para distribuir. Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.5.1.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-2.1.1. No apliques fixes, commits ni pushs».

- v5.0.0-2.1.1 DOCUMENTADO / NEEDS_REVIEW: reglas concretas de teléfono/registro/imágenes documentadas; drift de referencias históricas de registro y allowlist XML docs/contrato/regla. Pendientes inventario completo de estructuras, formatos DE/PS/CC, semántica email, OpenAPI del commit, ejemplos ejecutados e integraciones. HEAD 3362acec955936eb39e10a0c20e2facb9857420d; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado. Sin commit: AVANCE.md, revisiones/v5.0.0-2.1.1.md y propuesta API temporal fix/owasp (ruta/ref en ficha); distribución frontend/Core pendiente. Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-2.1.1.md`. Continuar solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-2.2.1. No apliques fixes, commits ni pushs».

- v5.0.0-2.2.1 DOCUMENTADO / NEEDS_REVIEW: allowlist cerrada de tipo de documento aplicada en los siete casos de uso que deciden autenticación, recuperación de password y rama del registro; patrón/rango en el alta de persona; validación de archivos por magic bytes y tamaño ignorando la extensión declarada. Dos hallazgos: (1) `InMemoryJsonSchemaRegistry` sólo registra `POST /api/datospersonales`, ruta inexistente en esta ref, por lo que la validación estructural por esquema no cubre ningún endpoint y el filtro global aporta sólo `Accept`/`Content-Type`; (2) el número de documento no tiene estructura ni longitud definidas para DE/PS/CC pese a decidir la creación del usuario LDAP y componer claves de caché/rate limiting. Pendientes: ruteo efectivo, decisión de poblar o retirar el registry, formatos DE/PS/CC, llamadores reales de `FileValidator` y ejecución de los tests existentes. HEAD evaluado 01c073b87227a9eccdb30be1f92ffb21c559f167; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob (ver aviso de método). Sin commit: AVANCE.md y revisiones/v5.0.0-2.2.1.md; distribución de propuestas a API pendiente, no se verificaron los worktrees temporales. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. La sesión comenzó en `main` y se hizo `git checkout fix/owasp` desde árbol limpio. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-2.2.1.md`. Continuar solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-2.2.2. No apliques fixes, commits ni pushs».

- v5.0.0-2.2.2 DOCUMENTADO / NEEDS_REVIEW: la validación sí vive en el servidor para lo central —sujeto siempre desde el token (19 usos de `GetUserId()`, ningún `personId` por body/query), política de password idéntica en los tres caminos de alta/cambio, teléfono recompuesto y revalidado descartando lo que afirmó el cliente, monto no enviado por el cliente y método de pago contra allowlist, IDs de catálogo resueltos contra la base, archivo validado antes del proveedor externo y anotaciones ejecutables por `[ApiController]` sin supresión de ModelState—. Tres hallazgos: (1) `PUT /person` acepta `DocumentType`, `DocumentNumber`, nombres, `BirthDate` y `Sex` sin ninguna anotación ni regla de dominio cuando la identidad no está restringida; en el frontend esos campos son `readonly` y quedan fuera del payload, así que la barrera efectiva es el cliente; (2) límites de longitud (100/200/254) y el patrón de documento no-CI existen sólo en el frontend, y `UpdatePersonDetailsRequest` no valida ni el formato del mail — comparte defecto con el hallazgo 2 de 2.2.1, no se cuenta dos veces; (3) `PhoneNumber.IsValid` está documentado como veredicto del cliente pero ningún camino de servidor lo lee en entrada: deuda de contrato, no defecto. Pendientes: probar `PUT /person` con identidad fuera de regla (es la prueba que decide PASS/FAIL), esquema Oracle para los largos, política de autorización efectiva en runtime, y el mismo par cliente↔servidor en becas/encuestas/catálogos (L2). HEAD evaluado 583baa1fd37377ee1f835aac74147d53ca917534; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-2.2.2.md; distribución de propuestas a API pendiente, no se verificaron los worktrees temporales. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar salvo el propio AVANCE.md. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-2.2.2.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-2.3.1. No apliques fixes, commits ni pushs».

- v5.0.0-2.3.1 DOCUMENTADO / NEEDS_REVIEW: el orden de los flujos multi-paso sí se impone en el servidor, no en el cliente —el frontend no tiene guards de ruta y el paso es una señal en memoria—. Login 2FA correcto por construcción: la sesión 2FA sólo nace tras bind LDAP y los tokens sólo se emiten tras código correcto. Inscripción verifica los pasos previos por su efecto persistido (interés activo, encuesta completa y vigente, documentos, reglamento, oferta abierta). Registro tiene gate de sesión `X-Flow-Id` en Redis con step y atadura del documento a la sesión. Tres hallazgos: (1) `analyze-attachment` no recibe ni valida `X-Flow-Id` y cachea las imágenes bajo `registro:documento-imagenes:{tipo}:{documento}` con el documento que extrajo Azure, TTL 24 h — un tercero puede sembrar imágenes para un documento ajeno y quedan persistidas en la ficha de esa persona cuando ella activa; no da acceso a la cuenta y exige que Azure lea el archivo como ese documento; (2) `confirm-new-person` retorna Ok sin actualizar el step cuando falla el mail, así que la sesión sigue en `evaluado` y el paso terminal admite reejecución; (3) deuda, no defecto: `Step.IdentidadVerificada` es constante muerta, `stepEsperado` siempre vale `Evaluado` y `RegistrationFlowSession.PersonId` se crea siempre `null`, así que la rama la elige el cliente — no es explotable porque cada rama revalida el documento contra el padrón. Pendientes: pruebas negativas de orden (deciden PASS/FAIL), estado de inscripción exigido por la API interna de pagos (`PayCartsByEnrollmentAsync`, externa), becas y encuesta como flujos propios, y ejecución de los tests existentes. HEAD evaluado e3ea636b6562f7fff39a25c1a7252e80bce6b5f2; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre el blob. Nota: HEAD cambió respecto de la ficha 2.2.2 (583baa1f…) porque el usuario commiteó aquel checkpoint; el agente no hizo commits. Sin commit: AVANCE.md y revisiones/v5.0.0-2.3.1.md; distribución de propuestas a API pendiente, no se verificaron los worktrees temporales. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-2.3.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.2.1. No apliques fixes, commits ni pushs».

- v5.0.0-3.2.1 DOCUMENTADO / NEEDS_REVIEW: único endpoint que sirve bytes de un recurso directamente es `GET /person/photo` (`PersonController.GetPersonPhoto`), con `Content-Type: image/jpeg` fijado por constante del servidor (no por el cliente) y `X-Content-Type-Options: nosniff` aplicado globalmente por `UseSecurityHeaders`; esa combinación mitiga el riesgo central del requisito para este endpoint sin ser evidencia reproducible de control suficiente. `/person/identity-document` no aplica al mismo riesgo porque el archivo viaja como campo de un JSON, no como bytes top-level. Dos hallazgos: (1) `/person/photo` no lleva `Content-Disposition`, no hay directive `sandbox` en la CSP global y no hay ningún chequeo de `Sec-Fetch-*` — ninguno de los controles que el texto oficial ejemplifica está presente, aunque el consumo previsto desde la SPA es XHR con `responseType: 'blob'`, no navegación directa; (2) deuda, no defecto: el `Content-Type` servido no se revalida contra el tipo real detectado por magic bytes al subir (`FileValidator`), aunque `nosniff` cubriría igual un blob corrupto. Pendientes: confirmación en runtime de si `nosniff` + tipo fijo son equivalentes a los controles del requisito, headers agregados por la plataforma de despliegue (no versionados en el repo), cobertura exhaustiva de otros posibles sinks de bytes y ejecución de tests existentes. HEAD evaluado 408d256bc93892a9657fff252edb94ade3ea8f35; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.2.1.md; sin propuesta distribuida a API (no se verificaron worktrees temporales). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.2.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.2.2. No apliques fixes, commits ni pushs».

- v5.0.0-3.2.2 DOCUMENTADO / NEEDS_REVIEW: sin sinks de interpretación HTML localizados en `admisiones/src` (sin `[innerHTML]`, `dangerouslySetInnerHTML`, `bypassSecurityTrust*`, `document.write`, `insertAdjacentHTML`; la interpolación `{{ }}` de Angular es la vía por defecto y equivale a `textContent`). En API/Core, el único generador de HTML relevante es `MailORT.EnvioMail` (plantillas de correo); sus métodos de plantilla no codifican internamente, pero el único consumidor con dato de usuario real (`PasswordMailTemplate.BuildActivationMail`/`BuildRecoveryMail`) codifica `Persona.PrimerNombre` y el link con `HtmlEncoder.Default.Encode` antes de interpolar; `TwoFactorAuthService` sólo interpola un código OTP generado por el servidor. No se hallaron vistas Razor ni otros motores de plantillas HTML en `api-admisiones`. Sin hallazgo de defecto confirmado; una observación de deuda de diseño: los helpers de `EnvioMail` no fuerzan codificación, por lo que un futuro consumidor podría omitirla sin aviso del compilador. Pendientes: ejecución de tests existentes, verificación en runtime del renderizado en cliente de correo/navegador, cobertura de otros consumidores futuros de `EnvioMail`/`IEmailSender` y del bundle Angular compilado (más allá del código fuente). HEAD evaluado f78829e4a367191f02752bd70f8f19e6031f2a68; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.2.2.md; sin propuesta distribuida a otro repo porque no hay fix concreto sobre código actual defectuoso, sólo una recomendación preventiva documentada en la ficha. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.2.2.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.3.1. No apliques fixes, commits ni pushs».

- v5.0.0-3.3.1 DOCUMENTADO / NEEDS_REVIEW: único emisor de cookies del sistema es `CookieAuthenticationHelper` (API), usado por `AuthController` para tres cookies de autenticación (`X-Access-Token`, `X-Refresh-Token`, `X-Password-Activation`); Core (534 archivos .cs, negativo) y frontend (sin `document.cookie`/librerías, solo `withCredentials`) no emiten cookies. Cumple la primera cláusula del requisito: `Secure = true` está hardcodeado sin condicional de entorno en las cinco operaciones de alta/baja, a pesar de un comentario de código engañoso que sugiere lo contrario (hallazgo 2, deuda). No cumple la segunda cláusula: ninguna de las tres cookies usa prefijo `__Host-` ni `__Secure-` en el nombre (hallazgo 1); `Domain` configurable por `AUTH_COOKIE_DOMAIN` descarta `__Host-` como opción mientras exista esa configuración, dejando `__Secure-` como único prefijo viable. Pendientes: verificación en runtime del comportamiento real del navegador por entorno, configuración de hosting/proxy no versionada, bundle Angular compilado. HEAD evaluado f10c072220428a1fb743083fe9e1363e00c511d0; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.3.1.md; sin propuesta distribuida a un worktree de API porque no se verificó la existencia de worktrees temporales vigentes (queda pendiente explícito en la ficha). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.3.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.3.4. No apliques fixes, commits ni pushs».

- v5.0.0-3.4.1 DOCUMENTADO / NEEDS_REVIEW: en API, `UseSecurityHeaders` fija `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` de forma incondicional (cumple 1 año + subdominios), pero coexiste con el `UseHsts()` estándar de ASP.NET Core (sin `AddHsts` explícito, default 30 días sin subdominios) registrado antes en el pipeline y antes de `UseForwardedHeaders`. Hallazgo: el callback tardío (`OnStarting`) de `HstsMiddleware` puede sobrescribir el valor correcto con el default de 30 días si la app ve `IsHttps=true` en ese punto (TLS terminado en la propia app); si está detrás de un proxy que reenvía esquema después, el callback no llega a registrarse y prevalece el valor correcto — depende de topología de despliegue no versionada en el repo, indeterminable por lectura estática. Frontend y docs-site fijan el header vía `web.config`/IIS con el valor correcto, sin el mismo riesgo de sobrescritura en código. Pendientes: confirmar topología real de TLS/proxy por ambiente, test de integración de pipeline completo, verificación del `web.config` generado. HEAD evaluado 437564bfdbf4e96bd9466d348496ba620593913a; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.4.1.md; sin propuesta distribuida a worktree de API (no se verificó su existencia). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.4.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.4.2. No apliques fixes, commits ni pushs».

- v5.0.0-3.4.2 DOCUMENTADO / NEEDS_REVIEW: única política CORS de `api-admisiones` (`ServiceCollectionExtensions.AddCorsPolicy`) usa `WithOrigins` sobre un array fijo de 8 orígenes hardcodeados (2 localhost dev + 6 `admisiones*.ort.edu.uy`), aplicada globalmente vía `UseCors("AllowAngularApp")`; sin `AllowAnyOrigin()`/`SetIsOriginAllowed` productivo (único uso está en un test unitario con su propio host de prueba) y sin construcción manual del header `Access-Control-Allow-Origin`. Cumple en código fuente la cláusula de allowlist fija del requisito. Dos hallazgos, ninguno defecto confirmado: (1) los tests `AddCorsPolicy_Includes*Origins` declaran orígenes esperados que no coinciden con la allowlist real (`gestion.ort.edu.uy` vs `admisiones*.ort.edu.uy`) y solo verifican `Assert.NotEmpty(services)`, por lo que no detectarían una regresión a `AllowAnyOrigin()`; (2) ADR-009 (draft) documenta una reversión sin explicar de `DisallowCredentials()` a `AllowCredentials()` en mayo 2026 y pide confirmar el estado vigente en producción, contexto de gobernanza sin evidencia de defecto en el código actual. Pendientes: verificación runtime del header efectivo por entorno, confirmación de que el binario desplegado coincide con el código, cierre del gap de tests y seguimiento externo de ADR-009. HEAD evaluado 01d8c9c93f03825433ec487b47b70778b40a8790; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.4.2.md; sin propuesta distribuida a worktree de API (no se verificó su existencia). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar salvo el propio AVANCE.md. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.4.2.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.5.1. No apliques fixes, commits ni pushs».

- v5.0.0-3.5.1 DOCUMENTADO / NEEDS_REVIEW: la autenticación JWT lee el token prioritariamente desde cookie HttpOnly (`AuthenticationExtensions.HandleOnMessageReceived`), es decir, la credencial viaja automáticamente en cualquier request cross-origin con `withCredentials`; la única defensa CSRF diseñada es la combinación CORS con allowlist fija + `AllowCredentials()` (comentario explícito en el código) más el hecho de que todas las mutaciones del SPA usan JSON/métodos no-simples (disparan preflight) y no se encontró `FormData`/`multipart` en frontend ni `IFormFile`/`[FromForm]` en la API que abrieran una vía de "simple request". No se localizó ningún token anti-forgery ni header custom exigido de forma consistente (sólo un header de CAPTCHA condicional en algunos endpoints). Como el requisito 3.5.1 sólo exige esa capa alternativa cuando la app *no* depende del preflight, y aquí sí depende de él por diseño, la evaluación correcta pasa a v5.0.0-3.5.2 (siguiente en cola), del cual esta ficha queda dependiente antes de poder cerrarse en PASS/NOT_APPLICABLE. Hallazgo de deuda (no defecto confirmado): la defensa CSRF entera depende de una única capa (CORS+credentials) sin capa independiente, frágil ante cambios futuros (nuevo endpoint multipart, relajación de CORS) y sin test que la proteja (ya señalado en la ficha 3.4.2). No se reabre el hallazgo de `analyze-attachment`/`X-Flow-Id` de la ficha 2.3.1 (revisado y descartado como caso CSRF porque el endpoint es `[AllowAnonymous]`). HEAD evaluado 23f2a90949c8011edad03c093e61f9dcc9816370; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 (gitlink verificado); hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.5.1.md; sin propuesta distribuida a worktree de API (no se verificó su existencia). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.5.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.5.2. No apliques fixes, commits ni pushs».

- v5.0.0-3.5.2 DOCUMENTADO / NEEDS_REVIEW: de las 24 acciones mutantes de `api-admisiones`, 22 sostienen la dependencia del preflight declarada en 3.5.1 porque su body no-nulo requerido sólo puede poblarse con `Content-Type: application/json` (no-safelisted) y el binder fallido dispara el auto-400 de `[ApiController]`; 2 con body nulleable (`UploadPersonPhoto`, `UploadPersonIdentityDocument`) logran el mismo efecto por un chequeo explícito propio. Hallazgo central: `POST /auth/logout` y `POST /auth/refresh-token` (`AuthController`, ambos `[AllowAnonymous]`) no declaran ningún parámetro, por lo que una simple request CORS (sin preflight, cualquier `Content-Type` safelisted) ejecuta la acción completa; su única fuente de identidad es la cookie ambiente. `CookieAuthenticationHelper.ResolveSameSite` fija `SameSite=Strict` sólo en `Production`/`Preproduction` (`EnvironmentExtensions.IsProductionLike`); en cualquier otro ambiente (`Development`, y presumiblemente `Testing`/`Desa` a juzgar por los 4 hosts no-productivos de la allowlist CORS de `AddCorsPolicy`) la cookie es `SameSite=None`, por lo que viajaría en un `POST` cross-site simple (p. ej. un `<form>` HTML) sin que CORS lo impida — CORS sólo bloquea la lectura de la respuesta, no la ejecución server-side de una simple request, confirmado por el orden de pipeline (`UseCors` no bloquea, sólo omite headers). Es una manipulación de sesión (forzar logout o refresh de tokens) sin exposición de datos, no un compromiso de confidencialidad. `SameSite=Strict` en Production/Preproduction neutraliza el hallazgo ahí de forma independiente al preflight — capa no documentada como control CSRF en fichas previas 3.3.1/3.4.2/3.5.1. Resultado NEEDS_REVIEW (no FAIL): falta confirmar el `ASPNETCORE_ENVIRONMENT` real de los hosts Testing/Desa (no versionado) y ejecutar una prueba de tráfico reproducible, ninguna autorizada en esta fase. Pendientes: revisar cuerpo completo de los 14 `[HttpGet]` (heredado de 3.5.1) por side-effects ocultos, inventario de clientes externos, y actualizar la lectura conjunta de 3.5.1 con este hallazgo. HEAD evaluado 3a74decdf28d84e552265b224b061970905122a4; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 (gitlink verificado); hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.5.2.md; sin propuesta distribuida a worktree de API (no se verificó su existencia). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.5.2.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-3.5.3. No apliques fixes, commits ni pushs».

- v5.0.0-3.5.3 DOCUMENTADO / NEEDS_REVIEW: cierra el pendiente heredado de 3.5.1/3.5.2 (E8) sobre revisar el cuerpo completo de las acciones `[HttpGet]`. Inventario corregido: 15 acciones `[HttpGet]` en `api-admisiones` (no 14 como contó 3.5.1), en `EnrollmentsController`/`CatalogsController`/`PersonController`/`ScholarshipsController`; un agente de exploración dedicado trazó las 15 hasta su caso de uso/servicio y confirmó que las 15 son lecturas puras (sólo `uow.*.GetBy*`/`GetAll*`, sin `Save`/`Add`/`Update`/`Remove`, sin escritura de caché de negocio, sin integración externa mutante; la única llamada saliente desde un GET, hacia la API interna de pagos, es a su vez un GET). Las 24 acciones mutantes ya confirmadas en 3.5.2 usan exclusivamente POST/PUT. Sin hallazgo de defecto: ninguna funcionalidad sensible está expuesta bajo un método HTTP seguro. Hallazgo de deuda (no nuevo): no se implementa la vía alternativa de validación de `Sec-Fetch-*` en ningún punto del código (grep sin resultados fuera de la documentación de planificación y el JSON fuente), consistente con la dependencia de una única capa ya señalada en 3.5.1/3.5.2. Resultado NEEDS_REVIEW (no PASS) por ADR-002: evidencia estática completa pero no determinística/reproducible; pendientes runtime de `HEAD`/`OPTIONS` efectivo, API de pagos externa no inspeccionada y corrección de conteo en 3.5.1. HEAD evaluado fb73943ca1c61693d34d1dd098306a463a7a5d7b; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 (no reinspeccionado, ya confirmado sin HTTP propio); hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-3.5.3.md; sin propuesta distribuida a worktree de API porque se verificó con `git worktree list` que no existe ninguno vigente y no hay hallazgo de defecto que remediar. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-3.5.3.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-4.1.1. No apliques fixes, commits ni pushs».

- v5.0.0-4.1.1 DOCUMENTADO / NEEDS_REVIEW: el camino estándar de respuestas (`ObjectResult`/`ActionResult<T>` vía `[ApiController]` + `ProducesAttribute("application/json")` global en `AddApiControllers`) queda con `Content-Type` + `charset` garantizado por el formateador JSON por defecto de ASP.NET Core; único sink de bytes top-level es `PersonController.GetPersonPhoto` (`image/jpeg` fijo). Hallazgo concreto: `ModelBindingErrorLoggingMiddleware.HandleBadRequestProductionLikeAsync`/`HandleBadRequestDevelopmentAsync` reemplazan el **cuerpo** de toda respuesta 400 que no sea ya un `OperationResult` por un nuevo JSON, pero nunca fijan `context.Response.ContentType`, dejando potencialmente colgado el header `application/problem+json; charset=utf-8` (default de `[ApiController]` sin `AddProblemDetails` custom) sobre un cuerpo que ya no es Problem Details; ninguno de los 7 tests existentes que ejercitan ese branch verifica el header, sólo el body. `ExceptionHandlingMiddleware` fija `ContentType` sin charset pero su propio test documenta que `WriteAsJsonAsync` lo corrige a `application/json; charset=utf-8` (comportamiento de framework, no verificado en runtime por este agente). Pendientes: confirmación runtime del header heredado real en 400, ejecución de tests existentes, configuración IIS/`mimeMap` no versionada para el hosting estático del frontend, y resolución de la deuda ya documentada en 3.2.1 sobre `GetPersonPhoto` (referenciada, no reabierta). HEAD evaluado 873e861df7d5c2abd1cc71bee1b16d7dcb6f600e; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-4.1.1.md; sin propuesta distribuida a worktree de API porque `git worktree list` confirma que no existe ninguno vigente (único worktree es el propio monorepo). Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar salvo el propio AVANCE.md. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-4.1.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-4.4.1. No apliques fixes, commits ni pushs» (v5.0.0-4.1.2/4.1.3 son L2 y se saltan).

- v5.0.0-4.4.1 DOCUMENTADO / NOT_APPLICABLE: inventario negativo explícito de WebSocket en los tres componentes. Sin `new WebSocket(`/`ClientWebSocket`/`socket.io`/`SignalR` en código fuente propio de `admisiones`, `api-admisiones` ni `Core`; sin paquete SignalR/WebSocket en ningún `.csproj`; sin dependencia WebSocket productiva en `admisiones/package.json` (los únicos hits del repo — `websocket-driver`/`faye-websocket`/`websocket-extensions`/`@nestjs/websockets` — son transitivos de `package-lock.json` para tooling de build/dev, no del runtime de producción); sin configuración de infraestructura versionada (`.yml`/`.yaml`/`.bicep`/`.tf`/`web.config`/`*.conf`) que declare un servicio o proxy WebSocket. El requisito exige TLS (`wss://`) para conexiones WebSocket que no existen en este sistema, por lo que no aplica un control sobre un mecanismo ausente; se registra NOT_APPLICABLE en vez de PASS porque no hay nada que verificar positivamente. Límite: configuración de hosting/proxy de producción fuera del repo no auditable estáticamente; si se introduce WebSocket en el futuro, debe reabrirse esta ficha. HEAD evaluado 9c6db81078b22eaa9ae01c704b6dce2abe9f0f16; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial validado sobre el blob. Sin commit: AVANCE.md y revisiones/v5.0.0-4.4.1.md; sin propuesta distribuida porque no hay hallazgo de defecto ni código a remediar. Sin fixes/builds/tests/scans/commits/pushs; árbol limpio al iniciar. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-4.4.1.md`. Continuar solo con un nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-5.2.1. No apliques fixes, commits ni pushs» (v5.0.0-4.4.2/4.4.3/4.4.4, v5.0.0-4.2.1/4.3.1/4.3.2 y v5.0.0-5.1.1 son L2 y se saltan; v5.0.0-4.1.4/4.1.5 son L3).

- v5.0.0-5.2.1 DOCUMENTADO / NEEDS_REVIEW: Kestrel limita el JSON a 16 MiB; Core limita imágenes a 5 MiB y PDF/documentos a 10 MiB antes de persistencia o análisis Azure; reconocimiento agrega un tope configurable. Pendientes: límite efectivo del hosting, rechazo HTTP y capacidad de memoria/latencia bajo concurrencia, valor de configuración en producción y ejecución de tests. HEAD 01b373ffca7841c9cd78892552cb5a6ccb0b6979; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash JSON oficial validado. Sin fixes/builds/tests/scans/commits/pushs. Archivos de esta iteración sin commit: AVANCE.md y revisiones/v5.0.0-5.2.1.md. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-5.2.2. No apliques fixes, commits ni pushs».

- v5.0.0-5.2.2 DOCUMENTADO / NEEDS_REVIEW: FileValidator en Core identifica el tipo por magic bytes permitidos pero no coteja la extensión del nombre; un test existente espera éxito con bytes PNG y nombres .jpg, .exe o sin extensión. Afecta rutas L1 de reconocimiento de registro y foto/identidad: el primero usa el archivo para extraer datos y los demás pueden persistir nombre/tipo discordantes; frente/dorso se nombran .jpg aun al aceptar PNG. Adjuntos de becas sanitizan extensión pero no la correlacionan con bytes; no se localizó caller HTTP activo. Pendientes: prueba negativa reproducible en endpoints y Core, comportamiento de Azure, consumidores de nombres/BLOB y contenido malformado. HEAD evaluado c0c75b0f2866bf9f95ebc4324bf81b8cd155fccc; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash JSON oficial validado. Sin fixes/builds/tests/scans/commits/pushs. Archivos de esta iteración sin commit: AVANCE.md y revisiones/v5.0.0-5.2.2.md; propuesta sólo central, sin worktree fix/owasp disponible en clones originales. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-5.3.1. No apliques fixes, commits ni pushs».

- v5.0.0-5.3.1 DOCUMENTADO / NEEDS_REVIEW: no se halló camino de archivos no confiables a carpeta pública en los flujos revisados; foto/identidad/becas usan BLOB, reconocimiento temporal Redis, Core genera imagen en memoria. API sirve `/contracts` desde JSON versionados en ambientes no productivos; frontend publica assets del build. Falta mapa de montaje/permisos/handlers y artefacto desplegado para excluir otros caminos o probar ejecución HTTP. HEAD 4de0815537e19f4fe7d4ea5e10c31c7dda21ada5; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash oficial verificado sobre blob. Sin tests, runtime, scans, fixes, commits ni pushs. Archivos sin commit: AVANCE.md y revisiones/v5.0.0-5.3.1.md. Sin propuesta distribuida al no haber fix confirmado ni worktree externo vigente. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-5.3.2. No apliques fixes, commits ni pushs».

- v5.0.0-5.3.2 DOCUMENTADO / NEEDS_REVIEW: nombres de archivo de registro/persona/becas llegan a metadatos BLOB/Redis/DTO, no a paths locales de I/O observados. Los paths del API para contratos/comentarios XML usan raíz de proceso y literales; la URL Azure usa endpoint de configuración y parámetros codificados. Pendientes: consumidores/deployment externos de nombres persistidos, mapa de almacenamiento y validación reproducible si aparece un sink de path o URI. HEAD 4de0815537e19f4fe7d4ea5e10c31c7dda21ada5; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7 limpio; hash JSON oficial validado. AVANCE.md y ficha 5.3.1 ya estaban staged al iniciar y se preservaron; esta iteración deja AVANCE.md modificado en working tree y revisiones/v5.0.0-5.3.2.md sin seguimiento, sin staging propio. Sin tests/runtime/scans/fixes/commits/pushs. Sin propuesta distribuida, no hay fix concreto ni worktree externo vigente. Continuar sólo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-6.1.1. No apliques fixes, commits ni pushs».

## Contexto que debe preservarse

- Rama del monorepo: fix/owasp. Commit de preparación documental: 0c46d660 (usar git para obtener SHA completo y HEAD vigente).
- La fuente JSON tiene SHA-256 BCDBEC214D70ABCFAD9284A31D4F9E5134305831D628AAD3AA85D7E26626CB35; estaba sin commit al preparar el plan.
- Estado histórico de la sesión de v5.0.0-1.1.2: JSON fuente y source/README.md en staging, kb/.obsidian/graph.json sin seguimiento. En la verificación final, Git solo muestra los Markdown de esta iteración y graph.json; HEAD verificado: 490bf64f63cefe7b79bcd5322b5dd56e04a996ab. El agente no alteró staging ni ejecutó commits; comprobar siempre el estado real.
- Los clones originales contienen cambios locales y tienen refs distintas de los subtrees. Por defecto se evalúa el monorepo; los planes por clone no son evidencia de esa ref.
- Core consumido inicialmente: 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Verificar el gitlink vigente antes de inspeccionarlo.
- Los resultados del piloto existente y las revisiones OWASP Top 10 no se importaron como resultados de esta revisión punto por punto.

## Reglas para actualizar este archivo

Una fila por ID oficial; no eliminar ni reordenar requisitos. Seleccionar solo L1 EN_CURSO o el primer L1 POR_REVISAR según ITERACION.md; saltar L2 conservando sus estados y fichas. Al agotar L1, detenerse sin iniciar L2 automáticamente. Mantener los contadores L1 y L2 separados, además del total general. Mantener los punteros y contadores consistentes con la cola y las fichas. Cuando un punto esté DOCUMENTADO, completar resultado y enlace de ficha; si queda EN_CURSO, documentar la próxima acción exacta arriba y en su ficha. Los gaps de puntos documentados se conservan en sus fichas para revisión posterior.

## Cola canónica

| ID | Nivel | Avance documental | Resultado de seguridad | Ficha |
| --- | --- | --- | --- | --- |
| v5.0.0-1.1.1 | L2 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.1.1.md) |
| v5.0.0-1.1.2 | L2 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.1.2.md) |
| v5.0.0-1.2.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.2.1.md) |
| v5.0.0-1.2.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.2.2.md) |
| v5.0.0-1.2.3 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.2.3.md) |
| v5.0.0-1.2.4 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.2.4.md) |
| v5.0.0-1.2.5 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.2.5.md) |
| v5.0.0-1.2.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.2.7 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.2.8 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.2.9 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.2.10 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-1.3.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.3.1.md) |
| v5.0.0-1.3.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.3.2.md) |
| v5.0.0-1.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.7 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.8 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.9 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.10 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.11 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.3.12 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-1.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.5.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-1.5.1.md) |
| v5.0.0-1.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.5.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-2.1.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-2.1.1.md) |
| v5.0.0-2.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.2.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-2.2.1.md) |
| v5.0.0-2.2.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-2.2.2.md) |
| v5.0.0-2.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-2.3.1.md) |
| v5.0.0-2.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-2.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.4.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.1.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.2.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.2.1.md) |
| v5.0.0-3.2.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.2.2.md) |
| v5.0.0-3.2.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.3.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.3.1.md) |
| v5.0.0-3.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.4.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.4.1.md) |
| v5.0.0-3.4.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.4.2.md) |
| v5.0.0-3.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.4.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.5.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.5.1.md) |
| v5.0.0-3.5.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.5.2.md) |
| v5.0.0-3.5.3 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-3.5.3.md) |
| v5.0.0-3.5.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.5.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.5.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.5.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.5.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.6.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.7.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.7.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.7.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.7.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.7.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.1.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-4.1.1.md) |
| v5.0.0-4.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.1.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.2.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.2.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-4.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.1 | L1 | DOCUMENTADO | NOT_APPLICABLE | [Ficha](./revisiones/v5.0.0-4.4.1.md) |
| v5.0.0-4.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-5.2.1.md) |
| v5.0.0-5.2.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-5.2.2.md) |
| v5.0.0-5.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.2.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.3.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-5.3.1.md) |
| v5.0.0-5.3.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-5.3.2.md) |
| v5.0.0-5.3.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.1.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-6.1.1.md) |
| v5.0.0-6.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.1 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-6.2.1.md) |
| v5.0.0-6.2.2 | L1 | DOCUMENTADO | NEEDS_REVIEW | [Ficha](./revisiones/v5.0.0-6.2.2.md) |
| v5.0.0-6.2.3 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.4 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.5 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.6 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.7 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.8 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.9 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.10 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.11 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.12 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.3.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.3.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.3.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.3.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.4.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.4.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.4.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.5.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.5.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.5.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.5.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.5.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.5.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.5.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.6.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.6.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.6.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.6.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.7.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.7.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-6.8.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.8.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.8.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.8.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.2.3 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.2.4 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.4.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-7.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.4.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.5.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.5.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-7.6.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-7.6.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-8.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-8.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-8.1.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-8.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-8.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-8.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-8.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-8.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-8.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-8.3.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-8.3.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-8.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-8.4.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-9.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-9.1.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-9.1.3 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-9.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-9.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-9.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-9.2.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.2.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.3 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.4 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.5 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.7 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.8 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.9 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.10 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.11 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.4.12 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.4.13 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.4.14 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.4.15 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.4.16 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-10.5.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.5.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.5.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.5.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.6.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.6.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.7.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.7.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-10.7.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.1.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-11.3.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-11.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.3.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-11.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.5.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.5.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.6.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-11.6.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.7.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-11.7.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-12.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-12.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-12.1.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-12.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-12.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-12.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-12.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.1.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.1.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.2.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.2.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.2.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.3.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.3.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-13.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.4.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-13.4.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-13.4.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-14.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-14.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.2.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-14.2.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-14.2.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-14.2.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-14.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-14.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-14.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-15.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.1.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.1.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-15.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.3.7 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-15.4.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.4.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.4.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-15.4.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-16.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.2.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.2.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.5.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.5.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-16.5.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.1.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.2.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.2.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.2.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.2.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.2.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.2.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-17.3.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-17.3.2 | L2 | POR_REVISAR | PENDING | — |
