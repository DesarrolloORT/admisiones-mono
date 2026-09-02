---
slug: /flujos/inscripciones
title: Flujo de inscripciones
description: Pasos, campos condicionales, payloads y casos borde de la inscripción.
businessId: admisiones.inscripciones
sourcePaths:
  - src/app/features/enrollments/
  - src/app/features/catalogs/
  - src/app/features/home/pages/dashboard/
  - src/app/features/home/components/
  - src/app/features/home/api/home.api.ts
  - src/app/shared/process-flow/
---

import SourceLink from '@site/src/components/SourceLink';

# Enrollments de punta a punta

> Tipo: reference

Fuentes de verdad: `src/app/features/enrollments/**` y
`api-admisiones/WebApiAdmisiones/AppLogic/AppLogic.Enrollments/**`. Este documento
describe el comportamiento desplegado del frontend y del backend conectado a esta
rama: entrada desde el dashboard, encuesta, identidad, confirmación, reactivación,
pago, persistencia e integraciones. No uses el raw value de los formularios como
contrato: el payload HTTP sale de `enrollment-flow-mappers.ts` y OpenAPI es la
autoridad del wire contract.

## Acciones y evidencia end-to-end

| Acción visible               | Frontend                                                                                                                                                                                                                                           | HTTP                                                                   | Backend                                                                                                                                                                                                                                                |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Cargar Mis carreras          | <SourceLink repo="frontend" path="src/app/features/home/api/home.api.ts">HomeApi</SourceLink>                                                                                                                                                      | `GET /person/enrollments`                                              | <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.People/UseCases/GetMyEnrollments.cs">GetMyEnrollments</SourceLink> → vistas Fresco 1/2 y 3/4                                                                                       |
| Continuar propuesta          | <SourceLink repo="frontend" path="src/app/features/enrollments/facades/enrollment-proposal.ts">EnrollmentProposalFacade</SourceLink> → <SourceLink repo="frontend" path="src/app/features/enrollments/api/enrollments.api.ts">adapter</SourceLink> | `POST /enrollments/product-interest`                                   | <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.Enrollments/UseCases/RegisterProductInterest.cs">RegisterProductInterest</SourceLink> → Oracle + cola Tivenos                                                                      |
| Confirmar datos personales   | <SourceLink repo="frontend" path="src/app/features/enrollments/facades/enrollment-survey.ts">EnrollmentSurveyFacade</SourceLink> → adapter                                                                                                         | Documento/foto → encuesta → `POST /enrollments/confirm-pre-enrollment` | <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.People/">People</SourceLink> + <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.Enrollments/UseCases/ConfirmPreEnrollment.cs">ConfirmPreEnrollment</SourceLink> |
| Elegir forma de pago         | <SourceLink repo="frontend" path="src/app/features/enrollments/facades/enrollment-payment.ts">EnrollmentPaymentFacade</SourceLink> → adapter                                                                                                       | `POST /enrollments/start-payment`                                      | <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.Enrollments/UseCases/Payments/StartEnrollmentPayment.cs">StartEnrollmentPayment</SourceLink> → API interna de Enrollments y Pagos                                                  |
| Reactivar desde Mis carreras | <SourceLink repo="frontend" path="src/app/features/home/components/dashboard-quick-actions/dashboard-quick-actions.ts">DashboardQuickActions</SourceLink>                                                                                          | `POST /enrollments/reactivate`                                         | <SourceLink repo="backend" path="WebApiAdmisiones/AppLogic/AppLogic.Enrollments/UseCases/ReactivateEnrollment.cs">ReactivateEnrollment</SourceLink> → `ConfirmPreEnrollment`                                                                           |

Las rutas y shapes HTTP son autoridad de OpenAPI. Las reglas internas del servidor viven en el backend; si una evidencia contradice esta página, registrar un bloque **Drift detectado** hasta alinear ambos repositorios.

## Límites y componentes

- Todos los endpoints de `person`, `enrollments` y `catalogs` usados aquí requieren
  la identidad del usuario autenticado. El backend obtiene `personId` del JWT/cookie;
  nunca acepta la persona desde el request.
- `EnrollmentsApi` es la frontera anticorrupción del front: traduce los modelos
  generados en inglés a tipos propios de la feature. No hay caché HTTP que invalidar:
  `ApiHttpClient` manda cada request a la red, así que detalle, identidad, encuesta y
  reglamento siempre leen el estado vigente.
- Oracle persiste interés, encuesta, aceptación del reglamento, imágenes, inscripciones
  y método de reserva. La cola de Tivenos se registra en la misma transacción que el
  interés o la actualización de bachillerato; la entrega efectiva ocurre fuera del
  request.
- La API interna de **Enrollments y Pagos** confirma preinscripciones, consulta
  carritos, cobra cuenta personal y genera URLs de pasarela. Su handler agrega las
  credenciales de servicio. Admisiones traduce fallos de red a `API_NETWORK`/503,
  timeout mayor a dos minutos a `API_TIMEOUT`/504 y fallos inesperados a
  `API_UNEXPECTED`/500.
- La inscripción corporativa no llama a la API de pagos: crea un trámite de bandeja por
  oferta, con vencimiento a cinco días, y devuelve `waiting = true`.

## Pasos y escenarios

El flujo tiene 3 pasos. El **paso 1** (Propuesta académica) avanza con
`POST /enrollments/product-interest`. El **paso 2** (Informacion personal)
guarda primero los cambios de identidad, luego llama a
`POST /enrollments/initial-survey` cuando corresponde y finalmente a
`POST /enrollments/confirm-pre-enrollment`. El **paso 3** (Confirmacion /
pago) llama a `POST /enrollments/start-payment`; el detalle operativo forma parte de esta misma página.

Antes de entrar al paso 2 se consulta `GET /enrollments/initial-survey`. Su campo
`canAnswerSurvey` (`isEligibleForSurvey` en el contrato de feature) es el **único** dato que
decide qué se muestra: en `true` se ven Educación, decisión académica, experiencia ORT,
identidad y reglamento, con lo ya respondido precargado y editable; en `false` solo identidad
y reglamento. Actualización profesional es la excepción de siempre: muestra situación
laboral, identidad y reglamento en los dos casos, para preguntar si la inscripción es
corporativa.

```mermaid
flowchart TD
  A[Propuesta academica] -->|Interés registrado| B{canAnswerSurvey}
  B -->|true| C[Encuesta + identidad + reglamento]
  B -->|false| D[Identidad + reglamento]
  C --> F[Identidad + reglamento]
  D --> F
  C -->|Cada cambio de respuestas| N[Guardar delta de encuesta]
  C -->|Salir del proceso| N
  F --> G[Subir cambios de documento y foto en paralelo]
  G -->|Ambos OK y canAnswerSurvey true| H[Guardar delta de encuesta]
  G -->|canAnswerSurvey false / AP / sin cambios pendientes| I
  G -->|Error| M[Reabrir identidad sin check]
  H -->|OK| I[Confirmar preinscripcion]
  H -->|Error| K[Mostrar error y permanecer]
  I -->|Confirmada| J[Seleccion de pago]
  I -->|Error determinístico 400/403/404| K[Mostrar error y permanecer]
  I -->|Error ambiguo 0/409/5xx| O[Pantalla de espera y salida]
  J --> L[Pago / estado terminal]
```

Las cargas de identidad son condicionales: un archivo precargado y no modificado no se
vuelve a enviar. AP nunca guarda encuesta.

**`canAnswerSurvey` es el único dato que decide todo sobre la encuesta inicial.** Llega en
`GET /enrollments/initial-survey` y es por persona:

- `true`: los campos de encuesta se muestran editables (con lo respondido precargado, incluso
  si el backend marca la encuesta como completa) y se guarda siempre que haya algo nuevo.
- `false`: ya la respondió. El paso 2 muestra **solo** verificación de identidad y reglamento,
  y no se llama al `POST initial-survey` desde ningún lugar.

El estado de carga fallida (`GET` caído) se trata como "no sabemos": pantalla de error con
reintento y nunca un POST a ciegas.

### Hidratación de la encuesta

`details` y `initial-survey` son resolvers de la misma ruta: corren en paralelo y los dos
están resueltos antes de que exista la página, así que entre ellos no hay orden. `details` no
aporta nada a la encuesta (solo Paso 1 y pago).

El orden que sí importa es encuesta vs. **catálogos**: la encuesta trae ids y los catálogos que
les dan sentido se piden recién al activarse el paso 2. Reglas:

- **La encuesta se parchea UNA sola vez**, al derivar el estado inicial, con los catálogos
  todavía vacíos. No hay un segundo parcheo: nada puede pisar lo que el usuario respondió
  mientras los catálogos cargaban.
- **Ninguna limpieza dependiente de catálogo corre sin ese catálogo cargado.** "No hay
  opciones" no significa "la opción dejó de existir": con el catálogo vacío la orientación (y
  cualquier valor equivalente) se conserva tal cual llegó del backend. La limpieza vuelve a
  aplicar en cuanto el catálogo está, y también cuando el catálogo falla el valor se conserva.
- El snapshot de "lo último que el backend confirmó" se toma justo después de ese parcheo
  único, así que el primer delta no reenvía nada que el backend ya tenga.
- Los selects reciben su valor antes que sus opciones. `ResponsiveSelect` proyecta su propio
  `ort-select-trigger` derivado de `selectedOptions()` porque el trigger interno de
  `ort-select` recorre las `ort-option` proyectadas y esa lista no es reactiva: sin trigger
  propio, un valor escrito antes que las opciones deja el campo visualmente vacío en desktop.

Para niveles 1/2 el `POST initial-survey` es un **upsert parcial de las claves que manda el
front**: una clave ausente significa "sin cambios" y una clave presente en `null` significa
"borrar". El front nunca manda la encuesta entera: manda el delta contra lo último que el
backend confirmó (ver `## Payload de encuesta inicial`).

Los valores "sin responder" viajan como `null`, nunca como el valor por defecto del control.
Un campo cuyo payload no pueda expresar "sin responder" queda atrapado: si su valor vacío
coincide con una respuesta válida (el caso de `currentlyInSecondary` con `false`), responder
esa opción no produce delta, el backend nunca se entera y al volver el campo aparece vacío.

Momentos de guardado, todos con `canAnswerSurvey` en `true` y ninguno bloqueante:

1. **En el momento en que una sección de respuestas queda completa** (Educación, Decisión
   académica, Experiencia ORT): el mismo instante en que el accordion muestra el check, sin
   pulsar Continuar. Con el check ya puesto, **cada cambio posterior en esa sección vuelve a
   guardar**. Una sección incompleta no postea. Identidad y reglamento no aportan campos de
   encuesta, y `work-situation` es solo de AP.
2. **Al pulsar Continuar** en una sección: reintento de lo que quedó pendiente por un guardado
   fallido. Con todo guardado el delta está vacío y no hay request.
3. **Al cerrar el paso 2** (Continuar de la última sección), antes de confirmar la
   preinscripción.
4. **Al salir** del paso 2. Salir no pide confirmación: el guardado sale al vuelo, no bloquea
   la navegación y un fallo no muestra error. Es el único momento que rescata lo respondido en
   una sección que todavía no quedó completa, porque el guardado por sección solo dispara con
   la sección válida.

En los cuatro casos, si no hay cambios pendientes no hay request. **No hay ningún guardado
disparado por un timer.** El guardado es oportunista: corre en segundo plano sin loader y un
fallo no muestra error, porque el cambio queda pendiente y viaja en el intento siguiente. Los
POST no se solapan: mientras uno está en vuelo los disparos nuevos se descartan y su cambio
entra en el delta siguiente (`exhaustMap`, nunca `switchMap`: cancelar un POST deja el
servidor en estado desconocido).

Los cuatro campos de **escritura libre** de la encuesta —"¿Cuál?" de universidad de educación
superior, "¿Cuál?" de universidad consultada, institución educativa cuando la secundaria fue en
el exterior y "¿Cuántas veces?" de recursado— usan `updateOn: 'blur'`: actualizan el form al
salir del campo, no por tecla, así que escribir no dispara un POST por letra. La contracara es
que el check de la sección y el error de esos campos también aparecen al salir del campo.

> **Techo asumido:** no hay borrador local. Lo respondido en una sección que todavía no quedó
> completa se pierde si el usuario recarga o cierra la pestaña sin usar "Salir". Persistir el
> **delta pendiente** en `sessionStorage` (keyed por `idProducto`+`idProceso`, borrado al
> confirmar el POST y en logout) es la forma barata de cerrarlo si algún día hace falta; se
> descartó por ahora porque el dato está a la vista y son datos personales.

## Intención de entrada × estado × encuesta

Cómo arranca el flujo depende de la **intención de entrada** (decidida por la URL,
no por el backend) combinada con el estado de la inscripción (`Detalle` o respuesta
de `Reactivar`) y el de la
encuesta inicial (`GET/POST /enrollments/initial-survey`, que es **por persona**). La derivación es pura
y está fijada por la tabla ejecutable `models/enrollment-entry.spec.ts`; esta
matriz es su lectura de negocio.

- **Nueva** (`/inscripciones`, sin params): el Paso 1 arranca **siempre virgen y
  editable**, aunque exista una encuesta previa en progreso. La encuesta previa solo
  se reutiliza al llegar al Paso 2 (prellena respuestas), nunca precarga el Paso 1 ni
  reposiciona el flujo. `POST /enrollments/product-interest` se envía al continuar.
- **Retomar** (`/inscripciones?idProducto=X&idProceso=Y`, desde el panel):
  llegar con producto y proceso válidos **ya prueba que la inscripción existe**, así
  que el interés está registrado y el **Paso 1 nunca se muestra**. Al hacer clic en
  "Continuar inscripción", la tarjeta guarda sus `offeringIds` y `enrollmentIds` en
  `sessionStorage`; la URL conserva producto, proceso, `estado` y `nivel` (el `estado`, solo
  hasta el primer avance). El Paso 1 queda precargado y
  deshabilitado (no se vuelve a enviar `POST /enrollments/product-interest`) y el flujo arranca en el Paso
  2 o en la pantalla que corresponda al estado.

  | Estado del detalle                  | Dónde arranca / qué muestra                 |
  | ----------------------------------- | ------------------------------------------- |
  | En proceso (con o sin encuesta)     | Paso 2 (sección activa de la encuesta)      |
  | `Detalle` caído / sin estado útil   | Paso 2 con la precarga mínima de los params |
  | Pago pendiente / Pendiente sin seña | Paso 3 (elegir medio de pago)               |
  | Pago pendiente / Pendiente con seña | Pantalla de referencias de pago (reserva)   |
  | Confirmada                          | Pantalla de éxito terminal                  |
  | A la espera / desconocido           | Pantalla "Inscripción en proceso"           |

  En ambas filas, si el monto de la seña es `0`, el flujo omite la selección de pago y
  muestra directamente la pantalla con el mensaje de contacto a oficina (ver "Estados frontend").

  Los estados terminales pintan su pantalla por `payment` y dejan el flujo en el Paso 3:
  así el Paso 1 no es el paso corriente en ninguna combinación. Solo params **inválidos**
  (faltantes, no numéricos, ≤ 0) degradan a **nueva**, porque sin ellos no hay
  inscripción que retomar.

  **Precarga del Paso 1 al retomar.** Producto y proceso salen de la URL/Detalle; las
  ofertas salen del contexto guardado por la tarjeta en `sessionStorage`. Un enlace de
  Los `offeringIds` del contexto guardado mantienen precedencia; si esa fuente no
  existe, `detail.interests` aporta el fallback
  (una oferta por seminario en Actualización profesional). El
  contrato del `Detalle` **no expone `intakeId` ni `shiftId`** en ningún bloque, así
  que el comienzo y el turno solo llegan como texto; por eso la precarga llena
  `shift` y `seminars` con los `offeringIds` (el payload de `POST /enrollments/confirm-pre-enrollment`
  lee uno u otro según el tipo de propuesta) y toma el comienzo del param `idProceso`
  (el control `intake` guarda el `admissionProcessId`). Si el `Detalle` no llegó,
  los params conservan producto, proceso y ofertas, por lo que el Paso 2 puede confirmar
  igualmente. La
  selección académica de una encuesta previa nunca se aplica al Paso 1 al retomar. Si el
  catálogo de carreras falla y el nivel queda `null`, el tipo de propuesta se completa
  después desde el nivel de la carrera (`AcademicProposalSelection`).

  **Pago y resumen al retomar AP.** La respuesta de
  `EnrollmentPreEnrollmentResponse.seminars` es la fuente principal de los IDs a cobrar y
  de las filas del resumen. Si esa respuesta no trae el bloque, `POST /enrollments/start-payment` usa como
  fallback los `enrollmentIds` guardados en sesión, y el resumen usa los seminarios
  seleccionados del catálogo. El contexto se valida contra producto/proceso y conserva
  solo enteros positivos sin duplicados; el backend mantiene la validación final de pertenencia.

- **El flujo solo avanza.** Pasar de paso es un hecho ya registrado en el backend
  (Paso 1 ⇒ `POST /enrollments/product-interest`, Paso 2 ⇒ `POST /enrollments/confirm-pre-enrollment`), así que **no hay
  vuelta atrás entre pasos**: no se vuelve del Paso 2 al 1 ni del Paso 3 al 2. Lo único
  que retrocede es la navegación **dentro** del Paso 2 (secciones de la encuesta y
  lector de reglamento), y el botón de volver solo aparece cuando hay algo hacia atrás.
  Los pasos no están en la URL, así que la flecha del navegador no vuelve un paso: sale
  del flujo, y al reingresar el estado se vuelve a derivar del backend.

  **La URL identifica la inscripción, no su estado.** Como el paso no viaja en la URL,
  recargar re-deriva todo, y los params de entrada son una foto del panel: `estado` y
  `modo` describen la inscripción tal como estaba al entrar. En cuanto el flujo avanza esa
  foto queda vieja, así que el primer avance reescribe la URL (`replaceUrl`) con
  `idProducto`, `idProceso`, `idOferta` y `nivel`, y **sin** `estado` ni `modo`. Sin esa
  reescritura, recargar en el Paso 3 volvía al Paso 2 —con `estado=En proceso` el Detalle
  ya no encuentra ese estado (404) y el flujo cae en su fallback— y ahí "Continuar"
  reintentaba `POST /enrollments/confirm-pre-enrollment` sobre una preinscripción ya
  confirmada: `400` sin salida. En una inscripción nueva pasaba lo mismo por el otro lado:
  sin params, la recarga volvía al Paso 1 con el interés ya registrado. Se reescribe una
  sola vez por entrada y solo si algo cambia, porque el router usa
  `onSameUrlNavigation: 'reload'` y navegar a la misma URL recargaría la ruta.

- **Reactivar** (`/inscripciones?idProducto=X&idProceso=Y&modo=reactivar`): el botón
  del dashboard hace `POST /enrollments/reactivate` con **todas** las anotaciones de
  la tarjeta en `enrollmentIds` (una en niveles 1/2; una por seminario en los paquetes
  de Actualización profesional, que se dan de baja y se reactivan en bloque), y
  devuelve el mismo contrato que `POST /enrollments/confirm-pre-enrollment`. El frontend conserva transitoriamente esa respuesta y
  los IDs de las nuevas inscripciones: `isWaiting` muestra la pantalla informativa,
  seña `0` muestra la reserva y el resto abre la selección de pago. El resolver no
  llama a `GET /enrollments/details` en esta navegación; si la respuesta ya no está
  disponible por recarga o acceso directo, usa `Detalle` como fallback. Los
  `getDetail` posteriores a `POST /enrollments/start-payment` se mantienen para completar coordinación, materias
  o referencias de reserva que el POST de reactivación no devuelve.

  El backend verifica **todo o nada** que cada ID pertenezca a la persona, esté dado de
  baja y tenga oferta; deduplica ofertas y delega en la confirmación normal. No marca la
  baja original como “ya reactivada” ni usa clave de idempotencia, por lo que una
  repetición incierta debe resolverse consultando el estado antes de reintentar.

### Detalle como read model

`GET /enrollments/details?productId=X&admissionProcessId=Y&status=Z` busca primero la
vista Fresco 1/2 y luego 3/4. Devuelve `INS_DET_01`/404 si no encuentra estado y
completa solo el bloque correspondiente:

`status` es el `enrollmentStatus` que ya devuelve `GET /person/enrollments`; se agregó
porque `productId` + `admissionProcessId` dejaron de identificar una única inscripción
cuando la persona tiene más de una tarjeta con ese mismo par (por ejemplo, una
inscripción confirmada y una reactivación posterior del mismo producto/proceso). El
front lo manda siempre que lo conoce, pero el param sigue siendo opcional: un link
viejo sin `estado` se llama sin `status` y el backend resuelve como antes. El frontend
lo propaga por el query param `estado` en la URL de `/inscripciones` (mismo mecanismo
que `idProducto`/`idProceso`/`modo`), leído por `enrollmentDetailResolver` y por
`EnrollmentPaymentFacade` al retomar el pago desde el panel. Es un disambiguador de **un
solo uso**: describe el estado con el que se entró, así que el primer avance del flujo lo
saca de la URL (ver "El flujo solo avanza") y desde ahí el Detalle se consulta sin
`status`.

`nivel` viaja por el mismo mecanismo y con la misma regla de opcionalidad: es el
`productLevelId` que ya devuelve `GET /person/enrollments`, y existe para que el
resolver no reconstruya el nivel del producto consultando los tres tipos de propuesta.
No se envía al backend: solo alimenta `productLevelId` del read model de entrada. El
resolver lo descarta si no corresponde a un tipo conocido y cae al catálogo, así que un
valor manipulado no puede hacer más que elegir la rama de UI equivocada; la pertenencia
de los IDs la revalida el backend igual que siempre.

| Estado                               | Bloque                                                       | Fuente               |
| ------------------------------------ | ------------------------------------------------------------ | -------------------- |
| `En proceso`                         | `inProgress` con intereses/ofertas                           | Oracle               |
| `A la espera` o desconocido          | solo `status`                                                | Oracle               |
| `Pago pendiente`, sin método         | `pendingPayment` con carritos, saldo, seña y vencimiento     | Oracle + API interna |
| `Pago pendiente`, con Abitab/Paganza | `minimumDeposit` con método, documento, persona y seña total | Oracle + API interna |
| `Confirmada`                         | `confirmed` con persona, coordinadores y materias por oferta | Oracle               |

Si falla la consulta de carritos, el detalle propaga el error de la API interna; no
degrada silenciosamente a un bloque incompleto. El front, en cambio, captura el error
del resolver al retomar y abre el paso 2 con los IDs de la URL/contexto.

## Regla general de valores ocultos

Cuando un campo padre cambia, la UI actualiza validadores y puede ocultar
campos hijos. En varios casos el valor crudo del hijo queda en el form, pero
el payload lo ignora y envia `null` cuando el padre indica que no aplica.

Excepciones que si limpian valores:

- Propuesta academica: cambiar `proposalType` limpia `degreeProgram`, `intake` y
  `shift`; cambiar `degreeProgram` limpia `intake` y `shift`; cambiar `intake`
  limpia `shift`.
- Bachillerato: cambiar `highSchoolYear` limpia `orientation` si el valor anterior
  ya no existe en las orientaciones vigentes para ese año.
- Institucion educativa: cambiar `state` limpia `educationalInstitution`
  solo si el valor anterior no existe en el nuevo catalogo cargado.
- Pago: cambiar `paymentMethod` a algo distinto de `bank-account` limpia
  `bank`.

## Matriz de campos condicionales

| Condicion                               | Campo afectado                          | Validacion visible      | Limpieza al cambiar                                                     | Payload si no aplica                             |
| --------------------------------------- | --------------------------------------- | ----------------------- | ----------------------------------------------------------------------- | ------------------------------------------------ |
| `studiesHighSchool = studying`          | `highSchoolYear`                        | Requerido               | Conserva valor crudo                                                    | `highSchoolYear = null`                          |
| Año con orientaciones                   | `orientation`                           | Segun opciones vigentes | Limpia si la opcion deja de existir                                     | `highSchoolOrientationId = null`                 |
| `repeatsHighSchoolYear = yes`           | `highSchoolYearRepeatCount`             | Entero mayor que 0      | Conserva valor crudo                                                    | `highSchoolYearRepeatCount = null`               |
| `highSchoolLocation = 1`                | Departamento e institucion como selects | Ambos requeridos        | Limpia una opcion inexistente solo si el usuario cambio el departamento | Institucion libre en `highSchoolInstitutionName` |
| `highSchoolLocation != 1`               | Institucion como texto libre            | Texto requerido         | Puede conservar el id anterior                                          | `highSchoolInstitutionId = null`                 |
| `higherEducationStatus = 1`             | `higherEducationUniversities`           | Seleccion requerida     | Conserva valor crudo                                                    | `higherEducationUniversityIds = null`            |
| Universidad seleccionada incluye `0`    | Campo de universidad "Otro"             | Texto requerido         | Conserva valor crudo                                                    | Lista de otros `null`                            |
| Formacion de madre o padre es `5` o `6` | `motherOrtDegree` o `fatherOrtDegree`   | Si/no requerido         | Conserva valor crudo                                                    | Egresado ORT `null`                              |
| `otherUniversities = yes`               | `researchedUniversities`                | Seleccion requerida     | Conserva valor crudo                                                    | Ids y otros `null`                               |
| Experiencia ORT = `yes`                 | Rating o medios correspondiente         | Requerido               | Conserva valor crudo                                                    | Valoracion o medios `null`                       |
| Identidad completa desde backend        | `isIdentityCorrect`                     | Checkbox requerido      | No se envia                                                             | Solo controla validez de UI                      |
| `paymentMethod = bank-account`          | `bank`                                  | Requerido               | Se limpia al elegir otro metodo                                         | Se envía como `sistarbancBankId`                 |

Las referencias Figma se agregan a esta matriz cuando diseño entrega una URL
verificada al nodo exacto. No se publican enlaces generales ni placeholders.

## Casos borde consolidados

- Ocultar un control no garantiza que su valor crudo se elimine; los mappers son
  la frontera que envia `null` cuando no aplica.
- Cambiar la institucion de Uruguay a exterior puede conservar temporalmente el
  id anterior como texto.
- Una opcion dependiente se conserva mientras siga existiendo en el catalogo
  nuevo; si desaparece, se limpia.
- La encuesta completa oculta sus secciones, pero identidad y reglamento
  mantienen sus propias reglas.
- El paso de pago llama al backend; los pagos externos terminan en `external-payment-pending` hasta que exista confirmación automática.
- `resultado=en-proceso` fuerza el estado terminal "Inscripcion en proceso" para
  cualquier metodo.

## Paso 1: propuesta academica

El paso tiene cuatro campos en cascada. `proposalType` es una constante local
filtrada por niveles disponibles (`1` carrera universitaria, `2` tecnicatura,
`3` actualizacion profesional); no se envia directo al backend pero filtra las
carreras disponibles. `degreeProgram` usa el `idProducto` como string, proveniente de
`GET /catalogs/degree-programs`, y el adapter lo envía como `productId`. `intake`
usa el `idProceso` como string, proveniente de
`GET /catalogs/intakes?degreeProgramId=<degreeProgram>`, y el adapter lo envía como
`admissionProcessId`. `shift` usa el `idOferta` como string, proveniente de
`GET /catalogs/shifts?degreeProgramId=<degreeProgram>&admissionProcessId=<intake>`,
y el adapter lo envía dentro de `offeringIds`.

Cuando un catálogo devuelve **una sola opción**, el campo se precarga: no hay elección real
que pedir. Aplica a `intake`, `shift` y al campo de seminarios/horario de AP; `degreeProgram` no
se precarga, porque dispararía toda la cascada de catálogos sin intención del usuario. La
precarga vive en un `effect` de `AcademicProposalSelection` y nunca pisa un valor ya elegido,
así que el prefill de una encuesta previa y el de retomar quedan intactos. Precargar
`intake` sí encadena `GET /catalogs/shifts`, igual que si lo hubiera elegido la persona.

Al continuar se llama a `POST /enrollments/product-interest` con:

```json
{
  "offeringIds": ["Number(shift)"],
  "admissionProcessId": "Number(intake)",
  "productId": "Number(degreeProgram)"
}
```

El backend valida persona, producto admisible, proceso habilitado y cada oferta:
debe existir, pertenecer al producto y al proceso, y estar abierta con su supraoferta
en estado final. Para niveles 1/2 rechaza una inscripción previa o pendiente; para
niveles 3/4 permite varias ofertas concurrentes, pero rechaza repetir una oferta ya
registrada. El interés, sus ofertas y la fila de cola hacia Tivenos se guardan en una
única transacción Oracle. Un reintento luego de éxito no es idempotente: devuelve
`GEN_IP_06`/409 (o `GEN_IP_04`/`GEN_IP_05` según el caso) y no duplica el registro.

## Actualización profesional (niveles 3 y 4)

Cuando el tipo de propuesta es `3` (Actualización profesional, productos con
`productLevelId` 3 o 4) el flujo cambia de estructura. La detección vive en una
única fuente reactiva: `AcademicProposalSelection.isProfessionalUpdate`
(`isProfessionalUpdateType` en `academic-proposal.ts`), sincronizada también en
precarga/retomar vía el back-fill de `getAcademicProposalTypeByLevel`.

### Dashboard "Mis carreras": contrato agrupado y tarjetas

`GET /person/enrollments` devuelve una lista **agrupada por producto/proceso**:
cada elemento trae `productId`, `productFullName`, `admissionProcessId`,
`productLevelId`, `enrollmentStatus`, `hasSeminars`, `paymentDueDate` y un array
`enrollments[]` con las ofertas concretas (`enrollmentId`, `offeringId`,
`offeringDescription`, `shiftId`, `intakeId`, `intakeStartDate`, `intakeName`,
`shiftName`, `referenceDate`).
`HomeApi.toEnrollmentSummaries()` mapea cada grupo a uno o más
`EnrollmentSummary`; la fuente de la regla de nivel es `isProfessionalUpdateLevel`
(`src/app/features/catalogs/models/academic-proposal.ts`).

Los grupos cuyo `enrollments[]` llega vacío o `null` no representan una
inscripción y se omiten. Si no queda ninguna inscripción ni beca, la home muestra
el estado inicial con las dos cards ilustradas.

Un `404` de `GET /person/enrollments` significa que la persona no tiene registros y
se normaliza a una colección vacía. Los demás errores conservan el estado de error
de la home. `GET /person/scholarships` fue eliminado del contrato: la home ya no lo
consulta y compone `scholarships: []`.

> **Drift detectado:** testing devuelve `404` cuando no hay inscripciones, pero el
> endpoint generado solo documenta respuestas `200` y `400`.

- **Niveles 1 y 2:** sin cambios funcionales. Una tarjeta por inscripción (un
  `EnrollmentSummary` por item de `enrollments[]`), título = `degreeProgramName`,
  fila "Comienzo" y el CTA por `status` vía `DashboardQuickActions`.
- **Niveles 3 y 4 (Actualización profesional):** una tarjeta **por paquete** (un
  `EnrollmentSummary` por grupo). El título sigue siendo `degreeProgramName`.
  `DashboardCard.enrollmentsCount()` (`enrollment.seminars.length || 1`) decide
  qué se muestra debajo del título: con un solo seminario (o sin seminarios, niveles
  1/2) se muestra la fila "Comienzo" con su `intakeName`; con 2+ seminarios se
  reemplaza por el texto `"Anotado a N seminarios"`, sin desglose por seminario.

> Reemplaza la regla anterior: antes la tarjeta de niveles 3/4 usaba
> `offeringDescription` del primer item como título. Ese comportamiento queda
> superado por `degreeProgramName` + conteo de seminarios.

### Alert de pago pendiente y fecha límite

`EnrollmentSummary` incluye `paymentDueDate: string | null` (fecha cruda de la
API; el formateo a `dd/MM/yyyy` se hace en la UI reutilizando
`formatPaymentDeadline`). El alert del dashboard se muestra cuando alguna
inscripción está en `Pago pendiente` y su título/detalle/navegación los arma
`buildPendingPaymentSummary()` (`src/app/features/home/models/enrollment-summary.ts`).
El título va en plural apenas hay **más de una inscripción pendiente**, sin
importar si comparten fecha. El detalle, en cambio, deduplica fechas repetidas
(`Set`) y menciona cada fecha distinta una sola vez:

| Inscripciones pendientes | Fechas distintas usables | Título                              | Detalle del alert                                                  |
| ------------------------ | ------------------------ | ----------------------------------- | ------------------------------------------------------------------ |
| 1                        | 1                        | `Inscripción pendiente de pago.`    | `Realizá el pago antes del 15/07/2026.`                            |
| 1                        | 0 (sin fecha informada)  | `Inscripción pendiente de pago.`    | `Consultá el detalle desde Mis carreras.` (texto genérico)         |
| 2+                       | 1 (misma fecha)          | `Inscripciones pendientes de pago.` | `Las mismas vencerán el 15/07/2026.`                               |
| 2+                       | 2                        | `Inscripciones pendientes de pago.` | `Las mismas vencerán los días 15/07/2026 y 20/07/2026.`            |
| 2+                       | 3+                       | `Inscripciones pendientes de pago.` | igual, unido con `, ` y `y` antes de la última (`Intl.ListFormat`) |
| 2+                       | 0 (sin fecha informada)  | `Inscripciones pendientes de pago.` | `Consultá el detalle desde Mis carreras.` (texto genérico)         |

La flecha del alert (`actionIcon`, evento `actionTriggered`) sólo se renderiza y
navega a `/inscripciones?idProducto=&idProceso=` cuando hay **una única**
inscripción en `Pago pendiente` en total. Con 2 o más pendientes —tengan la
misma fecha o fechas distintas— no hay un destino de pago único, así que la
flecha se oculta (`buildPendingPaymentSummary().navigable === false`).

La tarjeta (`dashboard-card`) ya no repite la fecha límite de pago por separado:
para eso está el alert del dashboard. Lo que muestra cada tarjeta debajo del
título depende únicamente de `enrollmentsCount()`, ver sección anterior.

`GET /person/enrollments` expone la fecha únicamente como
`MyEnrollmentsResponse.paymentDueDate`, a nivel de grupo. El adapter la mapea a
`EnrollmentSummary.paymentDueDate`; si no viene, resuelve a `null` y el alert usa
el texto genérico.

### Drift detectado (resuelto)

El adapter (`HomeApi.toEnrollmentSummaries()`) mapeaba la forma plana vieja
contra `MyEnrollmentsResponse`, el contrato agrupado que el backend
ya devolvía. Como todos los campos de ese DTO son opcionales, la respuesta
nueva era estructuralmente asignable y TypeScript compilaba sin error, pero
`enrollmentId`, `intakeId`, `shiftId`, `intakeName`, `shiftName` y
`offeringDescription` habían pasado al item interno (`enrollments[]`) y
resolvían a `undefined` → `0`/`''`. Efecto observable: el botón "Reactivar
inscripción" quedaba inerte y la fila "Comienzo" salía vacía. Resuelto por el
mapeo agrupado descrito arriba.

Un segundo drift dejó el mismo botón inerte después de ese arreglo: la tarjeta
dejó de pasarle `enrollmentId` a `DashboardQuickActions`, que era la condición de
`reactivatesFlow`. Resuelto unificando la reactivación sobre `idEnrollments`
(el set que la tarjeta ya calculaba), que además cubre los paquetes de
Actualización profesional con más de una anotación.

### Paso 1 AP: Programa + Seminarios

- El selector de carrera se muestra como **Programa** (misma UI y validaciones;
  la terminología es data-driven en `ACADEMIC_PROPOSAL_TYPES.terminology`).
- No hay selects de Comienzo ni Turno. Al elegir un programa aparece el
  **select de Seminarios** (oculto hasta entonces), cada uno con su fecha de
  comienzo debajo. Cambiar de programa limpia los seminarios elegidos.
- El programa manda el modo de selección: `hasSeminar === true` en
  `GET /catalogs/degree-programs` habilita **multi-select**; cualquier otro valor deja
  un **select simple** de una sola oferta
  (`AcademicProposalSelection.allowsMultipleSeminars`). El control `seminars`
  guarda siempre `string[]`, así que el resto del flujo no cambia.
- `hasSeminar` decide además la terminología del campo
  (`AcademicProposalSelection.seminarLabel` / `seminarErrorText`, que también alimentan el
  resumen de errores del paso): con seminarios se rotula **Seminario** y el error es
  "Seleccioná al menos un seminario"; sin seminarios el programa ofrece una sola oferta,
  así que el campo se rotula **Próximo comienzo** y el error va en singular
  ("Seleccioná un próximo comienzo", derivado del rótulo en minúscula).
- Catálogo: el `idProceso` del producto y su `idProducto` llaman
  `GET /catalogs/shifts`; el resultado llena el multiselect de seminarios.
- Cada opción muestra `offeringDescription` y, debajo, `referenceDate`.
  `toAcademicSeminarOption` normaliza esa fecha a `dd/MM/yyyy`: el catálogo la manda como
  ISO con hora fija (`2026-10-16T00:00:00`) y la opción muestra solo la fecha. Sin fecha
  no se pinta la descripción.
- Al continuar se llama `POST /enrollments/product-interest`. El contrato de
  feature y el HTTP son arrays (`idOfertas` → `offeringIds`), por lo que se envían
  todas las ofertas elegidas en una sola operación.

### Paso 2 AP reducido

`getVisibleSections(canAnswerSurvey, isProfessionalUpdate)` devuelve
`PROFESSIONAL_UPDATE_SURVEY_SECTIONS` (`work-situation`, `identity`, `regulation`)
sin mirar `canAnswerSurvey`: las tres se muestran siempre para AP, para preguntar
en todos los casos si la inscripción es corporativa. Un clamp en
`EnrollmentSurveyFacade` reposiciona la sección activa si dejó de ser visible o
cambió la primera sección aplicable.

La sección `work-situation` contiene una sola pregunta,
**¿A título de quién deseás realizar la inscripción?**, y solo se muestra en AP. La
selección es obligatoria y se representa como `isCorporate`: título personal es
`false` y corporativa es `true`. Los tipos 1/2 no ven la sección y envían
`isCorporate = false`.

**AP no envía `POST /enrollments/initial-survey`** (guard en `savePartial`, que
solo corre al cerrar el paso 2). `isCorporate` se envía únicamente en
`POST /enrollments/confirm-pre-enrollment`. Una inscripción personal continúa al paso 3; una
corporativa termina en la pantalla "Inscripción corporativa pendiente" y espera
que la empresa acredite el pago.

### Retomar AP "En proceso"

AP nunca postea `GET/POST /enrollments/initial-survey`, así que exigir una encuesta prefilled para
arrancar en el paso 2 dejaba estas inscripciones en el paso 1 vacío. Hoy
`deriveRetomar` nunca muestra el paso 1 (ver la matriz de retomar) y la tarjeta
guarda todas sus ofertas en `sessionStorage` antes de navegar. `detail.interests`
queda como fallback para entradas sin ese contexto; los params `idOferta` se leen solo
por compatibilidad con enlaces generados anteriormente.
Ojo con los productos que no están en `GET /catalogs/degree-programs` (o cuyo `Detalle`
falla): el paso 2 se abre igual, pero el tipo de propuesta queda vacío hasta que el
catálogo resuelva el nivel, así que las secciones visibles pueden arrancar como las del
flujo tradicional.

`productLevelId` decide el tipo de propuesta del paso 1 y, con eso, las secciones
visibles del paso 2. Al retomar desde el panel llega por el query param `nivel`: la
tarjeta ya lo recibió como `productLevelId` en `GET /person/enrollments`, así que el
resolver lo usa directo y **no** consulta el catálogo. Solo cuando ese param falta o
no corresponde a un tipo conocido (link viejo, entrada directa, valor manipulado) el
resolver cruza el Detalle contra `GET /catalogs/degree-programs` para reconstruirlo,
lo que cuesta una consulta por tipo de propuesta. Si el catálogo falla queda `null`:
el paso 2 igual se abre y `AcademicProposalSelection` completa el tipo cuando el
catálogo carga. `EnrollmentProcessFacade` aplica el `academicPrefill` después del slice de
encuesta y bloquea el paso 1 (`disableForResume`).

El Detalle no informa si la inscripción es corporativa, por lo que al retomar un
estado pendiente se mantiene la pantalla genérica "Inscripción en proceso".

### Resumen de pago: Programa + Seminarios

`GET /enrollments/details`, `POST /enrollments/confirm-pre-enrollment` y su
`pendingPayment` devuelven un array `enrollments[]` (`EnrollmentOfferingSummary`:
`enrollmentId`, `offeringId`, `intake`, `shift`, `offeringDescription`) junto al
`summary` plano (`EnrollmentHeader`: `productId`, `degreeProgram`,
`paymentDueDate`). El adapter (`EnrollmentsApi.toSeminars()`) mapea ese
array completo a `EnrollmentPreEnrollmentResponse.seminars` (y a
`EnrollmentPendingPaymentDetail.seminars` para "retomar"), además de seguir
colapsando `enrollments?.[0]` en los campos planos (`summary`, `enrollmentId`) que
usa el resto del flujo.

`POST /enrollments/start-payment` recibe `enrollmentIds: number[]`, así que el pago cobra el
paquete completo: `EnrollmentPaymentFacade.paymentEnrollmentIds()` prioriza los
`seminars[].enrollmentId` y el `enrollmentId` plano de la respuesta. Si ambos faltan
al retomar, usa los `enrollmentIds` positivos y deduplicados guardados por la tarjeta
en `sessionStorage`. Si tampoco quedan IDs válidos, el pago no se envía y la pantalla
muestra "No pudimos identificar la inscripción pendiente.".

En la pantalla de pago (`enrollment-confirmation-step`), el "Resumen de inscripción"
usa `AcademicProposalSelection.isProfessionalUpdate` para decidir el layout:

- **Niveles 1 y 2:** las 3 filas de siempre (Carrera, Comienzo, Turno), sin cambios.
- **Niveles 3 y 4 (AP) con 2+ seminarios:** una sola fila **Programa** (mismo ícono e
  ídem fallback de `Carrera`) seguida de una sección **Seminarios** con una fila por
  elemento de `seminars[]` (nombre, comienzo, turno). Si la confirmación omite ese
  array, se usan como fallback las ofertas seleccionadas del catálogo de seminarios.
  Esta lista vive fuera de `summaryItems()` para no romper
  `enrollment-success-step.html`, que usa `summaryItems()[0]` como título y
  `summaryItems().slice(1)` para el resto.
- **Niveles 3 y 4 (AP) con un solo seminario:** no hay nada que desglosar, así que el
  resumen se lee como los niveles 1/2: filas **Programa** + **Comienzo** (el `intake`
  del único seminario) y **sin** sección Seminarios. La decisión es por conteo
  (`seminars.length === 1`), no por `hasSeminar`, igual que
  `DashboardCard.enrollmentsCount()` en el panel (ver línea ~255). En el facade,
  `selectedSeminars()` es la fuente única: alimenta `summaryItems()` y
  `seminarsSummary()` solo devuelve filas con 2+.
- El botón "Pagar" envía el pago directo: no hay diálogo de confirmación intermedio.

### Regla de cardinalidad y drift backend

La regla de negocio confirmada es: **niveles 1/2 admiten exactamente una oferta**;
solo niveles 3/4 pueden enviar varias. El frontend cumple esa cardinalidad y los
paquetes AP se procesan completos en `product-interest`, `confirm-pre-enrollment`,
`reactivate` y `start-payment`.

> **Drift detectado:** `SelectedOfferingsCompatibility` hoy no rechaza dos ofertas de
> nivel 1/2 si comparten producto, comienzo y turno. El contrato esperado es una sola;
> falta endurecer esa validación en backend. Para niveles 3/4 se permiten varias del
> mismo producto y no hay pago parcial del paquete desde esta UI.

## Paso 2: informacion personal

### Educacion

`studiesHighSchool` controla si se muestra `highSchoolYear`. Si pasa a
`no-studying` el año queda crudo en el form pero no se envia; el backend recibe
`highSchoolYear = null`. Si cursa secundaria y el año elegido trae
orientaciones en `educacion.aniosBachillerato[].orientaciones`, se muestra
`orientation` directamente. No hay selector intermedio de tipo nacional o
internacional. Si el año no trae orientaciones o cambia a uno donde la seleccion
anterior no existe, se envia `highSchoolOrientationId = null`.

`repeatsHighSchoolYear` es requerido. Si vale `yes`, se muestra
`highSchoolYearRepeatCount` y se exige un numero mayor a 0. Si vale `no`, la
cantidad puede quedar cruda en el form pero el backend recibe
`highSchoolYearRepeatCount = null`.
`highSchoolLocation` decide el control de institucion educativa. Con valor `1`
(Uruguay) se muestran `state` e `educationalInstitution` como select, ambos requeridos.
**El departamento no se guarda en la encuesta**: el backend lo deriva de la institucion
elegida y lo devuelve de solo lectura en `secondaryInstitutionStateId` (contrato v11), que
es lo que permite precargar el combo al retomar y con ese valor pedir
`GET /catalogs/institutions?countryId=1&stateId=<state>`. Viene `null` si la secundaria fue
en el exterior, si todavia no hay institucion elegida, o si la institucion no tiene
departamento cargado; en ese ultimo caso el usuario tiene que volver a elegirlo.
Al hidratar, una institucion que no aparezca en el catalogo de su departamento **se
conserva**: solo se limpia cuando el departamento lo cambia el usuario. El
backend recibe `finalHighSchoolYearLocationId = 1`,
`highSchoolInstitutionId = Number(educationalInstitution)` y
`highSchoolInstitutionName = null`. Con valor `2` (exterior)
`educationalInstitution` pasa a texto libre; el backend recibe
`finalHighSchoolYearLocationId = 2`, `highSchoolInstitutionId = null` y
`highSchoolInstitutionName = <texto>`. Cambiar de Uruguay a exterior no limpia
el control, por lo que puede conservar el id anterior como texto.

`higherEducationStatus = 1` muestra el multiple `higherEducationUniversities`.
Si la seleccion incluye `Otro` (`0`), se muestra el `ortInput`
`otherHigherEducationUniversity` y el backend recibe
`otherHigherEducationUniversities = [texto]`. Si cambia a otro valor o no esta
seleccionado `0`, la seleccion y el texto pueden quedar crudos pero el backend
recibe `higherEducationUniversityIds = null` o
`otherHigherEducationUniversities = null`, segun corresponda.
`motherEducation` con valor `5` o `6` muestra `motherOrtDegree` (si/no). Si cambia
a otro valor el titulo queda crudo pero se envia `null`. Lo mismo aplica a
`fatherEducation` y `fatherOrtDegree`.

### Decision academica

`otherUniversities = yes` muestra el multiple `researchedUniversities`. Si la
seleccion incluye `Otro` (`0`), se muestra el `ortInput`
`otherResearchedUniversity` y el backend recibe `universidadConsideradaOtros =
[texto]`. Si pasa a `no`, la seleccion queda cruda pero el backend recibe
`consideredUniversityIds = null` y `otherConsideredUniversities = null`.
`decisionCertainty` no tiene hijos condicionales; `1` es decidido/a y `2` es con
dudas, y se envia como `decisionLevelId`.
Los campos directos de esta seccion son `degreeProgramDecisionYear` (`degreeProgramDecisionYearId`),
`decisionSupport` (`decisionSupportId`), `ortDecisionYear` (`ortDecisionYearId`) y
`ortReasons` (`ortChoiceReasonIds`), todos con ids de catalogo.

### Experiencia con ORT

Cuatro campos booleanos (si/no) tienen ratings condicionales: `advisingMeeting`,
`visitedWebsite`, `visitedCampus` y `recallsAdvertising`. En cada caso, si el campo padre
vale `yes` se muestra el rating o multiple correspondiente (`advisingRating`,
`websiteRating`, `campusRating`, `advertisingChannels`). Si cambia a `no`, el
valor hijo queda crudo pero el backend recibe `null` para ese campo. Los ratings
envian el numero elegido y las etiquetas salen del catalogo de valoraciones si esta
disponible.

### Situacion laboral

La encuesta inicial ya no tiene preguntas laborales: el backend dejó de publicar el
catálogo `situacionLaboral` y los campos `trabajaActualmente` y `tipoJornadaId`. La
sección `work-situation` sobrevive solo en AP y únicamente pregunta la titularidad
de la inscripción (`isCorporate`, ver [Paso 2 AP reducido](#paso-2-ap-reducido)).

### Identidad

Se precargan datos desde `GET /person/identity-document` y `GET /person/photo`. La
seccion requiere frente y dorso del documento (`File`, `image/jpeg` o `image/png`),
selfie (`File`, `image/jpeg` o `image/png`) y `documentExpiration` (`Date`).

Si frente, dorso, vencimiento y selfie ya vinieron completos desde el backend, se
muestra el checkbox `Verifico que la identidad es correcta` (`isIdentityCorrect`)
y la seccion queda invalida hasta marcarlo. Si alguno vino `null`, no se pide ese
checkbox: el flujo normal pide completar solo el dato faltante.

Si el usuario elimina un archivo, queda `null` y la seccion vuelve a invalida. Si
sube o cambia archivos, el front guarda esos cambios antes de confirmar la
preinscripcion. `isIdentityCorrect` es solo de UI y no se envia al backend.

Al cerrar el paso 2, si se toco frente/dorso o cambio el vencimiento se llama a
`POST /person/identity-document`; si se toco la selfie se llama a
`POST /person/photo`.

El backend no confía en extensión ni nombre: frente, dorso y foto aceptan
`.jpg`, `.jpeg` o `.png`, validan contenido por magic bytes y limitan cada imagen
a 5 MiB. Documento exige ambos lados y una fecha no vencida; crea o reemplaza los
dos slots temporales y actualiza el vencimiento de la persona. La foto crea o
reemplaza el slot de foto definitivo. Antes de confirmar, el servidor acepta un par
temporal completo y vigente o, como fallback, un par definitivo completo y vigente;
falta de lado, vencimiento o blob vacío se rechazan aunque el front haya validado.

### Reglamento

`GET /enrollments/student-regulations` indica si el reglamento ya fue aceptado.
Si ya fue aceptado, la UI oculta el checkbox y marca `acceptsRegulation = true`
automaticamente; el backend recibe `acceptedRegulation = true`. Si no fue aceptado,
se muestra un checkbox requerido y el backend recibe `acceptedRegulation = true`
solo si el usuario lo marca.

## Payload de encuesta inicial

Se envía con `POST /enrollments/initial-survey` en los tres momentos listados arriba, y
**solo con las claves que cambiaron**.

Cómo se sabe qué cambió (`diffInitialSurveyPayload` en `models/enrollment-flow-mappers.ts`):

- La facade guarda un **snapshot de lo último que el backend confirmó**, construido con el
  mismo `buildInitialSurveyPayload` que arma el envío. Se toma al aplicar el estado inicial
  del flujo: para una encuesta prellenada, justo después de parchear los formularios con los
  datos del backend, así lo que vino de backend no cuenta como cambio del usuario.
- Antes de cada envío se compara el payload actual contra el snapshot, clave por clave
  (arrays por contenido y orden; nunca por _truthiness_, porque `0` y `false` son respuestas
  válidas distintas de `null`). Si el delta es vacío, no hay request.
- El snapshot **solo avanza con la confirmación del backend**, y solo con las claves
  enviadas. Un POST fallido deja el cambio pendiente y vuelve en el delta siguiente; un
  cambio hecho mientras hay un POST en vuelo tampoco se pierde (el trigger se descarta, no
  el dato).
- No se usa el `dirty` de los controles: hay claves del payload que agregan varios controles
  y el prellenado con `patchValue` ensucia todo el formulario.

Semántica en el backend: **clave ausente = sin cambios**, **clave presente en `null` =
borrar**. Por eso el delta incluye los `null` (el usuario desmarcó una opción, o un
validador condicional vació el campo dependiente).

El adapter traduce las claves de feature a las del wire con la tabla
`SURVEY_PAYLOAD_TO_WIRE` (`api/enrollments.api.ts`), que es la fuente única del renombrado
(`intakeId` → `admissionProcessId`, `highSchoolOrientationId` → `highSchoolTrackId`,
`currentlyStudiesHighSchool` → `currentlyInSecondary`, `finalHighSchoolYearLocationId` →
`lastSecondaryYearLocationId`, `visitedOrtCampus` → `visitedOrtFacilities`, entre otros) y
chequea en compilación los dos lados contra `SaveInitialSurveyRequest`. La lista de abajo
usa los nombres **de feature** (`EnrollmentInitialSurveyPayload`) y describe cómo se calcula
cada valor a partir de los formularios.

- `degreeProgramId`: `Number(degreeProgram)`.
- `intakeId`: `Number(intake)`.
- `highSchoolOrientationId`: `Number(orientation)` si cursa secundaria y el año elegido tiene orientaciones.
- `highSchoolYear`: `Number(highSchoolYear)` si `studiesHighSchool = studying`.
- `repeatsHighSchoolYear`: `yes -> true`, `no -> false`.
- `highSchoolYearRepeatCount`: cantidad solo si `repeatsHighSchoolYear = yes`; si no, `null`.
- `fatherOrGuardianEducationLevelId`: `Number(fatherEducation)`.
- `motherOrGuardianEducationLevelId`: `Number(motherEducation)`.
- `degreeProgramDecisionYearId`: `Number(degreeProgramDecisionYear)`.
- `ortDecisionYearId`: `Number(ortDecisionYear)`.
- `researchedOtherUniversities`: `yes -> true`, `no -> false`.
- `otherUniversitiesInfoLine1` y `Linea2`: sin campo UI, siempre `null`.
- `decisionSupportId`: `Number(decisionSupport)`.
- `highSchoolInstitutionId`: `Number(educationalInstitution)` solo si `highSchoolLocation = 1`.
- `highSchoolInstitutionName`: texto de `educationalInstitution` si `highSchoolLocation != 1`.
- `finalHighSchoolYearLocationId`: `Number(highSchoolLocation)`.
- `priorHigherEducationStatusId`: `Number(higherEducationStatus)`.
- `decisionLevelId`: `Number(decisionCertainty)`.
- `hadOrtAdvising`: `yes -> true`, `no -> false`.
- `ortAdvisingRatingId`: rating si `advisingMeeting = yes`.
- `visitedOrtWebsite`: `yes -> true`, `no -> false`.
- `ortWebsiteRatingId`: rating si `visitedWebsite = yes`.
- `visitedOrtCampus`: `yes -> true`, `no -> false`.
- `ortCampusRatingId`: rating si `visitedCampus = yes`.
- `recallsOrtAdvertising`: `yes -> true`, `no -> false`.
- `isMotherOrGuardianOrtGraduate`: `motherOrtDegree` (`yes -> true`, `no -> false`), solo si `motherEducation` es `5` o `6`.
- `isFatherOrGuardianOrtGraduate`: `fatherOrtDegree` (`yes -> true`, `no -> false`), solo si `fatherEducation` es `5` o `6`.
- `consideredUniversityIds`: `researchedUniversities.map(Number)` solo si `otherUniversities = yes`; incluye `0` si selecciona `Otro`.
- `otherConsideredUniversities`: `[otherResearchedUniversity.trim()]` solo si `consideredUniversityIds` incluye `0`; si no, `null`.
- `higherEducationUniversityIds`: `higherEducationUniversities.map(Number)` solo si `higherEducationStatus = 1`; incluye `0` si selecciona `Otro`.
- `otherHigherEducationUniversities`: `[otherHigherEducationUniversity.trim()]` solo si `higherEducationUniversityIds` incluye `0`; si no, `null`.
- `ortAdvertisingIds`: `advertisingChannels.map(Number)` solo si `recallsAdvertising = yes`.
- `ortChoiceReasonIds`: `ortReasons.map(Number)`, `null` si no hay seleccion.

### Persistencia y finalización de la encuesta

`canAnswerSurvey` es por documento, no por inscripción. `GET initial-survey` lo devuelve en
`false` si la persona ya figura como Fresco, tiene la encuesta histórica o tiene una encuesta
de admisión completa.

El front **no interpreta ningún otro dato** para decidir: no mira el `status` de la respuesta
del POST, ni el flag `complete` de la encuesta, ni el estado de la preinscripción. Mientras
`canAnswerSurvey` sea `true` los campos siguen editables y cada cambio se guarda; en cuanto
es `false`, el paso 2 queda en identidad + reglamento y no hay más POST. Una encuesta que el
backend marca completa pero con `canAnswerSurvey = true` se muestra precargada y editable.

Si `GET initial-survey` falla (`load-failed`), el valor es desconocido y se trata como
`false`: no se postea a ciegas. El reintento manual de la pantalla de error vuelve a
consultarlo.

Cada guardado valida opciones fijas y catálogos dinámicos, resuelve producto/proceso
contra el interés activo y actualiza la encuesta y sus listas hijas en una sola
transacción. La completitud se calcula sobre lo ya persistido:

- con campos pendientes queda `temporal` y la respuesta enumera secciones/campos;
- sin pendientes queda `definitivo`, calcula el vencimiento de admisión en días
  hábiles y, si la persona cursa secundaria, crea o actualiza su bachillerato y
  encola la sincronización a Tivenos dentro de la misma transacción;
- si producto y proceso vienen juntos debe existir exactamente una oferta de interés;
  cero produce `INS_EI_53`/404 y más de una `INS_EI_54`/409. AP evita esta restricción
  porque no guarda encuesta.

## Confirmacion de preinscripcion

Despues de completar la ultima seccion del paso 2 el front vuelve a validar todas
las secciones visibles. Si alguna quedo invalida por navegacion manual o borrador,
vuelve a esa seccion y no llama al backend de confirmacion.

Orden de cierre:

1. En paralelo, `POST /person/identity-document` si se toco frente/dorso o cambio
   el vencimiento, y `POST /person/photo` si se toco la selfie.
2. `POST /enrollments/initial-survey` con el delta pendiente, solo si las cargas de
   identidad requeridas terminaron correctamente y `canAnswerSurvey` es `true`. Se **omite**
   si los guardados por sección ya persistieron todo (delta vacío).
3. `POST /enrollments/confirm-pre-enrollment` con:

```json
{
  "acceptedRegulation": "Boolean(acceptsRegulation)",
  "isCorporateEnrollment": "isCorporate para AP; false para los demás tipos",
  "selectedOfferingIds": "seminars.map(Number) para AP; [Number(shift)] para los demás"
}
```

El servidor vuelve a validar que cada oferta exista, esté abierta y tenga interés
activo. Para niveles 1/2 exige encuesta nueva `definitivo` y vigente, salvo que exista
la encuesta histórica; niveles 3/4 no requieren encuesta. La cardinalidad esperada es
una oferta para niveles 1/2 y una o más del mismo producto para niveles 3/4; ver el
drift backend señalado arriba.

La aceptación se persiste por persona + producto + comienzo. Si la persona ya aceptó
alguna vez, el backend puede crear la fila específica sin volver a exigir `true`; si
nunca aceptó, `false` devuelve `INS_CPI_02`/400. Esta escritura ocurre antes de llamar
a la API interna, por lo que queda persistida aunque la confirmación remota falle.

La rama normal llama una sola vez a
`ORTSecure/Enrollments/ConfirmarPreInscripcionMultiple` y mapea su éxito —incluido
un posible resultado parcial del sistema remoto— a la respuesta propia. No envía clave de
idempotencia: ante timeout o respuesta incierta, no se debe asumir que reintentar sea
seguro sin consultar primero `GET /enrollments/details` o el dashboard.

La rama corporativa solo admite niveles 3/4. En una transacción crea, por cada oferta,
un trámite/instancia de workflow y su relación estructurada; el primer paso queda
autocompletado y el segundo pendiente para el grupo responsable. Si una oferta falla,
se hace rollback de todo el paquete. El response no contiene pago ni resumen:
`confirmed = false`, `isWaiting = true`.

Si `isCorporateEnrollment === true`, una respuesta exitosa termina en la
pantalla de espera del pago empresarial sin abrir el paso de pago. Para las demás
inscripciones, `isWaiting === true` muestra "Inscripción en proceso" y el resto
avanza al paso de pago. Si Documento o Foto falla por HTTP,
`OperationResult.success === false` o `data === false`, no se guarda la encuesta:
se conserva la seleccion de archivos y se reactiva Verificacion de identidad sin
el check de completada.

### Fallo ambiguo de la confirmación

`confirm-pre-enrollment` no lleva clave de idempotencia y llama APIs externas, así que
un `0` (red/timeout), `409` o `5xx` deja el resultado **indeterminado**: la inscripción
pudo haber quedado creada. En ese caso el front no ofrece reintentar: cierra el flujo en
la pantalla terminal "Inscripción en proceso", con un único enlace a `/inicio`. Ahí se
muestra el copy genérico incluso en AP corporativa, porque el copy corporativo
("tu empresa deberá enviar la solicitud…") daría por hecha una inscripción que quizá no
existe.

Un error determinístico (`400`, `403`, `404`) mantiene el error inline en el paso 2 con
la opción de reintentar.

### Salir del flujo nunca se bloquea

El modal "¿Querés salir de la inscripción?" dispara el guardado del delta pendiente y
**navega sin esperar la respuesta**: la salida no puede fallar ni mostrar error. El POST va
adrede sin `takeUntilDestroyed`, porque navegar destruye el componente y cancelaría la
peticiòn en vuelo.

Solo se guarda **desde el paso 2** (`currentStep() === 'survey'`): fuera de él los
formularios de encuesta no se editan, y como el POST es un upsert de las claves que manda el
front, salir desde el paso 1 con los formularios vírgenes mandaría `null` en todo y borraría
respuestas previas.

Historia: antes de esto la salida intentaba guardar **y esperaba**, y el fallo típico —403
porque el backend ya había marcado la encuesta `definitivo`— dejaba al usuario **encerrado en
el flujo** con "No se pudo guardar la encuesta. Intentá nuevamente.". El guardado al salir
volvió, pero nunca vuelve a bloquear.

`EnrollmentSurveyFacade.savePartial()` (usado por el guardado al completar una sección, por el
Continuar, por el cierre del paso 2 y por la salida) llama a `POST /enrollments/initial-survey` solo si `canAnswerSurvey`
es `true`, no es AP y el delta no está vacío.

### El guard de `savePartial()`

`EnrollmentSurveyFacade.savePartial()` es el **único** punto que persiste la
encuesta, y corre solo al cerrar el paso 2 (`finishSurveyStep`). Llama a
`POST /enrollments/initial-survey` si la persona tiene derecho a encuesta, no
es AP, **y** `EnrollmentProcessStore.preEnrollmentResponse` sigue en `null`.
Una vez que `confirmPreEnrollment` respondio con éxito (paso 3, pago) ese
signal deja de ser `null` y `savePartial()` retorna `true` sin llamar al
backend: la encuesta ya quedo guardada como parte de la confirmacion y
reintentar el POST no aporta nada, solo puede fallar.

### Salir del flujo no confirma ni guarda

La X del header y "Salir del proceso" del rail emiten `closeFlow`, que llama a
`EnrollmentProcessFacade.exit()`: navega a `/inicio` y nada más. No hay diálogo
de confirmación intermedio y no se dispara ningún POST, así que la salida nunca
puede fallar ni dejar a la persona encerrada en el flujo. Mismo comportamiento
que el flujo de becas, que ya salía directo.

Contrapartida asumida: como el avance solo se persiste al cerrar el paso 2, lo
que quede a medio completar al salir se pierde. Lo ya confirmado en pasos
cerrados no se toca. No hay guard `CanDeactivate` ni `beforeunload`: el botón
atrás del browser y F5 salen igual que el botón.

## Paso 3: pago

El paso llama a `POST /enrollments/start-payment`. El adapter traduce los métodos propios de la UI al contrato backend y la fachada decide si termina confirmado, reservado o pendiente en una pasarela externa.

`GET /catalogs/banks` se difiere hasta entrar al paso de pago editable; no se
consulta al abrir Enrollments ni para reservas o inscripciones ya confirmadas.

## Pago: detalle operativo

## Contrato del paso

Payload feature:

```json
{
  "enrollmentIds": [1072704, 1072705],
  "paymentMethod": "bank-account",
  "sistarbancBankId": "brou"
}
```

Payload HTTP:

```json
{
  "enrollmentIds": [1072704, 1072705],
  "paymentType": "SISTARBANC",
  "sistarbancBankId": "brou"
}
```

Para niveles 1/2 el array debe tener un único ID; AP envía todos los IDs del paquete.
El backend rechaza listas vacías, IDs no positivos o inscripciones ajenas.

Mapping adapter:

| UI                 | API               | `sistarbancBankId` |
| ------------------ | ----------------- | ------------------ |
| `personal-account` | `CUENTA_PERSONAL` | `null`             |
| `abitab`           | `ABITAB`          | `null`             |
| `paganza`          | `PAGANZA`         | `null`             |
| `banred`           | `BANRED`          | `null`             |
| `geopay`           | `GEOPAY`          | `null`             |
| `bank-account`     | `SISTARBANC`      | código del banco   |

`tarjeta-credito` no queda como método activo hasta que el backend confirme un
`tipoPago` propio o su mapeo dentro de Sistarbanc.

`personal-account` solo se ofrece si la seña es positiva y el saldo de cuenta
corriente (`currentAccount.currentBalance` de `/enrollments/details` o de la
preinscripción) es distinto de `0`. Con saldo `0` la opción se oculta; con saldo
informado pero menor a la seña se muestra deshabilitada.

Comportamiento servidor:

- `CUENTA_PERSONAL` cobra todos los carritos en la API interna. Si responde éxito,
  el backend arma el detalle confirmado desde Oracle y devuelve
  `result = PAGO_CONFIRMADO`.
- `ABITAB`/`PAGANZA` escriben una fila de seña mínima por inscripción en una sola
  transacción y devuelven `METODO_GUARDADO`. Si alguna ya existe, toda la operación
  hace rollback y devuelve `INS_MP_04`/409; repetir no duplica reservas.
- `BANRED`/`GEOPAY`/`SISTARBANC` solicitan la URL de factura a la API interna y
  devuelven `URL_GENERADA`, URL base y parámetros cifrados separados. SISTARBANC
  exige banco (`INS_UF_03`/400).
- Cuenta personal y generación de factura no llevan clave de idempotencia local; la
  semántica de un reintento tras timeout depende de la API interna.

## Flujo por método

```mermaid
flowchart TD
  A[Inscripción con pago pendiente] --> B[POST /enrollments/start-payment]
  B --> C{Método}
  C -->|CUENTA_PERSONAL| D{Backend confirma pago}
  D -->|OK| E[Inscripción confirmada]
  D -->|Error| F[Permanece en pago con error]
  C -->|ABITAB / PAGANZA| G[Reserva / pago pendiente externo]
  C -->|BANRED / GEOPAY / SISTARBANC| H{Backend devuelve URL de pago}
  H -->|Sí| I[Front redirige a pasarela]
  H -->|No| J[Pago pendiente externo]
  I --> J
```

## Respuesta de `/enrollments/start-payment`

El adapter mapea `result`, `paymentUrl`, `encryptedParameters`, `messages` y el
bloque `confirmed` (número de estudiante, coordinación y materias) cuando el
backend confirma el pago en línea (p. ej. cuenta personal). Con eso la pantalla de
éxito pinta el detalle sin un `getDetail` adicional; ese `getDetail` queda solo
como fallback si la respuesta no trae `confirmed`.

`ConfirmedEnrollmentDetailsResponse` es una **cabecera compartida** (`personId`,
`productId`, `degreeProgram`, `academicCoordinator`, `courseCoordinator`) más un array
`enrollments[]` (`ConfirmedEnrollment`), con una entrada por cada oferta
confirmada y su propio `intake`/`shift`/`firstSemesterSubjects`: en niveles 3 y 4
vienen varias, una por seminario. Ya no existe el bloque plano `confirmed.summary`
ni un `confirmed.firstSemesterSubjects` único. El adapter arma
`EnrollmentConfirmedDetail.summary` con la cabecera más el comienzo/turno de
`enrollments[0]` (mismo colapso que usa `pendingPayment`) y expone el array completo
en `EnrollmentConfirmedDetail.enrollments`;
`EnrollmentPaymentFacade.subjects()` lista las materias de **todos** los seminarios
sin repetir las compartidas.

El listado de la pantalla de éxito arranca recortado a 4 materias (`visibleSubjects()`)
con el botón "Ver todas las materias", pero el contador del título usa siempre el total
de `subjects()`: al desplegar el listado el número no cambia, solo aparecen las materias
que faltaban.

## Estados frontend

- `processing`: solo mientras responde `/enrollments/start-payment`.
- `enrollment-confirmed`: pago confirmado por backend. Usa `confirmed` de la
  respuesta del endpoint de pago; si no vino, cae al `getDetail`.
- `reservation`: Abitab o Paganza quedan con instrucciones de pago. Se muestran la
  cédula (Abitab), el número de estudiante y el monto que informa `seniaMinima`.
  En el flujo fresco se consultan con un `getDetail` tras quedar en reserva; si
  falla, se muestra solo el monto.
- `reservation` con seña 0: si `seniaInscripcion` (o `seniaMinima.senia`/
  `pendingPayment.senia` al retomar) es exactamente `0`, no hay nada que cobrar, así
  que no corresponde mostrar el paso de pago ni llamar a `POST /enrollments/start-payment`. El front fuerza el
  outcome `reservation` directo y corta la navegación (en `EnrollmentSurveyFacade.finishSurveyStep` para el
  flujo fresco, en `EnrollmentProcessFacade.applyPaymentInit` para el caso
  `awaiting-method` al retomar) y `buildReservationInstructions` reemplaza fecha
  límite, cédula, monto y el texto de acreditación por un mensaje que indica
  comunicarse con la oficina de Admisiones; la resolución queda en manos de la
  oficina. **La rama corporativa se evalúa antes**: su respuesta tampoco trae datos de
  pago (seña `0`), pero termina en `enrollment-in-progress` ("Inscripción corporativa
  pendiente"), no en esta pantalla.
- `external-payment-pending`: Banred, Geopay o Sistarbanc ya salieron a pasarela o
  quedaron esperando definición de acreditación.
- `editing`: errores de validación o error de backend; el usuario puede corregir
  y reintentar.

## Limitación confirmada de pagos externos

Banred, Geopay y Sistarbanc **no tienen callback hacia Admisiones**. El front abre la
pasarela fuera del proyecto y deja la pantalla actual en
`external-payment-pending`; tampoco hace polling ni consulta automática de estado. Para
ver el estado actualizado, la persona debe volver al dashboard o reingresar al flujo,
que consulta nuevamente el estado persistido. Nunca se mantiene un loader infinito.

### Contrato con las páginas Pagos\*Gestion.aspx

Las tres pasarelas intermedias (`PagosBanRedGestion.aspx`,
`PagosGeoPayGestion.aspx`, `PagosSistarbancGestion.aspx`, en LogicaORT) leen el
POST así: `Request.Form["data"].Split('=')[1].Split('"')[0]` — extraen lo que
está entre el primer `=` y la primera `"`. Por eso el front envía
`data = {"params":"encryptedParameters=<blob>"}` (mismo formato que Gestion_V2
en producción). El backend de admisiones ya le quitó el prefijo
`encryptedParameters=` a la URL original (`InvoicePaymentUrl.Split`), así que el
front lo reconstruye.

### Salteo del intermediario ASPX (propuesta a backend)

El ASPX desencripta el blob, crea la transacción contra BanRed y recién ahí
redirige a la pasarela. El front no puede replicar ese paso: la clave de
desencriptación es server-side.

Propuesta: que `POST /enrollments/start-payment` devuelva directamente la URL final de
la pasarela (BanRed) ya resuelta. Con eso admisiones muestra todo el detalle del
pago en su propia pantalla y redirige sin pasar por el ASPX intermedio.

## Catalogos usados

- Carreras: el ingreso nuevo espera la elección de tipo y hace una sola llamada a
  `GET /catalogs/degree-programs?academicOffer=<1|2|3>` con el valor elegido. Aplana
  `products` para niveles 1/2 y `seminars[].products` para niveles 3/4,
  conservando `hasSeminar` del grupo. Al retomar, el nivel llega por el query
  param `nivel` y el resolver no consulta el catálogo; solo si ese param falta o es
  inválido consulta los tres tipos para reconstruirlo.
- Comienzos: `GET /catalogs/intakes?degreeProgramId=<idProducto>`
- Turnos: `GET /catalogs/shifts?degreeProgramId=<idProducto>&admissionProcessId=<idProceso>`
- Encuesta inicial: `GET /catalogs/initial-survey`
- Departamentos: `GET /catalogs/countries-states-cities` filtrando Uruguay (`codigoPais = 1`)
- Instituciones: `GET /catalogs/institutions?countryId=1&stateId=<state>`
- Bancos: `GET /catalogs/banks`

## Inventario HTTP y efectos

| Método y ruta                              | Uso                                    | Escritura / integración                                 |
| ------------------------------------------ | -------------------------------------- | ------------------------------------------------------- |
| `GET /person/enrollments`                  | dashboard agrupado                     | Oracle, solo lectura                                    |
| `GET /enrollments/details`                 | retomar/terminales                     | Oracle; carritos remotos solo para pago pendiente       |
| `POST /enrollments/product-interest`       | cerrar paso 1                          | Oracle + cola Tivenos, transaccional                    |
| `GET/POST /enrollments/initial-survey`     | precarga y guardado parcial/definitivo | Oracle + cola Tivenos al finalizar bachillerato         |
| `GET/POST /person/identity-document`       | precarga y reemplazo frente/dorso      | Oracle (`ImagenTemporal`)                               |
| `GET/POST /person/photo`                   | precarga y reemplazo selfie            | Oracle (`Imagen`)                                       |
| `GET /enrollments/student-regulations`     | aceptación previa global               | Oracle, solo lectura                                    |
| `POST /enrollments/confirm-pre-enrollment` | cerrar paso 2                          | aceptación Oracle + API interna, o workflow corporativo |
| `POST /enrollments/reactivate`             | recrear bajas                          | API interna mediante confirmación normal                |
| `POST /enrollments/start-payment`          | cobrar/reservar/redirigir              | API interna o seña mínima Oracle                        |

## Errores e idempotencia

El front muestra validaciones específicas de formulario; para fallos HTTP conserva el
paso editable y usa un mensaje recuperable. Los códigos estables para diagnóstico son:

| Área                | Códigos principales                               | Significado                                                                       |
| ------------------- | ------------------------------------------------- | --------------------------------------------------------------------------------- |
| Interés             | `GEN_IP_00..11`                                   | request/oferta inválida, persona/producto/proceso, duplicado o inscripción previa |
| Encuesta            | `GEN_OEI_*`, `INS_EI_*`                           | persona/documento, derecho, catálogos, condicionales, interés y finalización      |
| Identidad           | `FILE_VAL_*`, `GEN_SDA_*`, `GEN_SFA_*`            | archivo vacío/tipo/tamaño, fecha o persona                                        |
| Confirmación        | `INS_CPI_*`                                       | ofertas, encuesta, reglamento, identidad, vigencia y compatibilidad               |
| Reactivación        | `INS_REA_00..02`                                  | request, pertenencia o inscripción no dada de baja                                |
| Pago                | `INS_PAG_*`, `INS_PC_*`, `INS_MP_*`, `INS_UF_*`   | tipo, IDs, pertenencia, reserva repetida o banco                                  |
| Detalle/integración | `INS_DET_*`, `API_*` y códigos del cliente remoto | estado no encontrado o fallo de Enrollments y Pagos                               |

Resumen operativo de reintentos:

- identidad es un upsert y admite repetir el mismo contenido; la encuesta también, y el
  reintento sale gratis del delta: un POST fallido no avanza el snapshot, así que el cambio
  vuelve a viajar en el intento siguiente (próxima edición, Continuar o salir del paso 2);
- interés y reserva Abitab/Paganza rechazan el duplicado con 409;
- confirmación, reactivación, cuenta personal y generación de URL no envían una clave
  de idempotencia: ante resultado incierto, releer estado antes de reintentar;
- `sessionStorage` solo conserva contexto de navegación. El backend siempre revalida
  pertenencia de IDs y no lo usa como autoridad.

## Seguridad y datos sensibles

- La persona siempre sale de la sesión autenticada; producto, proceso, oferta e
  inscripción se validan contra Oracle antes de mutar o pagar.
- Imágenes y documento viajan como bytes/base64 dentro de JSON. Se validan por
  extensión, magic bytes y tamaño; no incluir ejemplos reales en este portal.
- La URL externa solo se acepta si usa `http` o `https`, exige parámetros cifrados y
  se envía por un formulario POST temporal en una pestaña nueva. El front nunca
  desencripta el blob ni contiene la clave.
- Este flujo no crea sesión Redis propia. La única persistencia del navegador es el
  contexto efímero de retomar/reactivar en `sessionStorage`, validado por
  producto/proceso y limitado a enteros positivos.

## Evidencia automatizada

- Frontend: <SourceLink repo="frontend" path="src/app/features/enrollments/models/enrollment-entry.spec.ts">matriz de entrada</SourceLink>, <SourceLink repo="frontend" path="src/app/features/enrollments/facades/enrollment-survey.spec.ts">encuesta</SourceLink>, <SourceLink repo="frontend" path="src/app/features/enrollments/facades/enrollment-payment.spec.ts">pago</SourceLink>, <SourceLink repo="frontend" path="src/app/features/enrollments/api/enrollments.api.spec.ts">mapeo HTTP</SourceLink> y <SourceLink repo="frontend" path="src/app/features/home/models/enrollment-summary.spec.ts">dashboard/pagos pendientes</SourceLink>.
- Backend: <SourceLink repo="backend" path="WebApiAdmisiones/UnitTesting/AppLogic/Services/EnrollmentUseCasesTests.cs">casos de uso</SourceLink>, <SourceLink repo="backend" path="WebApiAdmisiones/UnitTesting/AppLogic/Services/InitialSurveyServiceTests.cs">encuesta</SourceLink>, <SourceLink repo="backend" path="WebApiAdmisiones/UnitTesting/AppLogic/Services/IdentityDocumentServiceTests.cs">identidad</SourceLink>, <SourceLink repo="backend" path="WebApiAdmisiones/UnitTesting/AppLogic/Contracts/InscripcionDetalleContractTests.cs">detalle</SourceLink> y <SourceLink repo="backend" path="WebApiAdmisiones/UnitTesting/AppLogic/Contracts/EnrollmentsAndPaymentsWireContractTests.cs">contrato remoto</SourceLink>.
