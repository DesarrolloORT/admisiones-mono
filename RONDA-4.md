# Ronda 4 — hallazgos de la re-auditoría del frontend

> Documento de trabajo vivo. Se actualiza al cerrar cada lote.
> Complementa `AUDITORIA-ERRORES.md`: son los hallazgos que la ronda 3 dejó abiertos
> o que la lista "Remapeos de frontend restantes" no cubría.
>
> Repo: `admisiones`, rama `v1.0.0/fix/apiMessages`.

## Línea base verificada (2026-09-15, antes de tocar nada)

Se auditó el código real contra lo declarado en `AUDITORIA-ERRORES.md`. **Todo lo de las
rondas 1-3 está implementado tal como se declara**:

- `APP_API_ERROR_POLICY` con `useBackendMessage` en los 7 status (`api-error-policy.ts`).
- `getApiErrorMessage` es el único lector de errores de API: 29 usos en producción.
- 0 usos de `CUSTOM_ERROR_MESSAGES`; 0 mapeos `errorCode`→texto fuera de DTOs generados.
- `IdentitySaveError`, `loadFailedMessage`, `phoneValidation`, `MAX_IMAGE_SIZE_BYTES`,
  `catalogError` renderizado en `academic-proposal-select.html:47-48` y
  `enrollment-personal-step.html:20-21`, y los 5 puntos en cascada de
  `academic-proposal-selection.ts`: todos verificados.
- Los 3 remapeos deliberados (FE-AUTH-006, FE-ENR-011, FE-HOME-003) siguen intactos.

**Verificación ejecutada**: `npm test` → **962/962 tests verdes (144 archivos)**.
`tsc --noEmit -p tsconfig.app.json` → limpio.

---

## Estado de los lotes

| Lote | Qué                                                  | Estado       |
| ---- | ---------------------------------------------------- | ------------ |
| 1    | Remapeos que descartan un mensaje disponible del backend | ✅ hecho |
| 2    | Bug de flujo: fallo de `savePartial` clasificado como confirmación ambigua | ✅ hecho |
| 3    | Degradaciones que fabrican datos o estado de negocio | ⬜ pendiente |
| 4    | Menores (etiquetado, duplicación de copy)            | ⬜ pendiente |
| 5    | Actualizar `AUDITORIA-ERRORES.md` con la ronda 4     | ⬜ pendiente |

---

## Lote 1 — Remapeos que descartan un mensaje disponible del backend

Son los que rompen la promesa del documento ("el texto que ve el usuario viene ~99% del
backend") y **no** están en la lista de "3 remapeos restantes".

### 1.1 · `enrollment-survey-options.ts:131-132` — catálogos de encuesta inicial

**Qué pasa**: el handler de error de `loadInitialSurveyCatalogs` ni siquiera captura el
error y setea un texto fijo:

```ts
error: () => {
  this.catalogError.set('No se pudieron cargar los catálogos de encuesta inicial.');
```

Sus dos hermanos en el mismo archivo (`:193` departamentos, `:245` instituciones) sí usan
`getApiErrorMessage`. Además **la ronda 3 lo empeoró sin querer**: al renderizar
`catalogError` en `enrollment-personal-step.html:21`, un texto inventado por frontend pasó
de invisible a visible al usuario.

**Fix**: capturar el error y usar `getApiErrorMessage` con el texto actual como fallback.

**Test**: la suite de `enrollment-survey-options` debe afirmar que un error con `message`
del backend llega a `catalogError()`.

---

### 1.2 · `enrollment-survey.ts:438` y `:486` — guardado de identidad (FE-ENR-012 a medias)

**Qué pasa**: la ronda 3 arregló el control de flujo (`throw new Error('identity-save')` →
`IdentitySaveError`), pero **no el mensaje**. El `catchError(() => of(false))` de la línea
438 descarta el `NormalizedApiError` de `uploadIdentityDocument` / `uploadIdentityPhoto`, y
la línea 486 muestra siempre el mismo texto fijo:

```ts
identitySaveFailed
  ? 'No se pudo guardar la verificación de identidad. Intentá nuevamente.'
  : getApiErrorMessage(error, '...')
```

El backend puede estar explicando la causa real (documento vencido, formato no soportado,
tamaño) y el usuario nunca la ve.

**Fix**: que `IdentitySaveError` transporte el error original y que el mensaje salga de
`getApiErrorMessage(cause, <texto actual como fallback>)`. La rama `savedIdentity === false`
(sin error HTTP) se queda con el fallback, que es lo correcto: ahí no hay mensaje que leer.

**Cuidado**: `isAmbiguousConfirmFailure` solo se evalúa cuando `!identitySaveFailed`, así
que envolver el error de identidad mantiene esa separación (ver Lote 2).

---

### 1.3 · `home.routes.ts:20-21` + `home.html:1-11` — FE-HOME-001

**Qué pasa**: cualquier error de `getMyEnrollments` colapsa a `of(null)` y la pantalla
dibuja copy fijo de frontend. Está marcado HIGH abierto en la tabla de la auditoría, pero
**ausente de las secciones A/B/C/D de "Remapeos de frontend restantes"** — y esa lista es,
por diseño del documento, la que se consulta antes de reportar a Análisis Funcional ("si el
caso no está acá, el texto lo mandó el backend"). Hoy es un falso negativo.

**Fix**: propagar el mensaje normalizado hasta la pantalla de error del home, dejando el
copy actual como fallback.

**Extra**: errata "Porfavor" en `home.html:9`.

---

## Lote 2 — Bug de flujo: `savePartial` clasificado como confirmación ambigua

**Archivo**: `enrollment-survey.ts:437-443` y `:479`.

**Qué pasa**: la cadena de `finishSurveyStep` es

```
saveIdentityChanges() → savePartial() → confirmPreEnrollment()
```

El error de identidad se filtra con `instanceof IdentitySaveError`, pero **el de
`savePartial()` no**. Si `saveInitialSurvey` responde 409/5xx/red, `isAmbiguousConfirmFailure`
lo clasifica como confirmación ambigua y manda al usuario a la pantalla terminal
"Inscripción en proceso" con `confirmOutcomeUncertain`, **sin que `confirm-pre-enrollment`
se haya llamado nunca**.

El comentario que justifica la rama dice que `confirm-pre-enrollment` no lleva clave de
idempotencia y por eso no se puede reintentar a ciegas. Eso es cierto **solo para confirm**:
un guardado parcial fallido es perfectamente reintentable y no crea ninguna inscripción.

**Impacto**: el usuario queda convencido de que su inscripción está en trámite cuando no
existe, y el flujo cierra sin forma de volver.

**Fix**: que la clasificación de ambigüedad aplique únicamente a errores originados en
`confirmPreEnrollment`, no a cualquier error de la cadena.

---

## Lote 3 — Degradaciones que fabrican datos o estado de negocio

La auditoría las agrupa en la sección D ("degradaciones silenciosas de catálogos", fuera del
alcance de la política de mensajes) con severidad LOW. Estas dos no son "no mostrar un
mensaje": **inventan contenido**.

### 3.1 · `enrollment-payment.ts:203` — catálogo de bancos hardcodeado

```ts
error: () => this.bankOptions.set(FALLBACK_BANK_OPTIONS),
```

No degrada a lista vacía: presenta un catálogo de bancos hardcodeado en frontend que puede
no coincidir con lo que el backend acepta. Un banco desaparecido del backend sigue
ofreciéndose y el pago falla después, en un punto mucho más caro del flujo.

### 3.2 · `enrollment-survey.ts:715` — aceptación del reglamento

```ts
error: () => this.hasAcceptedStudentRegulation.set(false),
```

Remapea un fallo de consulta a un **estado de negocio** ("no aceptó el reglamento"),
indistinguible de la realidad. El usuario puede volver a aceptar algo ya aceptado.

---

## Lote 4 — Menores

- **`enrollment-process.ts:43-44`**: `proposal.catalogError() ?? survey.catalogError()` se
  renderiza en el paso 2 bajo el título "Hubo un problema al cargar la encuesta". Un error de
  turnos/comienzos del paso 1 puede aparecer mal etiquetado.
- **Copy duplicado de `phoneValidation`**: el mensaje del backend va al snackbar, pero el
  `<ort-error>` inline es fijo y con **dos textos distintos**:
  `register-personal-contact-fields.html:18` ("No se pudo validar el celular." con punto) y
  `personal-data.html:177` (sin punto).

---

## Fuera de alcance de esta ronda (requiere decisión de producto)

- **FE-SCH-004 confirmado vivo**: `scholarship-process.html:24` →
  `(confirmApplication)="showSuccess()"`. La UI confirma una postulación **sin ninguna
  llamada al backend**; `createApplication()` está stubbeada para fallar siempre y no tiene
  consumidor. Riesgo funcional serio, pero no se toca sin validar si la feature vuelve.
- Los demás endpoints stubbeados de becas (FE-HOME-002/013, FE-SCH-001/002/003).
- FE-ENR-013 (ocultamiento deliberado del fallo ambiguo de confirmación): el Lote 2 **no**
  lo toca, solo corrige a qué errores se aplica.

---

## Comandos de verificación

```bash
cd C:/GIT/admisiones-error-managment/admisiones
npx tsc --noEmit -p tsconfig.app.json
npm test
npm run lint:check
```

---

## Bitácora

### 2026-09-15 · Re-auditoría (solo lectura)

Verificadas rondas 1-3 contra el código. Línea base: 962 tests verdes, `tsc` limpio.
Identificados los lotes 1-4 y confirmado FE-SCH-004 como aún vivo.

### 2026-09-15 · Lotes 1 y 2 aplicados

**Archivos tocados** (sin commitear, rama `v1.0.0/fix/apiMessages`):

| Archivo                                             | Cambio                                                                                                          |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `facades/enrollment-survey-options.ts`              | `loadInitialSurveyCatalogs` captura el error y usa `getApiErrorMessage`.                                         |
| `facades/enrollment-survey.ts`                      | `IdentitySaveError` transporta `reason`; nueva `SurveySaveError` envuelve el fallo de `savePartial`; el handler de error se reescribió con salida temprana para identidad. |
| `models/home-data.ts`                               | Nuevos `HomeLoadFailure`, `HomeResolved`, `isHomeLoadFailure`, `DEFAULT_HOME_LOAD_ERROR`.                        |
| `home.routes.ts`                                    | El resolver devuelve `{ loadError }` con el mensaje del backend en vez de `null`.                                |
| `pages/home/home.ts` + `home.html`                  | Nuevos `loadError()` y `data()`; la pantalla de error renderiza el mensaje. Errata "Porfavor" corregida.         |
| `enrollment-personal-step.html`                     | Orden de atributos (error de `eslint` que venía de la ronda 3).                                                  |

**Detalle del Lote 2**: el comentario de `isAmbiguousConfirmFailure` ya declaraba que "los
fallos de identidad y de encuesta llegan como `Error` sin `status`, así que no matchean" —
pero el error de `savePartial` **no** venía envuelto y sí matcheaba. Envolverlo en
`SurveySaveError` hace que el código cumpla lo que el comentario ya documentaba; la función
de clasificación no se tocó.

**Tests nuevos (6)**:

- `enrollment-survey-options.spec.ts`: el mensaje normalizado del backend llega a `catalogError()`.
- `enrollment-survey.spec.ts`: mensaje del backend en fallo de subida de identidad; el fallo de
  guardado parcial con 0/409/500 (3 casos) deja el flujo en la encuesta sin llamar a confirm; y
  muestra el mensaje del backend.
- `home.routes.spec.ts`: reescrito con el resolver real — éxito, passthrough del mensaje del
  backend y fallback genérico.
- `home.spec.ts`: adaptado al nuevo contrato + caso de mensaje del backend renderizado.

**Verificación**: `npm test` → **972/972 verdes** (144 archivos, +10 tests).
`tsc --noEmit` limpio en `tsconfig.app.json` y `tsconfig.spec.json`.
`prettier --check`, `eslint` y `check-comment-noise` limpios en las áreas tocadas.
