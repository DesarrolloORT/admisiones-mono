# Plan — Errores visibles para Análisis Funcional

> Plan por fases, ejecutado por un **orquestador** lanzado desde `admisiones-mono` que delega
> cada lote en subagentes con modelo y esfuerzo fijos. Creado 2026-10-02.
> Reemplaza como guía de trabajo a `AUDITORIA-ERRORES.md` y `RONDA-4.md`, que quedan como
> evidencia histórica: se consultan por id con `grep`, no se leen enteros.

## Estado de las fases

| Fase | Qué                                                          | Ejecuta (agente)                         | Modelo · esfuerzo   | Estado       |
| ---- | ------------------------------------------------------------ | ---------------------------------------- | ------------------- | ------------ |
| F0   | Preparación y línea base                                     | `errores-verificador`, `errores-dev-docs` | Haiku 4.5 / Sonnet 5.5 · medium | ✅ hecha (línea base front: tsc OK, 1158/1158 tests en 155 archivos, lint 0 errores / 12 warnings Stylelint) |
| F1a  | Auditoría front: auth + home                                 | `errores-auditor-front`                  | Sonnet 5.5 · high   | ✅ hecha |
| F1b  | Auditoría front: enrollments + catalogs                      | `errores-auditor-front`                  | Sonnet 5.5 · high   | ✅ hecha |
| F1c  | Auditoría front: scholarships + shared                       | `errores-auditor-front`                  | Sonnet 5.5 · high   | ✅ hecha |
| F2   | Auditoría back: ¿todo error sale curado y desde el `.resx`?  | `errores-auditor-back`                   | Sonnet 5.5 · medium | ✅ hecha |
| CP1  | Punto de control de F1 + F2                                  | `errores-revisor`                        | Opus 5.5 · high     | ✅ hecho |
| F3a  | Back: capa host al `.resx` y saneamiento                     | `errores-dev-back`                       | Sonnet 5.5 · medium | ✅ hecha (sin commit; 1842 tests) |
| F3b  | Back: textos indirectos y 5xx no aptos                       | `errores-dev-back`                       | Sonnet 5.5 · medium | ✅ hecha (sin commit; 1902 tests) |
| F3c  | Back: contratos faltantes                                    | `errores-dev-back`                       | Sonnet 5.5 · medium | ⬜ pendiente |
| F4a  | Front: auth + home                                           | `errores-dev-front`                      | Sonnet 5.5 · medium | ✅ hecha (sin commit; 1170 tests; FE-AUTH-028 → F4d) |
| F4b  | Front: enrollments + catalogs                                | `errores-dev-front`                      | Sonnet 5.5 · medium | ✅ hecha (sin commit; 1201 tests; FE-ENR-003/004 y FE-R5-209 → F4d) |
| F4c  | Front: scholarships + shared                                 | `errores-dev-front`                      | Sonnet 5.5 · medium | ⬜ pendiente |
| F4d  | Front: consumir los contratos nuevos de F3c                  | `errores-dev-front`                      | Sonnet 5.5 · medium | ⬜ pendiente |
| F5a  | Librería `ngx-utils`: opción para respetar el mensaje en 5xx | `errores-dev-lib`                        | Sonnet 5.5 · high   | ✅ hecha (4.1.0, sin commit; lint de la lib no verificable por entorno) |
| F5a-bis | Front: migrar `admisiones` de `ngx-utils` 3.x a 4.x (imports a `@desarrolloort/ngx-utils/http`, schematic `ng update`) | `errores-dev-front` | Sonnet 5.5 · medium | ⬜ pendiente (nueva, 2026-10-02; va antes de F5b, tras publicar 4.1.0) |
| F5b  | Front: activar los 5xx                                       | `errores-dev-front`                      | Sonnet 5.5 · medium | ⬜ pendiente |
| CP2  | Punto de control: revisión de diffs F3–F5                    | `errores-revisor`                        | Opus 5.5 · high     | ⬜ pendiente |
| F6   | Catálogo unificado generado                                  | `errores-dev-docs`                       | Sonnet 5.5 · medium | ⬜ pendiente |
| F7   | Producto AF: página Docusaurus + XLSX                        | `errores-dev-docs`                       | Sonnet 5.5 · medium | ⬜ pendiente |
| F8   | Circuito de vuelta AF → código                               | `errores-dev-docs`                       | Sonnet 5.5 · medium | ⬜ pendiente |
| F9a  | Guardarraíl front (ESLint)                                   | `errores-dev-front`                      | Sonnet 5.5 · medium | ⬜ pendiente |
| F9b  | Guardarraíl back (test de literales)                         | `errores-dev-back`                       | Sonnet 5.5 · medium | ⬜ pendiente |
| F9c  | Cierre de documentación                                      | `errores-dev-docs`                       | Sonnet 5.5 · medium | ⬜ pendiente |

Después de cada lote de un `errores-dev-*`, `errores-verificador` (Haiku 4.5) corre la
verificación de la fase. El orquestador es la sesión principal: Sonnet 5.5 · high.

Dependencias: `F0 → (F1a ∥ F1b ∥ F1c ∥ F2) → CP1 → (F3 ∥ F4a–c ∥ F5a) → F4d → F5b → CP2 →
F6 → F7 → F8 → F9`. Si se quiere que AF empiece a mirar antes, F6–F7 pueden ir después de CP1:
el catálogo es generado, así que las correcciones se reflejan al regenerar.

---

## 0. Cómo se ejecuta

### 0.1 Arranque

Desde `C:\GIT\admisiones-mono`:

```text
claude --model sonnet
/effort high
/add-dir C:\GIT\angular-utils      ← desde la sesión S2 (la necesita F5a)
/errores-af
```

`/errores-af` carga el skill del orquestador (`.claude/skills/errores-af/SKILL.md`). Los
subagentes están en `.claude/agents/errores-*.md`; para cambiar modelo o esfuerzo se edita su
frontmatter (`model`, `effort`), no este plan. Entre sesiones: `/clear` y `/errores-af`. La
memoria es este archivo: el estado, las decisiones y la Bitácora.

**Prerrequisitos** (F0 los comprueba): token de GitHub Packages para `@desarrolloort` (npm de
`admisiones` y `docs-site`, que hoy no tienen `node_modules`), credenciales del NuGet `GITORT`
para `api-admisiones`, .NET SDK 10 y Node 22.

### 0.2 Agentes

| Agente                  | Modelo · esfuerzo   | Puede escribir en                                                                | Fases                 |
| ----------------------- | ------------------- | -------------------------------------------------------------------------------- | --------------------- |
| orquestador (principal) | Sonnet 5.5 · high   | `errores-af/PLAN.md`, commits del monorepo y, en F9c, la cabecera de `AUDITORIA-ERRORES.md` y `RONDA-4.md` | todas |
| `errores-auditor-front` | Sonnet 5.5 · high   | `errores-af/F1*.md`                                                              | F1a, F1b, F1c         |
| `errores-auditor-back`  | Sonnet 5.5 · medium | `errores-af/F2-auditoria-back.md`                                                | F2                    |
| `errores-dev-back`      | Sonnet 5.5 · medium | `api-admisiones/` salvo `Core/`, `BusinessLogic/`, `DataAccess/`, `DevartDtos/`  | F3a–c, F9b            |
| `errores-dev-front`     | Sonnet 5.5 · medium | `admisiones/src/`, filas del CSV, `ERROR-HANDLING.md`, `eslint.config.js`        | F4a–d, F5b, F9a       |
| `errores-dev-lib`       | Sonnet 5.5 · high   | `C:\GIT\angular-utils` (módulo `http-error-handling`)                            | F5a                   |
| `errores-dev-docs`      | Sonnet 5.5 · medium | `admisiones/docs/`, `admisiones/docs-site/`                                      | F0, F6, F7, F8, F9c   |
| `errores-verificador`   | Haiku 4.5           | nada: solo corre comandos                                                        | después de cada lote  |
| `errores-revisor`       | Opus 5.5 · high     | nada: solo lee                                                                   | CP1, CP2              |

Por qué así: Sonnet 5.5 · medium alcanza para los lotes de código, porque cada uno tiene
alcance, patrón y verificación cerrados. Sube a **high** donde un error se arrastra o afecta a
terceros: la clasificación de F1 alimenta todo F4, y `ngx-utils` la usan otras apps. Opus queda
para los dos puntos de control, donde se juzga criterio y no ejecución. Haiku verifica: correr
comandos y resumir números no necesita más, y deja la verificación en manos de un agente
distinto del que hizo el cambio.

### 0.3 Qué hace el orquestador y qué delega

**Hace él:**

- Leer el estado, elegir el siguiente lote y armar el prompt de delegación (§0.5).
- Pedirle al usuario las decisiones de §3 con `AskUserQuestion`. Nunca las toma ni las delega.
- Leer los reportes y hacer chequeos baratos: `git -C <repo> log -1 --stat` y
  `git -C <repo> status --short`.
- Actualizar el Estado, las Decisiones y la Bitácora, y commitear `errores-af/` en el monorepo
  (rama `rubino-f/docs/errors`).

**Delega siempre:**

| Tarea                                                              | Agente                  |
| ------------------------------------------------------------------ | ----------------------- |
| Cualquier cambio en `api-admisiones/`                              | `errores-dev-back`      |
| Cambios en `admisiones/src/`, el CSV de inventario o `ERROR-HANDLING.md` | `errores-dev-front` |
| Cambios en `admisiones/docs/` (salvo lo anterior) o `docs-site/`   | `errores-dev-docs`      |
| Cambios en `C:\GIT\angular-utils`                                  | `errores-dev-lib`       |
| Leer código o archivos grandes para auditar                        | `errores-auditor-*`     |
| Correr builds, tests, lint y escaneos                              | `errores-verificador`   |
| Revisar criterio o diffs                                           | `errores-revisor`       |

El orquestador no corre suites de tests: su salida le llena el contexto.

**Nadie** (ni el orquestador ni los subagentes): hace push, abre PRs, mergea, publica paquetes,
mueve el puntero del submódulo `Core` ni edita settings o permisos. Push y PR, solo si el
usuario los pide.

### 0.4 Concurrencia

- **Un solo agente que escriba por repo a la vez**: comparten árbol de trabajo, tests y el
  CSV. `errores-dev-front` y `errores-dev-docs` trabajan los dos en `admisiones/`, así que
  nunca van en paralelo.
- `errores-verificador` corre sobre un repo solo cuando ningún dev está escribiendo en él.
- En paralelo sí: repos distintos (F3 ∥ F4 ∥ F5a) y agentes de solo lectura entre sí
  (F1a ∥ F1b ∥ F1c ∥ F2). Los paralelos se lanzan en un mismo mensaje y se esperan las
  notificaciones, sin sondear.
- **No usar `isolation: worktree`**: en este monorepo los worktrees quedan con los submódulos
  vacíos (ver `.claude/worktrees/*/admisiones/`).

### 0.5 Prompt de delegación

El subagente no ve la conversación del orquestador: el prompt tiene que bastarse solo.

```text
Fase <FX>, lote <L>. Repo y rama: <ruta> @ <rama> (si no existe, creala desde <base>).
Leé errores-af/PLAN.md §1, §0.9 y la sección <FX>. Entrada: <archivos>.
Alcance exacto: <ids / archivos / puntos>. Todo lo demás queda fuera de alcance.
Decisiones vigentes: <D# = elección, …>.
Salida esperada: <archivo o commit>. Terminá con el reporte de §0.6.
```

Para el verificador:

```text
Repo: <ruta> @ <rama>. Desde <carpeta>, corré en orden: <comandos de la fase>.
```

### 0.6 Reporte de vuelta (máximo unas 40 líneas)

```text
ESTADO: completo | parcial | bloqueado
HECHO: <ids o puntos resueltos; conteos>
ARCHIVOS: <rutas tocadas>
VERIFICACIÓN: <comando → resultado con números; tests antes → después>
COMMIT: <repo> <rama> <hash> | sin commit (solo lectura)
PREGUNTAS: <decisiones que necesita, con la opción recomendada>
FUERA DE ALCANCE VISTO: <hallazgos que no tocó>
```

### 0.7 Ciclo por lote

1. Delegar el lote al agente de la tabla de estado.
2. Si el reporte trae `PREGUNTAS` o no está `completo`: resolver con el usuario y continuar el
   **mismo** agente con `SendMessage` (conserva su contexto).
3. Delegar la verificación de la fase a `errores-verificador`. El reporte del dev no alcanza.
4. Si falla: mandarle la falla al mismo dev con `SendMessage`. Máximo dos vueltas; después,
   parar y preguntarle al usuario.
5. Registrar el lote en el Estado y en la Bitácora (una entrada de hasta 8 líneas) y commitear
   `errores-af/` en el monorepo.

### 0.8 Sesiones del orquestador

Cada sesión cubre un bloque. Al cerrarlo, el orquestador registra todo y le pide al usuario
`/clear` y `/errores-af`. Si el contexto se llena antes (por ejemplo, por reintentos), corta en
el límite de un lote.

| Sesión | Bloque                                                  | En paralelo                                 | Decisiones que pide                                  |
| ------ | ------------------------------------------------------- | ------------------------------------------- | ---------------------------------------------------- |
| S1     | F0 → F1a–c + F2 → CP1 → correcciones de CP1             | 3 `auditor-front` + `auditor-back`          | D2, D7 y D8 al empezar; D1, D4 y D5 al cerrar CP1    |
| S2     | F3a ∥ F4a ∥ F5a, luego F3b ∥ F4b                        | `dev-back` ∥ `dev-front` ∥ `dev-lib`        | D2 y D8 tomadas                                      |
| S3     | F3c ∥ F4c, luego F4d                                    | `dev-back` ∥ `dev-front`                    | D4 tomada                                            |
| S4     | F5b → CP2 → correcciones de CP2 → ofrecer PRs           | —                                           | D1 tomada; publicación de `ngx-utils` (la hace el usuario) |
| S5     | F6 → F7                                                 | —                                           | D3 tomada                                            |
| S6     | F8 → F9a ∥ F9b → F9c                                    | `dev-front` ∥ `dev-back`                    | —                                                    |

### 0.9 Reglas comunes para subagentes

1. Del plan leé solo §1, esta sección y tu fase, más las entradas que te pase el orquestador.
2. En `admisiones` usá CodeGraph (`codegraph explore` desde `admisiones/`); en
   `api-admisiones` no hay índice, usá grep.
3. No leas enteros `AUDITORIA-ERRORES.md` (127 KB) ni los CSV: buscá con grep.
4. Respetá el `CLAUDE.md`/`AGENTS.md` del repo que tocás.
5. Si algo no está cubierto por §1 ni por las decisiones que te pasaron, no lo decidas: seguí
   con el resto y anotalo en `PREGUNTAS`.
6. No amplíes el alcance. Lo que veas afuera va a `FUERA DE ALCANCE VISTO`.
7. Commits: solo si sos dev y la verificación pasó; `git add` con rutas explícitas (nunca `-A`
   ni `.`); estilo de mensajes del repo (`git log --oneline -10`); sin push.
8. Nunca: tocar settings o permisos, push, PR, publicar, mover el puntero de `Core`.

---

## 1. Contrato (no se discute en las sesiones)

- **R1 — El backend manda el texto.** Si hubo respuesta del backend con `message`, el usuario
  ve ese `message`. Sin excepciones por status (incluye 5xx a partir de F5).
- **R2 — Si el backend no quiere contar qué pasó, lo re-mapea él.** El texto ya sale curado
  del `.resx`. El frontend no "protege" al usuario de un mensaje del backend.
- **R3 — El frontend no se come errores.** Un fallo de API nunca se convierte en silencio
  (lista vacía, `null`, `false`, `EMPTY`) ni en un dato de negocio inventado (catálogo
  hardcodeado, "no aceptó el reglamento"). Si la pantalla sigue funcionando degradada,
  igual muestra el mensaje.
- **R4 — El frontend decide flujo, no texto.** Puede ramificar por `status`/`errorCode`
  (navegar, 404 = "todavía no existe"), pero si muestra algo, es el `message` del backend.
- **R5 — Textos propios del frontend, solo estos:** validación antes de enviar (obligatorio,
  formato, largo), sin conexión/timeout (status 0), error que no es `NormalizedApiError`, y
  estados vacíos que no son error. Viven en los `*-es.json`.
- **R6 — Un solo origen editable por texto:** `.resx` en backend (clave = `errorCode`) o
  `*-es.json` en frontend. El producto para AF se **genera** desde ahí; no se edita a mano.

---

## 2. Diagnóstico al 2026-10-02

Refs: front `admisiones@v1.0.0/docs/errors-info` (`c31e3aa`), back
`api-admisiones@gabriele/feat/getScolarship` (`4ac780e2`), librería `C:\GIT\angular-utils`
(checkout local en `v1.1.1/fix/compilation`; `main` ya publicó `ngx-utils` 3.1.0; admisiones
pide `^3.0.0`).

### Frontend

- **Base sana**: `getApiErrorMessage` (`src/app/shared/errors/api-error-message.ts`) con 34
  usos; `APP_API_ERROR_POLICY` deja pasar el mensaje en 400/401/403/404/409/422/429; 0 usos
  de `CUSTOM_ERROR_MESSAGES`. Rondas 1–3 y lotes 1–2 de `RONDA-4.md` están en el código.
- **H1 — Todos los 5xx se tapan (el hallazgo más grande).** `ngx-utils`
  `ApiErrorNormalizerService.resolveSafeMessage`
  (`angular-utils/projects/ngx-utils/src/lib/http-error-handling/services/api-error-normalizer.service.ts:138`)
  reemplaza el mensaje por "Error interno del servidor…" **antes** de aplicar la política,
  así que no se arregla configurando la app. El backend tiene **29 puntos que responden 5xx
  (26 códigos)**, 25 de ellos con texto curado en `.resx` (p. ej. `AUTH_2FA_MAIL_01` "No fue
  posible enviar el código de verificación…", `API_TIMEOUT`, `REG_PERSONA_99`, `INS_CPI_13`
  con 502), y el usuario no ve ninguno. Ninguno figura en el CSV.
- **H2 — ~40 puntos que degradan en silencio** (`catchError(() => of([]|null|false|EMPTY))`,
  `error: () => undefined`). La auditoría anterior los dejó "fuera de política" (sección D)
  porque no reemplazaban texto; con R3 entran. Semilla en F1.
- **H3 — Pendientes de `RONDA-4.md`**: lote 3 (`enrollment-payment.ts:204` catálogo de bancos
  hardcodeado; `enrollment-survey.ts:665` fallo de consulta → "no aceptó el reglamento") y
  lote 4 (etiquetado de `catalogError`, copy duplicado de `phoneValidation`).
- **H4 — Remapeos "deliberados" que con R2 pasan al backend**: FE-AUTH-006 (recuperar
  contraseña convierte cualquier 4xx en éxito, por anti-enumeración), FE-AUTH-028
  (`mailSent:false` en 2xx, el texto lo inventa el front), FE-AUTH-018/019 (matriz de flujo
  de registro resuelta en front). FE-ENR-011 (404 = ausencia) es flujo, no texto: se queda (R4).
  **Enmienda CP1 (2026-10-02)**: FE-HOME-003 pasa a `SILENCIO`, severidad baja: el back nunca
  emite 404 ahí (`PersonController.cs:117-135`), así que un 404 solo ocurre con un despliegue
  roto. FE-AUTH-018/019 no son contrato de back: el back ya cumple (F2 §9.4).
- **H5 — El front valida respuestas del backend**: `enrollment-errors-es.json` →
  `payment.gatewayMissingData`, `payment.gatewayInvalidUrl`, `payment.missingEnrollment`.
  Si la pasarela responde mal, debería detectarlo y decirlo el backend.
- **Docs**: Docusaurus en `docs-site/` publica `docs/` a IIS. `docs/index.md:52` enlaza
  `PLAN-RELEVAMIENTO-ERRORES.md`, que nunca existió en git; con `onBrokenMarkdownLinks:
  'throw'` el build debería fallar (F0 lo confirma).
- **Inventario** `docs/inventarios/errores-visibles.csv`: 446 filas verificadas (277
  backend, 120 validación front, 49 respaldo), columnas `id, origen, tipo, flujo, pantalla,
  campo, condicion, texto, codigo_backend, fuente_texto, fuente_uso, estado`.
  `errores-visibles-final.csv` es **byte a byte idéntico** (duplicado). El CSV no se muestra
  en Docusaurus. Los textos de backend están **copiados** (257 filas apuntan a
  `<Modulo>Resources.resx#<clave>` en el commit `98a3c5be`; 20 apuntan a `.cs` de un commit
  previo a los `.resx`) → van a divergir. Faltan los 5xx y toda la capa host (captcha, rate
  limit, validación de esquema, model binding, excepción no controlada).

### Backend

- **8 `.resx`, 319 claves**: Enrollments 94, Authentication 75, Scholarships 61, People 37,
  Registration 31, EnrollmentsAndPayments 11, Identity 6, Catalogs 4. Patrón
  `_localizer["CODIGO"]` con clave = `errorCode`; tests `*ResourcesTests` por módulo.
- **La serie de `.resx` no está en `develop` ni en `testing`**: solo en
  `origin/gabriele/feat/getScolarship` y `origin/rubino-f/feat/email-integration` (61
  commits adelante y 32 atrás de `develop`). `docs-site/src/config/source-repositories.ts`
  apunta el backend a `develop`.
- **Escaneo** (`python errores-af/scripts/scan-backend-messages.py api-admisiones/WebApiAdmisiones`):
  222 `IsFailed` → 182 desde `.resx`, **21 literales**, 13 variables y 6 que pasan
  `.Message` de otra capa.
  - Los 21 literales están en la capa host: `RecaptchaService` (7),
    `JsonSchemaValidationFilter` (7), rate limiters en `ServiceCollectionExtensions` (4),
    `ModelBindingErrorLoggingMiddleware` (2), `ExceptionHandlingMiddleware` ("Error
    inesperado") y `ScholarshipsController.cs:330` (`FDB_ADJ_01`).
  - **Filtran detalle interno**: `AUTH_CAPTCHA_02` (nombre de variable de entorno),
    `AUTH_CAPTCHA_03` (body de Google), `AUTH_CAPTCHA_04` (códigos de Google),
    `UNSUPPORTED_MEDIA_TYPE` (eco del Content-Type), `MBM_DA_02` (`Title` del ProblemDetails,
    en inglés), `SCHEMA_*` (texto técnico). Hay que sanearlos **antes** de F5.
  - Pasan texto de otra capa: `validation.Message`/`documentValidation.Message` (Registration
    ×4, `RecoverPassword`, `InitialSurveyService:130`), `passwordValidation`
    (`ChangePassword`, `CompleteNewPerson`), re-emisiones en `CompletePasswordFlow` y
    `OperationResultExtensions`. F2 confirma de dónde sale cada texto.
- `ExceptionHandlingMiddleware`: 500 con "Error inesperado"; el detalle va en `data` solo
  fuera de producción. Bien saneado, pero literal y sin código de referencia visible.
- `git status` de `api-admisiones` muestra `Core (new commits)` sin registrar. No tocar sin
  preguntar.

---

## 3. Decisiones

| id | Decisión                                               | Estado        | Recomendación                                                                                                                                                                                                                                                       |
| -- | ------------------------------------------------------ | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| D1 | Cómo abrir los 5xx                                     | tomada | **A**: opción nueva en `ngx-utils` (p. ej. `serverErrorMessage: 'generic' \| 'backend'`, default `generic` para no romper otras apps), versión menor, y admisiones la activa. **B** (puente): `getApiErrorMessage` lee `originalError.error.message` si `status >= 500`; es rápido, pero acopla la app a la forma interna de la librería. Ambas requieren F3 cerrada. | **Tomada 2026-10-02: opción A (opción nueva en `ngx-utils`, default `generic`).**
| D2 | Base del trabajo backend                               | tomada | Mergear la serie `.resx` a `develop` antes de F3 y ramificar desde ahí. Si no se puede, ramificar desde `gabriele/feat/getScolarship` y asumir el rebase.                                                                                                              | **Tomada 2026-10-02: ramificar desde `gabriele/feat/getScolarship` y asumir el rebase.**
| D3 | Formato del producto AF                                | **pendiente** | Página Docusaurus (ya existe y ya publica) + descarga XLSX con columnas para que AF proponga cambios. El catálogo JSON de F6 desacopla: si se elige otro formato, solo cambia F7.                                                                                     |
| D4 | Anti-enumeración de recuperar contraseña (FE-AUTH-006) | tomada | Pasa al backend: responde 2xx con un mensaje neutro (en `.resx`) para cualquier documento. Confirmar con quien lleva seguridad (ASVS).                                                                                                                               | **Tomada 2026-10-02: el front muestra el `message`; el back ya cumple. Pendiente confirmación de ASVS (si no la da, el back re-mapea los 4xx a 2xx neutro).**
| D5 | El 500 genérico muestra un código de referencia        | **diferida** | Sí: "Ocurrió un error inesperado. Código de referencia: {correlationId}". Permite a AF y soporte cruzar el reporte con el log. El `correlationId` ya existe en el middleware.                                                                                        | **2026-10-02: el usuario respondió "que el back decida luego". No es una decisión tomada: F3 deja el 500 genérico sin código de referencia y se retoma con quien lleva el back.**
| D6 | Textos propios del frontend                            | tomada        | Solo los de R5.                                                                                                                                                                                                                                                     |
| D7 | Ramas                                                  | tomada | **Una rama por repo para toda la iniciativa**, así ningún agente cambia de rama a mitad de camino: front `v1.0.0/fix/errores-af` desde `v1.0.0/docs/errors-info`; back `rubino-f/fix/errores-af` según D2; librería `fix/server-error-message` según D8; monorepo `rubino-f/docs/errors`. | **Tomada 2026-10-02: una rama por repo; back `rubino-f/fix/errores-af` desde `gabriele/feat/getScolarship`.**
| D8 | Base y publicación de `ngx-utils`                      | tomada | Ramificar desde `main` (tiene 3.1.0), no desde el checkout local `v1.1.1/fix/compilation`. La publicación del paquete la hace el usuario, después de CP2.                                                                                                             | **Tomada 2026-10-02: ramificar `ngx-utils` desde `main`; la publicación la hace el usuario después de CP2.**
| D9 | `ApiHttpClient` suprime siempre el notifier global (`api-http-client.ts:193`) | tomada | Mantener la supresión y agregar en F9a una regla ESLint que exija manejo de error en todo `subscribe` a la API. **Tomada 2026-10-02**: el usuario pidió "lo más sostenible en el tiempo" y se interpretó como la recomendación (la regla estática cubre las llamadas futuras); revertible. |

Cuando una decisión se tome, cambiar el estado a "tomada", anotar la opción elegida y la fecha.

---

## 4. Fases

Cada fase indica: **ejecución** (agente, paralelismo), **objetivo**, **tareas**, **salida**,
**verificación** y **cierre**.

### F0 — Preparación y línea base

**Ejecución**: `errores-verificador` → `errores-dev-docs` → `errores-verificador`, en
secuencia (los tres pasos tocan `admisiones/`). Antes, el orquestador pide D2, D7 y D8.

**Tareas**

1. `errores-verificador`, línea base. En `admisiones/`: `npm ci`, `npm ci --prefix docs-site`,
   `npx tsc --noEmit -p tsconfig.app.json`, `npm test` y `npm run build --prefix docs-site`
   (se espera que falle por el link roto). En `api-admisiones/WebApiAdmisiones`:
   `dotnet build WebApiAdmisiones.sln` y `dotnet test WebApiAdmisiones.sln`. Si falla por
   credenciales (GitHub Packages o NuGet `GITORT`), el orquestador para y se lo pide al usuario.
2. `errores-dev-docs`, en la rama front de D7 (creándola si hace falta):
   - `docs/index.md:52`: quitar el enlace a `PLAN-RELEVAMIENTO-ERRORES.md` o reemplazarlo por
     uno que exista.
   - Borrar `docs/inventarios/errores-visibles-final.csv` (duplicado exacto) y actualizar
     cualquier referencia.
3. `errores-verificador`: `npm run build --prefix docs-site`.
4. El orquestador anota en la Bitácora los commits de los tres repos y los totales de tests de
   front y back.

**Cierre**: build de `docs-site` verde y línea base de tests anotada.

---

### F1 — Auditoría front: ¿dónde se pierde el mensaje del backend? (solo lectura)

**Ejecución**: tres `errores-auditor-front` en paralelo (F1a, F1b, F1c), junto con F2. Cada
uno escribe su archivo:

| Lote | Carpetas de `admisiones/src/app`                        | Salida                                    |
| ---- | ------------------------------------------------------- | ----------------------------------------- |
| F1a  | `features/auth`, `features/home`, `core`                | `errores-af/F1a-auth-home.md`             |
| F1b  | `features/enrollments`, `features/catalogs`             | `errores-af/F1b-enrollments-catalogs.md`  |
| F1c  | `features/scholarships`, `shared`                       | `errores-af/F1c-scholarships-shared.md`   |

**Objetivo**: la lista completa y clasificada de puntos donde el usuario no ve el `message`
del backend. Es la entrada de F4 (F1a → F4a, F1b → F4b, F1c → F4c).

**Tareas**

1. Revisar cada punto de la semilla que cae en el lote, leyendo el handler y lo que la
   pantalla muestra.
2. Barrido complementario dentro del lote (la semilla no lo cubre):
   - Handlers que **reciben** el error pero no lo leen:
     `grep -rnE "error: \(|catchError\(\(" <carpetas> --include=*.ts | grep -v spec`, y
     descartar los que llaman `getApiErrorMessage`.
   - `subscribe(...)` sobre llamadas de API **sin** callback de error. Como `ApiHttpClient`
     suprime el snackbar global, el error se pierde sin aviso.
   - Textos de `*-es.json` usados como error **después** de una llamada a API sin pasar por
     `getApiErrorMessage`.
   - Ramas por `status`/`errorCode` que además ponen un texto propio (viola R4).
3. Clasificar cada punto en **una** categoría:

   | Categoría       | Qué es                                                                                         | Regla                 |
   | --------------- | ---------------------------------------------------------------------------------------------- | --------------------- |
   | `REMAPEO`       | Hay `NormalizedApiError` con `message` y se muestra otro texto                                 | viola R1              |
   | `SILENCIO`      | El error no se muestra (degrada a vacío/`null`/`false`/`EMPTY`, handler vacío)                 | viola R3              |
   | `ESTADO-FALSO`  | El error se convierte en dato de negocio                                                       | viola R3 (grave)      |
   | `CONTRATO-BACK` | El front inventa el texto porque el backend no lo manda (2xx con booleano, validar respuesta) | se arregla en F3c     |
   | `OK-FLUJO`      | Decisión de flujo sin texto (404 = ausencia, autosave, sondeo de sesión)                       | R4, documentar motivo |
   | `OK-RESPALDO`   | El texto propio aplica solo si no hay `NormalizedApiError`                                     | R5                    |

   Los 5xx no se listan uno por uno: son un único hallazgo global (H1) y los resuelve F5.
4. Para cada `REMAPEO`/`SILENCIO`/`ESTADO-FALSO`, proponer el fix en una línea e indicar si
   requiere algo del backend. Reusar los ids `FE-*` de `AUDITORIA-ERRORES.md` cuando coincidan
   (buscar por archivo con grep); los nuevos van como `FE-R5-NNN`.

**Semilla** (grep del 2026-10-02 en `admisiones/src/app`, sin specs):

| Lote | Archivo:línea                                                                 | Patrón                                     | Pista                         |
| ---- | ----------------------------------------------------------------------------- | ------------------------------------------ | ----------------------------- |
| F1a  | `features/auth/services/auth-session.ts:88-90, 108`                           | `of(false)`, `EMPTY`                       | sondeo de sesión              |
| F1a  | `features/auth/facades/register-flow.facade.ts:304`                           | `of(null)`                                 |                               |
| F1a  | `features/auth/pages/set-password/set-password.ts:197`                        | `of(null)`                                 |                               |
| F1a  | `features/home/home.routes.ts:21`                                             | becas propias → `of([])`                   |                               |
| F1a  | `features/home/pages/personal-data/personal-data.ts:339`                      | `of(null)`                                 |                               |
| F1a  | `features/home/api/home.api.ts:30, 40`                                        | 404 → `[]`                                 | FE-HOME-003                   |
| F1a  | `register-personal-contact-fields-es.json:15`, `register-flow.facade-es.json:15`, `personal-data-es.json:36, 50, 63` | copy duplicado de `phoneValidation` (con y sin punto) | RONDA-4 lote 4 |
| F1a  | FE-AUTH-006, FE-AUTH-028, FE-AUTH-018/019                                     | ver H4                                     | `CONTRATO-BACK`               |
| F1b  | `features/enrollments/services/enrollments.ts:23-24`                          | documento y foto de identidad → `of(null)` |                               |
| F1b  | `features/enrollments/resolvers/enrollment-detail.resolver.ts:111, 119`       | `of(null)`, `of([])`                       |                               |
| F1b  | `features/enrollments/resolvers/enrollment-initial-survey.resolver.ts:43`     | `status === 404`                           | FE-ENR-011                    |
| F1b  | `features/enrollments/facades/enrollment-payment.ts:204`                      | `FALLBACK_BANK_OPTIONS`                    | RONDA-4 3.1, `ESTADO-FALSO`   |
| F1b  | `features/enrollments/facades/enrollment-payment.ts:422`                      | `error: () => undefined`                   |                               |
| F1b  | `features/enrollments/facades/enrollment-survey.ts:283`                       | `EMPTY`                                    |                               |
| F1b  | `features/enrollments/facades/enrollment-survey.ts:665`                       | reglamento → `false`                       | RONDA-4 3.2, `ESTADO-FALSO`   |
| F1b  | `features/enrollments/facades/enrollment-survey.ts:695`                       | `isAmbiguousConfirmFailure`                | FE-ENR-013                    |
| F1b  | `features/enrollments/facades/enrollment-survey-identity.ts:153`              | `error: () => undefined`                   |                               |
| F1b  | `features/enrollments/facades/enrollment-process.ts:43-44`                    | `catalogError` mal etiquetado              | RONDA-4 lote 4                |
| F1b  | `features/enrollments/facades/enrollment-process.ts:107`                      | `EMPTY`                                    |                               |
| F1b  | `features/enrollments/models/enrollment-errors-es.json` → `payment.*`         | textos que validan la respuesta de la pasarela | H5, `CONTRATO-BACK`       |
| F1c  | `shared/process-flow/auto-save.ts:48`                                         | `catchError(() => EMPTY)`                  | autosave, probable `OK-FLUJO` |
| F1c  | `features/scholarships/pages/scholarships/scholarships.ts:70`                 | `catchError`                               |                               |
| F1c  | `features/scholarships/facades/scholarship-process.ts:145, 164, 183, 212`     | `EMPTY`, `of(null)`, `of(false)`           |                               |
| F1c  | `features/scholarships/facades/scholarship-personal.ts:121-124`               | 4 catálogos → `of([])`                     |                               |
| F1c  | `features/scholarships/facades/scholarship-personal.ts:239, 278, 311, 334`    | `of(false)`                                |                               |

**Salida** (cada lote): una tabla `id | archivo:línea | flujo/pantalla | categoría | qué ve
hoy | qué debería ver | fix propuesto | requiere back | severidad | resuelto`. Al final, el
conteo por categoría y la lista `CONTRATO-BACK`, que el orquestador le pasa a F2/F3c.

**Cierre**: todos los puntos de la semilla y del barrido del lote clasificados; ninguno queda
"a revisar".

---

### F2 — Auditoría back: ¿todo error sale curado y desde el `.resx`? (solo lectura)

**Ejecución**: `errores-auditor-back`, en paralelo con F1. Si la lista `CONTRATO-BACK` de F1
llega después, el orquestador continúa el mismo agente con `SendMessage` para el punto 7.

**Objetivo**: saber qué textos del backend hay que mover, sanear o crear antes de que el front
los muestre todos.

**Tareas**

1. Correr el script de escaneo y pegar sus tablas en la salida.
2. Para cada **literal** (21): proponer clave de `.resx` y texto para el usuario. La capa host
   no tiene `.resx` propio: proponer dónde va (p. ej.
   `WebApiAdmisiones/Resources/ApiResources.resx`, siguiendo el patrón de `AppLogic.*`).
3. Para cada **variable / `.Message` de otra capa** (19): seguir el texto hasta su origen y
   decir si ya es curado (`.resx` de Identity, validador de contraseña, `Core`).
4. Para cada **5xx** (29 puntos, 26 códigos): ¿el texto se puede mostrar tal cual a un usuario
   final? Marcar los que no.
5. Validación automática de ASP.NET (`ProblemDetails` de model binding): qué le llega hoy al
   front (`MBM_DA_02` toma el `Title`, en inglés).
6. Textos que vienen de `Core/` (AzureService, LdapService) y pueden terminar en un `message`:
   listarlos. `Core` no se toca desde acá; si hace falta cambiarlo, queda anotado como pedido
   aparte.
7. Cruzar con la lista `CONTRATO-BACK` de F1: qué endpoint y qué `errorCode`/mensaje haría
   falta para cada uno.

**Salida**: `errores-af/F2-auditoria-back.md` con: tabla de literales (clave y texto
propuestos), tabla de 5xx (apto / no apto) y tabla de contratos faltantes (endpoint, caso,
código propuesto, texto propuesto).

**Cierre**: los 21 literales, los 19 indirectos y los 29 puntos 5xx con veredicto.

---

### CP1 — Punto de control de las auditorías

**Ejecución**: `errores-revisor` sobre `F1a/F1b/F1c` y `F2`. El orquestador manda cada
corrección al auditor dueño del archivo (con `SendMessage` si sigue vivo; si no, un auditor
nuevo con la lista). Después le pide al usuario D1, D4 y D5, y que apruebe los textos
propuestos en F2 (o los ajuste).

**Cierre**: veredicto `listo` del revisor y D1, D4 y D5 tomadas.

---

### F3 — Back: textos al `.resx`, saneamiento y contratos faltantes

> **Regla de CP1 (2026-10-02)**: antes de tocar `PasswordActivationService.cs:468-545`, revisar `ACT_SES_01..05` con R2 y unificarlos con el texto del enlace vencido que propone F2 (la variante sin tildes de `ACT_SES_03` se resuelve ahí). F3 también crea las claves nuevas de F2 §8 (`INS_UF_05`, `INS_CPI_18`, `INS_PC_03`, `BEC_PO_03`). Textos de F2 aprobados por el usuario el 2026-10-02; a confirmar con AF: E15 ("Admisiones se va a comunicar con vos"), `CAM_PAS_03`, `REC_DOC_11`, `SANITIZATION_ERROR`.
> D5 diferida: el 500 genérico se carga sin código de referencia.

**Ejecución**: `errores-dev-back`, un lote por invocación, en secuencia (mismo repo). En
paralelo con F4 y F5a. Requiere D2 y D7; F3a usa D5 y F3c usa D4.

| Lote | Alcance                                                                                                                                                                                                                                  |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| F3a  | Los 21 literales de la capa host al `.resx` nuevo, con los textos aprobados en CP1. Reescribir los que filtran detalle (captcha, Content-Type, esquema, model binding en inglés). El detalle técnico va al log. Si D5 = sí, el 500 incluye el `correlationId`. |
| F3b  | Textos indirectos y 5xx marcados "no apto" en F2.                                                                                                                                                                                         |
| F3c  | Contratos faltantes (`CONTRATO-BACK`): anti-enumeración de recuperar contraseña (D4), `mailSent:false`, validación de la respuesta de la pasarela de pago, flujo de registro resuelto. Cada cambio de contrato se anota en `docs/MATRIZ-TRAZABILIDAD.md`. |

Al cerrar F3c, el orquestador copia a la Bitácora la lista de contratos nuevos: es la entrada
de F4d.

**Verificación** (`errores-verificador`, desde `api-admisiones/WebApiAdmisiones`):
`dotnet build WebApiAdmisiones.sln`, `dotnet test WebApiAdmisiones.sln` y
`python ../../errores-af/scripts/scan-backend-messages.py .` (desde F3a debe dar 0 literales).

**Cierre**: 0 literales, ningún 5xx "no apto" y los contratos de F3c documentados.

---

### F4 — Front: mostrar siempre el mensaje del backend

**Ejecución**: `errores-dev-front`, un lote por invocación, en secuencia (mismo repo). F4a–c
en paralelo con F3; F4d espera a F3c.

| Lote | Entrada                                    | Alcance                                              |
| ---- | ------------------------------------------ | ---------------------------------------------------- |
| F4a  | `errores-af/F1a-auth-home.md`              | auth + home + core                                   |
| F4b  | `errores-af/F1b-enrollments-catalogs.md`   | enrollments + catalogs (incluye RONDA-4 lotes 3 y 4) |
| F4c  | `errores-af/F1c-scholarships-shared.md`    | scholarships + shared                                |
| F4d  | lista de contratos nuevos de F3c           | consumir los contratos nuevos del backend            |

**Patrón de fix** (copiar el que ya existe, no inventar):

- Error → `getApiErrorMessage(error, <texto del *-es.json como respaldo>)`.
- Degradación que debe seguir funcionando: degradar **y** setear el mensaje en el estado de
  error que la pantalla ya renderiza (ver `catalogError` en `academic-proposal-select.html`
  y `enrollment-personal-step.html`).
- `ESTADO-FALSO`: quitar el dato inventado; mostrar el error y permitir reintentar.
- `OK-FLUJO`: no se toca código; un comentario de una línea con el motivo, solo si no es
  obvio.

**Cada fix lleva su test** (el mensaje del backend llega a lo que renderiza la pantalla) y su
fila en el CSV de inventario: actualizar `condicion`/`fuente_uso` o agregar la fila.

**Verificación** (`errores-verificador`, desde `admisiones/`):
`npx tsc --noEmit -p tsconfig.app.json`, `npm test` y `npm run lint:check`, con el total de
tests antes y después.

**Cierre**: 0 `REMAPEO`/`SILENCIO`/`ESTADO-FALSO` abiertos en los archivos F1 (cada fila
marcada como resuelta, con commit).

---

### F5 — Abrir los 5xx

**Ejecución**: F5a con `errores-dev-lib` (puede ir en paralelo con F3/F4: la opción nueva
nace apagada). F5b con `errores-dev-front`, después de F3 cerrada, F4d y la publicación de la
versión nueva por parte del usuario. Requiere D1 y D8.

> **Corrección 2026-10-02 (S2)**: `origin/main` de `angular-utils` está en **4.0.0** (no 3.1.0); la
> opción nueva quedó como **4.1.0**. `admisiones` usa `^3.0.0`, por eso se agregó **F5a-bis**
> (migración 3.x → 4.x) antes de F5b. F5b necesita que el usuario publique 4.1.0.

**F5a (opción A de D1)** — en `angular-utils`, rama según D8:

1. Opción nueva en `ApiErrorHandlingConfig` para que `resolveSafeMessage` respete el `message`
   del backend en `>= 500`. Por defecto, sin cambios.
2. Tests del normalizador para ambos modos, `CHANGELOG` y versión menor.

Verificación (desde `C:\GIT\angular-utils`): `npm test`, `npm run build`, `npm run lint:check`.

**F5b** — en `admisiones`:

1. Subir `@desarrolloort/ngx-utils` a la versión nueva, activar la opción y agregar 500 a
   `APP_API_ERROR_POLICY` con `useBackendMessage: true`. Actualizar `api-error-policy.spec.ts`.
2. `docs/ERROR-HANDLING.md`: la sección "Los 5xx quedan fuera a propósito" pasa a explicar por
   qué ahora es seguro mostrarlos.
3. Agregar al CSV las filas de los 26 códigos 5xx y las de la capa host de F3a.
4. Un test que pruebe que un 500 con `message` del backend llega a la pantalla.

**Opción B de D1**: no hay F5a. F5b hace los pasos 2–4 con el puente en `getApiErrorMessage`
y un `TODO` que apunte a la opción A.

Verificación: la de F4.

---

### CP2 — Punto de control del código

**Ejecución**: `errores-revisor` sobre los diffs de los tres repos (ramas de D7 contra su
base). El orquestador manda cada hallazgo al dev que indica el revisor y repite la
verificación. Al terminar, ofrece al usuario abrir los PRs (no los abre sin que se lo pidan).

**Cierre**: veredicto `listo`.

---

### F6 — Catálogo unificado generado

**Ejecución**: `errores-dev-docs`. Requiere F4 y F5b mergeados en la rama front (o al menos
commiteados).

**Objetivo**: un JSON único con todos los textos visibles, generado desde las fuentes (R6),
que alimenta F7.

**Diseño**

- Script `admisiones/docs-site/scripts/build-error-catalog.mjs`. Lee:
  - los `.resx` del backend desde `--backend ../api-admisiones` (layout del monorepo): fuente
    de verdad del texto de backend;
  - `src/app/**/*-es.json` del front: fuente de verdad del texto de front;
  - `docs/inventarios/errores-visibles.csv` como **mapeo** (dónde y cuándo aparece cada
    texto): flujo, pantalla, campo, condición, clave.
- Escribe `docs-site/src/data/error-catalog.json`, con una cabecera `fuentes` (commits de front
  y back usados) y una entrada por fila del CSV con el texto resuelto desde la fuente.
- Modos: `--write` (regenera el JSON y refresca la columna `texto` del CSV) y `--check` (falla
  si hay diferencias). Reporta:
  - claves de `.resx` sin fila en el CSV ("sin mapear");
  - filas del CSV que apuntan a una clave que no existe;
  - las 20 filas que todavía apuntan a `.cs` en vez de `.resx` (migrarlas a `#clave`);
  - textos con placeholders sin explicar en `condicion`.
- El JSON se commitea: el CI de `docs-site` no tiene acceso al repo del backend, así que solo
  renderiza. Se regenera localmente en el monorepo.

**Verificación** (`errores-verificador`, desde `admisiones/`):
`node docs-site/scripts/build-error-catalog.mjs --check` y `npm run build --prefix docs-site`.
Más un test mínimo del parser de `.resx` y de la resolución de claves de `*-es.json`.

**Cierre**: 0 filas rotas; "sin mapear" revisado (mapeado, o explicado por qué no se ve).

---

### F7 — Producto para AF

**Ejecución**: `errores-dev-docs`. Requiere D3 (si D3 elige otro formato, se reescribe esta
fase; F6 no cambia).

**Tareas**

1. `docs/errores/index.mdx`: tabla filtrable (flujo, pantalla, origen front/back, tipo
   validación/api/respaldo) con búsqueda de texto. Columnas: id, flujo, pantalla, campo,
   condición, texto, código, origen. Agrupada por flujo → pantalla.
2. `docs/errores/como-leer.md`: el contrato de §1 en lenguaje funcional: qué textos son del
   backend, cuáles del front, qué significa "respaldo" y cómo pedir un cambio.
3. Descarga XLSX generada en el build (no se commitea) desde `error-catalog.json`, con
   columnas extra vacías: `Texto propuesto`, `Decisión (OK / Cambiar / Eliminar)`,
   `Comentario`. La página muestra la cabecera `fuentes` (versión de front y back).
4. Entrada en `docs-site/sidebars.ts` y enlace desde `docs/index.md`.

**Verificación**: `npm run build --prefix docs-site` (por `errores-verificador`). El
orquestador le pide al usuario que abra la página local y descargue el XLSX.

---

### F8 — Circuito de vuelta (AF → código)

**Ejecución**: `errores-dev-docs` escribe y prueba el script. Cuando llegue un XLSX real de
AF, el orquestador corre esta misma fase de nuevo: `errores-dev-docs` aplica el script y
`errores-verificador` corre los tests de los dos repos.

**Tareas**: script `admisiones/docs-site/scripts/apply-af-feedback.mjs` que lee el XLSX
devuelto y, para cada fila con `Decisión = Cambiar`:

- backend → actualiza el `<value>` de la clave en el `.resx`;
- frontend → actualiza la ruta en el `*-es.json`;
- valida que se conserven los placeholders (`{0}`, `{nombre}`);
- no commitea: deja un reporte con lo aplicado y lo rechazado.

`Eliminar` y los comentarios no se aplican solos: van al reporte para decidir.

**Verificación**: con un XLSX de ejemplo que cambie un texto de cada origen, después
`dotnet test`, `npm test` y `build-error-catalog --check`. Descartar los cambios de prueba.

---

### F9 — Guardarraíles y cierre

**Ejecución**: F9a (`errores-dev-front`) ∥ F9b (`errores-dev-back`), después F9c
(`errores-dev-docs`, en `admisiones/`, así que espera a F9a).

- **F9a — Front**: regla ESLint `no-restricted-syntax` que marque los handlers de error que
  ignoran el error (`catchError` y la propiedad `error` con una arrow function sin
  parámetros). El repo **prohíbe** los comentarios `eslint-disable`
  (`scripts/quality/check-disable-comments.js`), así que las excepciones `OK-FLUJO` se
  escriben explícitas (`catchError(error => isNotFound(error) ? of([]) : throwError(() => error))`)
  o pasan por un helper compartido con nombre (p. ej. `ignoreBackgroundError()` para el
  autosave), cuyo archivo se exceptúa en `eslint.config.js`. Una línea en
  `admisiones/CLAUDE.md` que apunte a `ERROR-HANDLING.md`.
- **F9b — Back**: test que falle si aparece un `IsFailed` con texto literal (puede reusar la
  lógica del script de escaneo). Contrato §1 en `docs/GUIA-ESTILO-CODIGO.md` y una línea en
  `api-admisiones/CLAUDE.md`.
- **F9c — Docs**: `errores-dev-docs` agrega `build-error-catalog --check` al checklist de PR
  (`docs/WORKFLOW.md`).
  La cabecera "Histórico. Reemplazado por `errores-af/PLAN.md`" en `AUDITORIA-ERRORES.md` y
  `RONDA-4.md` la pone el orquestador: esos archivos están en el monorepo, no en un submódulo.

**Verificación**: la de F3 en back y la de F4 en front.

---

## 5. Si algo sale mal

| Situación                                                   | Qué hace el orquestador                                                                                         |
| ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| La verificación falla dos veces seguidas                    | Para el bloque, registra la falla en la Bitácora y le pregunta al usuario.                                      |
| Un subagente tocó algo fuera de su zona (§0.2)              | No commitea; le pide al mismo agente que lo revierta y lo anota en `FUERA DE ALCANCE VISTO`.                    |
| Un subagente pide una decisión que no está en §3            | La lleva al usuario con la recomendación del agente; la agrega a §3 al tomarla.                                 |
| Falta una credencial (npm, NuGet)                           | Para y le sugiere al usuario el comando para correr con `!`.                                                     |
| El reporte no es creíble (dice "verde" sin números)         | Lo trata como no verificado y manda a `errores-verificador`.                                                    |
| Se quiere más calidad en una fase                           | Subir `effort` en el frontmatter del agente, o pasar `model: "opus"` en esa llamada puntual.                    |

---

## 6. Bitácora

Una entrada por lote, la más nueva arriba.

### 2026-10-02 · S2 bis: F4a y F4b hechas (Sonnet 5.5)

- F4a ✅ (`admisiones` @ `v1.0.0/fix/errores-af`): 11 puntos resueltos (FE-R5-001/002/005/006/007/008/009, FE-HOME-003/009/013, FE-AUTH-006 y -018); `FE-AUTH-028` queda para F4d (necesita `confirm-new-person` de F3c). Tests 1158 → 1170, tsc OK, lint 0 errores / 12 warnings. Filas nuevas en `errores-visibles.csv`; FE-R5-007 quedó unificado en `shared/forms/phone-es.json`.
- F4b ✅: 15 filas resueltas de F1b (bancos sin lista inventada, Detalle con error y reintento, 409 de `confirm-pre-enrollment` ya no va a "en proceso", reglamento/precarga/países/propuestas/guardado con mensaje del back); FE-ENR-003/004 y FE-R5-209 → F4d. Nuevo `AutoSaveOptions.onError` en `shared/process-flow/auto-save.ts` (F4c lo conecta en `scholarship-process.ts`, FE-R5-305). Tests 1170 → 1201 (156 archivos), tsc OK, lint 0 / 12. CSV 461 líneas, LF.
- Incidente: se lanzó un segundo `errores-dev-front` para F4a cuando ya había uno terminado (de una corrida previa no registrada); se detuvo el duplicado, sus restos (`phone-es.json`, filas CSV) se revisaron y se conservaron, y el lint se corrigió con el primer dev.
- Durante la sesión aparecieron dos commits del usuario en `admisiones` (6d72cab, 0b656e7), con el contenido de F4a; el orquestador no commitea. El resto de F4a/F4b sigue sin commit.
- **Preguntas abiertas**: ¿404 del Detalle (FE-R5-201) es error o "sin inscripción"? (se dejó error; confirmar con back). Snackbar del celular duplicado con el texto bajo el campo (se dejó). FE-AUTH-006 sigue condicionado a ASVS.
- **Para F6/F8 (docs)**: `docs/flujos/inscripciones.md:96` y `:900` dicen "0/409/5xx" y están desactualizados tras FE-ENR-013.
- 2026-10-05: el usuario confirmó dejar FE-R5-008 como está (el notifier ya no ignora el 404). Las imágenes estáticas no pasan por `HttpClient` y la única imagen del back (foto de identidad) atrapa su 404 en `Enrollments.preloadOrThrow`; todo 404 esperado se maneja en quien llama.
- **Siguiente (S3)**: F3c ∥ F4c, luego F4d; pendientes también F5a-bis y la publicación de `ngx-utils` 4.1.0 por el usuario antes de F5b.

### 2026-10-02 · S2 (cortada): F3a, F3b y F5a hechas; F4a/F4b sin empezar por falta de entorno (Sonnet 5.5)

- F3a ✅ (rama back `rubino-f/fix/errores-af`, sin commit): 21 literales de la capa host + captcha/middleware al `ApiResources.resx` (27 claves); ACT_SES_01..06 unificados; scan sin filas "literal" (quedan 16 variables y 6 `.Message` = F3b). Build 0 errores, 1842/1842 tests (antes 1783). Puntero de `Core` intacto (ee5acc01). SANITIZATION_ERROR a confirmar con AF. Claves INS_UF_05 → F3c; INS_CPI_18, INS_PC_03, BEC_PO_03 → F3b.
- F5a ✅ (`angular-utils` @ `fix/server-error-message`, sin commit): `serverErrorMessage: 'generic' | 'backend'`, default `generic`; 370/370 tests (antes 344), build OK; `lint:check` no verificable (CRLF de Windows y falta `angular-eslint`). **Hallazgo**: `origin/main` está en 4.0.0, la versión nueva es 4.1.0; `admisiones` usa `^3.0.0` → nueva fase F5a-bis (migración a 4.x) antes de F5b, elegida por el usuario. Con solo la opción no alcanza: F5b también pone `useBackendMessage: true` en la política 500.
- F3b ✅ (verificada por `errores-verificador`): 16 variables y 6 `.Message` curados desde el `.resx`; LDAP y Azure re-mapeados sin tocar `Core`; claves INS_CPI_18, INS_PC_03, BEC_PO_03 creadas; 29 puntos 5xx sin ninguno "no apto"; build 0 errores, 1902 tests (antes 1842). Scan: 0 literales y 6 falsos positivos documentados (4 `Localizer(...)` en `ServiceCollectionExtensions.cs`, 2 en `OperationResultExtensions.cs`). AUTH_LDAP_03 no es huérfana (F2 se equivocó).
  - **Contratos visibles para F4d** (también en `MATRIZ-TRAZABILIDAD`): `change-password` AUTH_LDAP_20/21/22 → CAM_PAS_03 (400), AUTH_LDAP_97 → CAM_PAS_99 (500); `complete-initial-password` AUTH_LDAP_50/51/52/94 → INI_PAS_05 y AUTH_LDAP_40/41/42/95 → REG_PERSONA_98 (conservan 400/500); `confirm-pre-enrollment`, `start-payment` (cuenta personal) y `scholarships/applications`: API_TIMEOUT/API_NETWORK/API_UNEXPECTED y CONFIRMAR_PREINSCRIPCION_MULTIPLE_01 → INS_CPI_18 / INS_PC_03 / BEC_PO_03 (502); `analyze-attachment` mismo código y status, texto nuevo (REC_DOC_01..14 y REC_DOC_99). El front no debe ramificar por API_* en esos endpoints.
  - **A confirmar con AF**: "Admisiones se va a comunicar con vos" (INS_CPI_13/18, BEC_PO_03), CAM_PAS_03, REC_DOC_11, SANITIZATION_ERROR; textos de F3b que no estaban en F2 (LOGIN_LDAP_02/03, REC_PAS_02/03, REG_DOC_01/02, GEN_OEI_02/INS_EI_55, INI_PAS_99, FLOW_02/04, variantes de link/token, mensajes de éxito) y REG_TEL_01/PER_ADP_07 con texto fijo en vez de lista blanca.
  - Fuera de alcance visto: textos sin tildes en Enrollments (GEN_IP_*, INS_CPI_02/03/15, la mayoría de INS_EI_*), `GEN_SFA_02/05` interpola un texto de Core, `SCHEMA_ERROR` responde 400 (debería ser 500), `httpCode: 500` fijo en el cuerpo del middleware, 401/403/404/405/413 sin message, INI_PAS_02/05 duplicadas (F6 las unifica).
- F0 ✅ cerrado al reanudar S2 (el usuario corrió `npm run env -- desa`): línea base front tsc OK, 1158/1158 tests, 155 archivos, lint 0 errores / 12 warnings. (Antes estaba abierto: faltaba `src/environments/generated-environment.ts` (`npm run env -- desa`, credenciales Azure). Los tipos `generated/` ya se generaron. `lint:check` del front: 0 errores, 12 warnings Stylelint. F4a no arranca hasta tener `tsc` y `npm test` en línea base.
- **Siguiente (S2 bis)**: con `generated-environment.ts` generado, cerrar F0 (`tsc` + `npm test` línea base), luego F4a ∥ F4b (los dos escriben en `admisiones/`: van en secuencia, no en paralelo). F3c ∥ F4c quedan para S3; la rama del front es `v1.0.0/fix/errores-af`.
- Estado de los repos al cortar (todo sin commit por regla del usuario): `api-admisiones` @ `rubino-f/fix/errores-af` (61 modificados, 4 nuevos); `angular-utils` @ `fix/server-error-message` (8 modificados); `admisiones` @ `v1.0.0/fix/errores-af` (solo F0 docs, staged); `errores-af/` sin commitear.
- Nota: el verificador del back confundió la rama del monorepo con la de `api-admisiones` y leyó las filas de F3b como falla; el orquestador lo revalidó a mano.

### 2026-10-02 · S1: CP1 cerrado (Sonnet 5.5 orquestador, Opus 5.5 revisor)

- CP1: revisor dio `no listo` con 16 correcciones; aplicadas por los auditores dueños; re-revisión con 5 ajustes más; veredicto final `listo`.
- Cambios de clasificación: FE-ENR-013 → ESTADO-FALSO alta (el 409 deja de ir a "en proceso"); FE-AUTH-018/019, FE-HOME-009, FE-ENR-012, FE-ENR-005, FE-R5-010 ya no son contrato de back; `CAT_BANCOS_01` descartado (lista vacía = estado vacío R5); FE-R5-305/210 SILENCIO. CONTRATO-BACK finales: F1a FE-AUTH-028 (parcial) y FE-AUTH-006 (condicional ASVS); F1b FE-ENR-003/004 (→ `INS_UF_05`) y FE-R5-209; F1c ninguno.
- Pendientes de fix front: F1a 12, F1b 18, F1c 16. F2: claves nuevas `INS_UF_05`, `INS_CPI_18`, `INS_PC_03`, `BEC_PO_03`.
- Decisiones: D1 = A, D4 = front muestra `message` (ASVS a confirmar), D5 diferida, D9 nueva (tomada, revertible). Textos de F2 aprobados.
- Hallazgo para seguridad (ASVS): `evaluate-document` responde `UserAlreadyRegistered` (anónimo, captcha ScoreOnly); revela si una cédula tiene cuenta. Decidirlo junto con D4.
- Fuera del plan, ticket aparte: datos hardcodeados en `scholarship-confirmation-step.html`, y posible bug de `canConfirm`/`missing` sin refrescar tras guardar.
- **F0 sigue abierto**: falta repetir `tsc` y `npm test` del front. Requiere que el usuario corra `npm run env -- desa` y luego `npm run update-api -- --spec-dir .api-spec --skip-build`.
- Nadie commiteó (regla del usuario). Sin commits en los repos; cambios de F0 docs staged en `admisiones`; `errores-af/` sin commitear.
- Siguiente: S2 (F3a ∥ F4a ∥ F5a, luego F3b ∥ F4b), con `/add-dir C:\GIT\angular-utils`.

### 2026-10-02 · S1: F0 parcial, F1a–c y F2 hechas (Sonnet 5.5)

- D2 = base back `gabriele/feat/getScolarship`; D7 = una rama por repo; D8 = `ngx-utils` desde `main`.
- Línea base back (`gabriele/feat/getScolarship` @ 4ac780e2): falló por el submódulo `Core` desfasado (01239cdf); alineado al puntero ee5acc01 con `git submodule update` (el puntero no se movió). Build 190 warnings / 0 errores; 1783/1783 tests.
- Línea base front: `docs-site` roto por link a `PLAN-RELEVAMIENTO-ERRORES.md`; `tsc` (47 errores) y `npm test` (0 ejecutados) caen por faltar `generated-environment.ts` (lo genera `npm run env -- desa`, requiere credenciales Azure: lo corre el usuario) y los tipos de `generated/` (`npm run update-api -- --spec-dir .api-spec --skip-build`). **Pendiente**: repetir `tsc` y `npm test`.
- F0 docs: rama front `v1.0.0/fix/errores-af` (desde `v1.0.0/docs/errors-info`); `docs/index.md` sin el link roto y CSV duplicado borrado; `docs-site` compila (verificado aparte). Cambios staged, sin commit.
- F1: F1a 34 puntos (14 con fix), F1b 34 (18), F1c 21 (16). CONTRATO-BACK: 6 + 5 + 0. F2: 222 / 182 / 21 / 13 / 6, 29 puntos 5xx; §9 con contratos nuevos.
- Por regla del usuario nadie commitea: todo queda staged/sin commit. Siguiente: CP1.

### 2026-10-02 · Orquestación definida (Opus 5.5)

- Plan reestructurado para un orquestador en `admisiones-mono` que delega en 8 subagentes
  (`.claude/agents/errores-*.md`) y se lanza con `/errores-af`
  (`.claude/skills/errores-af/SKILL.md`).
- Agregados D7 (una rama por repo) y D8 (base y publicación de `ngx-utils`).
- Siguiente paso: sesión S1 (F0).

### 2026-10-02 · Plan creado (Opus 5.5)

- Relevamiento de solo lectura de front, back y `ngx-utils`. Diagnóstico en §2.
- Agregado `errores-af/scripts/scan-backend-messages.py` (escaneo de `IsFailed` por origen
  del texto). Resultado actual: 222 llamadas, 182 `.resx`, 21 literales, 13 variables, 6
  `.Message` de otra capa; 29 puntos que responden 5xx (26 códigos).
- No se modificó código de `admisiones` ni de `api-admisiones`.
- Pendiente para el usuario: D1–D5.
