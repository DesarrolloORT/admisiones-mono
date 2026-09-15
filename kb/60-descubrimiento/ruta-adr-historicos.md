---
status: investigation
owner:
updated: 2026-09-15
---

# Ruta agéntica para recuperar decisiones de arquitectura

## Objetivo

Recorrer el historial anterior al baseline, detectar decisiones arquitectónicas respaldadas por evidencia y producir ADR retrospectivos en orden cronológico. Un commit es evidencia de un cambio, no prueba suficiente de su motivación.

## Estrategia de agentes y costo

- **Agente económico:** inventario, filtros, agrupación de commits, lectura de diffs y preparación de candidatos.
- **Agente de mayor capacidad o revisión humana:** conflictos, motivación ambigua, decisiones de seguridad y promoción definitiva a ADR.
- Procesar lotes pequeños y guardar checkpoint tras cada lote. No volver a analizar commits ya registrados.
- Escalar únicamente candidatos de confianza `medium` o impacto alto/crítico. Los de confianza `high` pueden redactarse, pero no marcarse `accepted` sin revisión.

## Alcance y baseline

- Repositorio: `https://github.com/DesarrolloORT/admisiones-mono`
- Commit del baseline: `8354b7af`
- Frontera histórica: `4bb986030f9efe8b0e24d33a79366d0593e97716`
- Baseline de seguridad local: [`../../security/`](../../security/)
- Dirección de descubrimiento: desde la frontera histórica hacia todos sus ancestros.
- Orden de publicación de ADR: desde la decisión más antigua hacia la más reciente.
- Historial importado: el DAG contiene historiales separados de frontend y backend unidos por merges. Recorrer todas las ramas alcanzables, no solamente el primer padre.
- Exclusiones: código generado, vendorizado, lockfiles aislados, formato, merges sin cambio propio, renombres mecánicos y actualizaciones rutinarias sin consecuencia arquitectónica.

> Los commits del baseline no son historia recuperada. La arqueología comienza en `history_boundary` y recorre todos sus ancestros.

## Estado reanudable

```yaml
baseline_commit: 8354b7af905917007e27be150c5b30dad7fa242c
history_boundary: 4bb986030f9efe8b0e24d33a79366d0593e97716
traversal: all-reachable-topological
publication_order: oldest-first
traversal_correction: |
  LOTE-02 y LOTE-03 procesaron api-admisiones de punta a punta antes de
  tocar admisiones. Esto viola publication_order: los rangos de fecha se
  solapan fuertemente (api-admisiones restante: 2026-03-24 a 2026-08-27;
  admisiones completo: 2026-04-08 a 2026-09-15). A partir de LOTE-04, los
  lotes se arman por VENTANA DE FECHA combinada entre ambos repos (todos
  los commits de ambos, dentro de esa ventana, se investigan juntos),
  no repo por repo. Esto es necesario para que el orden de publicación
  de ADR refleje la cronología real de decisiones, no el orden en que
  se recorrió cada árbol.
last_batch: LOTE-06
last_commits_reviewed:
  - cf9f380c0a695dd99f26992dbf8b1540c5ba4e3d
  - 39190874deb0e61292ac15103e8dd8e5e2a1cb59
  - 9f725f4c8ede1f1a75d5f6e99d43ab8aa69acf6f
  - 5a2a83779a04d28538ccc0dc08afea9f15271ef3
  - a11e9724
window_reviewed: "2026-05-20 a 2026-05-29 (ambos repos, filtro economico por titulo — primer lote con el nuevo ritmo)"
next_candidate_id: ARCH-HIST-011
next_adr_id: ADR-005
status: in-progress
```

Nota `LOTE-06`: de ~90 commits de `api-admisiones` y ~50 de `admisiones` en esta ventana, el filtro por título descartó en bloque los de frontend (features de UI/accesibilidad/formularios de registro, sin disparador arquitectónico) y ~80 del backend (refactors, tests, merges de PR sin cambio propio, ajustes de DTO). Se investigaron en detalle 5 grupos de commits del backend que sí activaban disparadores de seguridad/autenticación, produciendo `ARCH-HIST-009` y `ARCH-HIST-010` (ambos `medium`, no promovidos).

Nota de progreso: `LOTE-05` cerró los dos pendientes de `LOTE-04` (sandbox de reconocimiento de documentos → resultó ser el prototipo de `ARCH-HIST-008`/`ADR-004`; rename de `RECAPTCHA_ENTERPRISE_KEY`→`RECAPTCHA_KEY` → descartado como táctico, era solo preparación de nombre de variable, sin uso real todavía en ese commit). Además se investigó a fondo el cluster de integración Azure Document Intelligence/Face (`ARCH-HIST-008`/`ADR-004`) y se usó como evidencia extra del gap de gobernanza de BD el commit `ecea9100`.

**Decisión de ritmo (2026-09-15, confirmada por el usuario)**: a partir de `LOTE-06`, cada lote se filtra primero por título de commit y rutas tocadas (barrido económico, sin abrir diffs) contra los disparadores del protocolo (límites de componentes; auth/sesión/seguridad; persistencia; contratos/APIs/integraciones; config por ambiente; deployment/infra/observabilidad; manejo transversal de errores; dependencias que cambien responsabilidades). Solo los commits que activan un disparador se investigan en detalle (diff completo, mismo estándar de evidencia que antes). El resto se descarta en bloque, por lote, no commit por commit. Esto prioriza cobertura del volumen (~1300 commits restantes) sobre exhaustividad en lo rutinario.

Total de ancestros de `history_boundary`: 1535 commits (incluyéndolo). Roots detectados en todo el DAG (`git rev-list --all --max-parents=0`): `2ec7478` (mono, hoy), `aa3b22c9` (repo `admisiones` frontend), `56c5536c` (repo `api-admisiones`, rama principal), `66a19be4` (repo `api-admisiones`, rama `develop`, histórico disjunto — ver gap en ARCH-HIST-002). Pendiente recorrer: historia completa de `admisiones` (root `aa3b22c9`) y de `api-admisiones` (roots `56c5536c`/`66a19be4`), ~1530 commits restantes.

Al comenzar, calcular `next_adr_id` desde los archivos reales de [`../00-adr/`](../00-adr/) y nunca reutilizar un ID.

## Protocolo de ejecución

### 1. Preparar evidencia

1. Leer [`../README.md`](../README.md), [`../AGENTS.md`](../AGENTS.md), ADR existentes y el baseline de seguridad.
2. Confirmar el remote y resolver el SHA completo del baseline.
3. Sincronizar metadatos remotos solo en modo lectura cuando sea necesario. No hacer checkout, merge, rebase, push ni modificar commits.
4. Enumerar todos los commits alcanzables hasta el baseline con orden topológico. Crear luego una vista cronológica para publicar decisiones.
5. Si GitHub está disponible, enriquecer con PR, issue, revisión y conversación asociadas. Registrar enlaces; no asumir que el mensaje del commit contiene la motivación completa.

### 2. Filtrar y agrupar

Marcar commits que afecten límites y estructura de componentes; autenticación, autorización, sesiones y seguridad; persistencia; contratos, APIs, eventos e integraciones; configuración por ambiente; deployment, infraestructura, observabilidad; manejo transversal de errores; o dependencias que cambien responsabilidades.

Agrupar commits que implementen una sola decisión. Un refactor repartido en diez commits produce un candidato, no diez ADR.

### 3. Investigar cada candidato

Reunir SHA, fecha, autor, mensaje, diff, rutas, padres, commits relacionados, PR/issues, documentación y tests contemporáneos, estado antes/después, alternativas observables y consecuencias. Separar hechos de inferencias.

- `high`: motivación y decisión explícitas en PR, documentación, ticket o commit.
- `medium`: cambio y consecuencias claros; motivación parcialmente inferida.
- `low`: solo se observa implementación; intención o alternativas desconocidas.

### 4. Decidir destino

- `high` + decisión significativa: redactar ADR retrospectivo `draft`.
- `medium`: conservar candidato y solicitar validación antes de promover.
- `low`: conservar únicamente como investigación.
- Cambio táctico/reversible: descartar como ADR y registrar el motivo.
- Fuentes contradictorias: escalar; no resolver inventando.

### 5. Publicar en orden

1. Ordenar por la fecha de la decisión inicial, no la del último commit del grupo.
2. Asignar IDs correlativos disponibles.
3. Crear `kb/00-adr/ADR-NNN-nombre.md` usando [`../00-adr/template-adr.md`](../00-adr/template-adr.md).
4. Indicar que es retrospectivo, commits fuente, confianza y gaps.
5. Usar `status: draft`; `accepted` solo con aprobación explícita.
6. Enlazar decisiones reemplazadas, contradictorias o dependientes.
7. Actualizar el checkpoint tras cada lote.

## Registro de lotes

| Lote | Rango/DAG | Commits revisados | Candidatos | ADR redactados | Estado |
|---|---|---:|---:|---:|---|
| LOTE-01 | `4bb98603`, `06142d7d`, `0dbd9a10`, `2ec7478` (los 4 commits entre `history_boundary` y los roots de `admisiones`/`api-admisiones`) | 4 | 2 | 1 (`ADR-001`, vía testimonio directo del autor el 2026-09-15, no solo evidencia de commits) | completado |
| LOTE-02 | Arranque de `api-admisiones`: roots `56c5536c`/`66a19be4` hasta el merge `825fd59a` (2026-03-02 a 2026-03-11) | 7 | 3 (`ARCH-HIST-002` resuelto/descartado, `ARCH-HIST-003`, `ARCH-HIST-004`) | 0 (candidatos `medium`, requieren validación del equipo antes de redactar ADR) | completado (parcial: resto de `api-admisiones` y toda `admisiones` pendientes) |
| LOTE-03 | `api-admisiones`: extracción desde `NewApi` hasta primer PR (2026-03-13 a 2026-03-26) | 5 (revisados en detalle; ~25 intermedios listados y descartados por rutina/no arquitectónicos) | 1 (`ARCH-HIST-005`) | 1 (`ADR-002`, confianza alta por mensaje de commit explícito) | completado (parcial) |
| LOTE-04 | **Ventana combinada** `admisiones`+`api-admisiones`, 2026-03-24 a 2026-04-30 (primer lote tras `traversal_correction`) | 4 revisados en detalle de 51 listados (resto descartado por rutina) | 2 (`ARCH-HIST-006`, `ARCH-HIST-007`) | 1 (`ADR-003`) | completado (parcial: quedan 2 items sin revisar en la misma ventana — ver nota de progreso) |
| LOTE-05 | Ventana combinada 2026-04-10 a 2026-05-19 (diff completo); resto de mayo (~180 commits) solo escaneado por título | 6 revisados en detalle | 1 (`ARCH-HIST-008`) | 1 (`ADR-004`) | completado (parcial: se detecta problema de volumen/ritmo, ver nota de progreso) |
| LOTE-06 | Ventana combinada 2026-05-20 a 2026-05-29 (primer lote con filtro económico por título) | 5 revisados en detalle de ~140 listados (resto descartado en bloque por título) | 2 (`ARCH-HIST-009`, `ARCH-HIST-010`) | 0 (ambos `medium`, requieren validación) | completado |

## Registro de candidatos

### ARCH-HIST-001 — Consolidación en monorepo `admisiones-mono` vía `git subtree`, con `Core` como submódulo

```yaml
status: promoted # discovered | investigating | validated | rejected | promoted
confidence: high
decision_date: 2026-09-15
domain: estructura-repositorio
commits: [2ec7478944611098c95ad30f718ba47c0c8383cd, 0dbd9a10c9986253c6fd2cbd1db7aafce68fa9c9, 06142d7dc805dc294a0136b6ab44452ebad6da3e, 4bb986030f9efe8b0e24d33a79366d0593e97716]
pull_requests: []
issues: []
files: [.gitmodules]
adr: ADR-001
```

#### Hechos verificados

- `2ec7478` crea el repo `admisiones-mono` desde cero (root, sin padres), con dos archivos de trabajo (`AUDITORIA-ERRORES.md`, `RONDA-4.md`) como único contenido, sin código de aplicación.
- `0dbd9a10` es un merge con metadata `git-subtree-dir: admisiones`, `git-subtree-split: 888ecfc8` — importa la historia completa del repo `admisiones` (frontend) preservando commits.
- `06142d7d` es un merge con metadata `git-subtree-dir: api-admisiones`, `git-subtree-split: e772ab60` — importa la historia completa del repo `api-admisiones` (backend) preservando commits.
- `4bb98603` agrega `.gitmodules` declarando `api-admisiones/Core` como submódulo apuntando a `https://github.com/DesarrolloORT/Core.git`.
- Ningún PR/issue asociado: los 4 commits son locales, autor único (`rubino-f`), mismo día.

#### Decisión inferida

Unificar en un solo repositorio (`admisiones-mono`) el frontend (`admisiones`) y el backend (`api-admisiones`), preservando su historial vía `git subtree` en lugar de un import squash, y mantener `Core` (librería backend compartida) como submódulo git separado en vez de incorporarlo también por subtree.

#### Motivación y alternativas

No hay motivación explícita en los commits, PR ni issues (no se recuperó ninguno), pero **sí hay testimonio directo del autor** (`rubino-f`, mismo usuario que ejecuta esta ruta), registrado en esta sesión el 2026-09-15, no en artefactos del repositorio:

> La decisión de juntar todo en un mono-repo comenzó por seguridad: era difícil conectar todos los escaneos (SAST/DAST/dependencias) si los repos estaban separados. Después se identificó que también sirve para documentar los cambios de forma centralizada. Muchos de los cambios documentados fueron realizados previamente (antes de existir esta documentación), por lo que buena parte de la reconstrucción de motivación es retrospectiva.

Esto confirma como hecho (no inferencia) que el driver primario fue la **trazabilidad de seguridad cruzada** (poder correlacionar escaneos de SAST/DAST/dependencias entre frontend y backend en un único lugar), y que la **documentación centralizada (KB/ADR) fue un beneficio secundario descubierto después**, no la motivación original. Esto es consistente con, y corrige la inferencia anterior basada solo en el orden de commits (`7b61f7d1` KB, `8354b7af` baseline ASVS): el orden de commits sugería documentación primero, pero el testimonio aclara que seguridad fue el driver y la documentación se planificó/valoró después.

Alternativas descartadas: no confirmadas todavía (el testimonio no cubrió por qué se eligió `git subtree` en vez de reescribir historia, ni por qué `Core` quedó como submódulo). Sigue como gap abierto.

#### Consecuencias observadas

- El historial de `admisiones` y `api-admisiones` queda preservado commit a commit dentro de `admisiones-mono` (no es un import squash).
- `Core` sigue siendo un repo aparte con ciclo de release propio (submódulo, no subtree): cualquier cambio a `Core` requiere bump de referencia de submódulo en `api-admisiones`.
- `AUDITORIA-ERRORES.md` y `RONDA-4.md` (documentos de auditoría de mensajes de error del frontend, sin relación con la estructura del repo) quedaron mezclados en el commit fundacional del monorepo — ver gap más abajo.

#### Gaps y validación requerida

- ~~Confirmar motivación real de la consolidación~~ — resuelto por testimonio directo del autor (ver arriba). Promovido a `ADR-001` (`status: draft`, pendiente de revisión por el equipo antes de `accepted`).
- Aclarar por qué `Core` es submódulo y no subtree, y si se evaluó extraerlo o mantenerlo así a futuro. **Sigue abierto** — no cubierto por el testimonio.
- `AUDITORIA-ERRORES.md`/`RONDA-4.md` documentan una auditoría de mensajes de error de frontend en curso (branch `admisiones@v1.0.0/fix/apiMessages`) que no tiene relación con esta decisión estructural; están commiteados junto al scaffolding del mono repo sin ninguna nota que lo explique. No es una decisión de arquitectura por sí misma, pero es una mezcla de contenido no relacionado en el mismo commit — señalarlo al equipo como "cosa extraña" a limpiar o documentar aparte. **Sigue abierto**, incluido como nota en el ADR.
- Sin contexto de base de datos ni de análisis funcional involucrado en este candidato (es puramente estructura de repositorio); no aplica gap de esos dominios aquí. Se revisará de nuevo en cada lote posterior si algún candidato sí toca esquema de datos o reglas funcionales, ya que el testimonio del autor advierte que "muchos cambios fueron realizados previamente" sin documentación — es esperable encontrar decisiones de base de datos/funcionales sin contexto recuperable más adelante en el historial.

### ARCH-HIST-002 — Historia disjunta de `api-admisiones`: dos roots (`main` y `develop`) unidos por "merge main with develop"

```yaml
status: rejected # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-03-02
domain: control-de-versiones
commits: [56c5536c, 66a19be4, 564ae0a2, 825fd59a47f66e31da32ce4f7710043540d8d47a]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `56c5536c` ("Initial commit", 2026-03-02 10:36:52) y `66a19be4` ("Initialize develop", 2026-03-02 10:36:53) son dos roots **sin ancestro común**, creados por el mismo autor (`Emanuelle-Gabriele`) con **1 segundo de diferencia**, y con el **mismo scaffolding inicial** (mismos archivos de workflow, `.gitmodules`, `Core` como submódulo, entidades Devart).
- `56c5536c` es la línea que efectivamente recibe todo el desarrollo posterior (17 commits reales entre el 2 y el 9-11 de marzo: regeneración de entidades Devart, LDAP, JWT, etc.) hasta llegar a `ba7f26f0`. `66a19be4` ("develop") **no vuelve a recibir commits propios** — queda congelado en su único commit de creación.
- `825fd59a` ("merge main with develop", 2026-03-11, autor `emanuelle-gabriele`) tiene como primer padre `66a19be4` (rama `develop`, la que estaba checkouteada) y segundo padre `ba7f26f0` (punta de `main`). El diffstat es `485 files changed, 80897 insertions(+), 67 deletions(-)`: las únicas líneas eliminadas corresponden al puntero del submódulo `Core` y a texto del `README.md`, no a código de aplicación. **No se descartó trabajo real.**

#### Decisión inferida

No es una decisión de arquitectura: es un **artefacto de creación de repositorio**. Todo indica que `main` y `develop` se crearon como dos commits raíz independientes con el mismo scaffolding (probablemente al inicializar el repo con ambas ramas desde una plantilla, en vez de crear `develop` a partir de `main`), y que el desarrollo real ocurrió solo en `main`. El merge del 11 de marzo simplemente hizo que `develop` "alcanzara" a `main`, sin pérdida de código.

#### Motivación y alternativas

No recuperable del historial local (no hay PR/issue). El patrón (mismo autor, mismo scaffolding, 1 segundo de diferencia) es consistente con una inicialización de repositorio que crea ambas ramas por separado en vez de derivar `develop` de `main`, pero esto sigue siendo inferencia sobre la causa raíz — no confirmado por testimonio.

#### Consecuencias observadas

Ninguna funcional. No hay pérdida de código verificada (confirmado con el diffstat completo de `825fd59a`).

#### Gaps y validación requerida

- Causa raíz de la creación de dos roots no confirmada por testimonio (queda como curiosidad de bajo impacto, no bloqueante).
- **Descartado como ADR** conforme al protocolo (cambio táctico/reversible sin consecuencia arquitectónica ni de negocio) — se conserva aquí solo como investigación.

### ARCH-HIST-003 — Acceso a datos generado por Devart Entity Developer sobre base de datos preexistente (DB-first, sin migraciones EF en el repo)

```yaml
status: investigating # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-03-02
domain: persistencia
commits: [56c5536c7544fdac5327c22604c145fd49e635e0, 564ae0a24c4b25cfcaae027a80a67ec1dcb2f82c, a209d44c27dcb09899b8a1fd6a554bc8266f414b]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `56c5536c` (commit inicial de `api-admisiones`) ya incluye entidades y contexto EF Core **generados por Devart Entity Developer** (`APIModel.edps`, `APIModel.efml`, `DevartEntities/*.cs`, `DevartConverters/*.cs`, repositorios `*.Generated.cs`), es decir: el modelo de datos existía en el repo desde el primer commit, generado a partir de una base de datos ya existente (reverse engineering / DB-first), no creado por migraciones versionadas en el código.
- `564ae0a2` ("initial commit", 17 min después) **elimina** ese primer set de archivos Devart generados (miles de líneas).
- `a209d44c` ("generated devart entities", ~4 horas después) vuelve a generar entidades y convertidores Devart (distinto conjunto/alcance de tablas).
- No se observó, en ninguno de estos commits, carpeta de EF Core Migrations ni scripts de esquema versionados junto al código.

#### Decisión inferida

Usar **Devart Entity Developer** como generador del modelo de acceso a datos a partir de una base de datos SQL existente (enfoque *database-first* vía herramienta comercial de terceros), en lugar de Entity Framework Core Code-First con migraciones versionadas en el repositorio. El esquema de base de datos vive y se administra fuera de este repo; el código solo consume una regeneración del modelo.

#### Motivación y alternativas

No hay motivación explícita (sin PR/issue). Es consistente con un escenario típico de modernización de una API sobre una base de datos legacy ya en producción (de ahí el nombre `Devart`/`APIModel` y la reutilización de `Core` como submódulo con utilidades de conexión a BD). No se observan alternativas evaluadas (p. ej. Code-First, Dapper, otro ORM).

#### Consecuencias observadas

- El esquema de base de datos es una dependencia externa e implícita: cualquier cambio de esquema requiere regenerar el modelo Devart fuera de este flujo de commits, sin registro versionado del cambio de esquema en sí (solo del código generado resultante).
- El borrado y regeneración en `564ae0a2`/`a209d44c` sugiere ajuste de alcance de tablas mapeadas en las primeras horas del proyecto, no un cambio de estrategia.

#### Gaps y validación requerida

- **Falta contexto de base de datos**: motor y versión del SGBD, quién administra el esquema, cómo y dónde se versionan sus cambios (¿scripts SQL en otro repo? ¿herramienta de DB migration separada?), y si existe documentación del modelo de datos fuera de este repositorio. Este es exactamente el tipo de gap advertido por el autor ("muchos cambios fueron realizados previamente" sin documentación) — no inventar la respuesta, preguntar al equipo de datos/DBA.
- Falta de análisis funcional: no hay documentación de por qué se mapearon esas tablas específicas primero, ni de las reglas de negocio detrás de entidades como `Comienzo`, `EncuestaIniAdmision`, `FondoDeBeca` (aparecen en commits siguientes sin contexto funcional).
- Confianza `medium`: el cambio y sus consecuencias son claros; la motivación (por qué Devart y no otra alternativa) es inferida, no confirmada.

### ARCH-HIST-004 — Autenticación combinada: LDAP (`a255e12e`) + JWT/refresh-token con cookies seguras (`f1dd554a`)

```yaml
status: investigating # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-03-03
domain: autenticacion-autorizacion
commits: [a255e12e1d0ea5455c64f1d1b1ed29e2a5f8ae1b, f1dd554a13f9aad12919cfb40f725e3ec29967ce]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `a255e12e` (2026-03-03) agrega "LDAP auth support and generic module data access": cambios mínimos y focalizados (extensión de DI, un campo en `appsettings.json`), reutilizando módulos genéricos que vienen de `Core` (el submódulo compartido).
- `f1dd554a` (2026-03-09, 6 días después) agrega un mecanismo de autenticación **completo y separado**: `AuthController`, `AuthService`, `TokenService`, `RefreshTokenService`, `CookieAuthenticationHelper`, y una nueva entidad de datos `RefreshToken` (persistida vía el mismo modelo Devart/EF de `ARCH-HIST-003`, sin migración visible).
- Ambos mecanismos coexisten en el código; no hay commit que remueva o reemplace LDAP al introducir JWT.

#### Decisión inferida

Sostener **dos mecanismos de autenticación en paralelo**: LDAP (típicamente usado para autenticación de usuarios internos/staff contra un directorio corporativo) y JWT con refresh token en cookies seguras (típico de un API pública consumida por un frontend SPA, aquí probablemente `admisiones`). No se puede confirmar desde el código cuál mecanismo protege qué endpoints sin leer los controllers en detalle (pendiente para un lote posterior si se retoma este candidato).

#### Motivación y alternativas

No hay PR/issue ni documentación contemporánea. Es plausible (no confirmado) que LDAP sirva para consumidores internos (staff/administración) y JWT para el frontend público de postulantes, pero es una inferencia de patrón común, no un hecho verificado.

#### Consecuencias observadas

- Dos superficies de autenticación para auditar en seguridad (relevante para el baseline ASVS del monorepo).
- La entidad `RefreshToken` se suma al modelo de datos sin migración versionada visible — mismo gap de base de datos que `ARCH-HIST-003`.

#### Gaps y validación requerida

- Confirmar con el equipo qué endpoints/consumidores usa cada mecanismo, y si hay plan de deprecar uno en favor del otro.
- Mismo gap de base de datos que `ARCH-HIST-003` (tabla `RefreshToken` sin migración visible).
- Confianza `medium`: cambio y alcance de código claros; motivación y separación de responsabilidades entre ambos mecanismos, inferida.

### ARCH-HIST-005 — Extracción de `api-admisiones` (`WebApiAdmisiones`) desde una plantilla genérica multi-sistema (`NewApi`, compartida con Empleos/Funcionarios/Gestión)

```yaml
status: promoted # discovered | investigating | validated | rejected | promoted
confidence: high
decision_date: 2026-03-13
domain: estructura-backend
commits: [0324a3b4fed6852ec89e19640be6113e2f2d0f68, 5341b92be485bcbc84f477cd44808032be1a20e0, 9966521ba5f8e2590437635ee3ee7b6489262433, 7e90fccebfcffe5894af6819a82e1807d3c40c71]
pull_requests: []
issues: []
files: []
adr: ADR-002
```

#### Hechos verificados

- El commit inicial (`56c5536c`) ya tenía código bajo `NewApi/` y lógica consciente de "sistema origen" (source-system) para CORS, logging y manejo de excepciones — el backend arrancó como una plantilla genérica pensada para servir a **más de un sistema de ORT** (Admisiones, Empleos, Funcionarios, Gestión, FichaDePersona aparecen nombrados en distintos commits: CI, Dockerfile, labeler).
- `0324a3b4` (2026-03-13, autor `luchomila`): mensaje explícito — *"Removed support for Funcionarios and Gestion systems. Updated CORS origins to only allow Admisiones domains. Refactored source system logic and logging to exclusively reference Admisiones, simplifying related code and documentation."*
- `5341b92b` y `9966521b` (2026-03-24, mismo autor): renombran Dockerfile, CI/CD, labeler y documentación de `Empleos`/`FichaDePersona` a `WebApiAdmisiones`/`api-admisiones`.
- `7e90fcce` (2026-03-26): elimina por completo la carpeta `NewApi/` (scaffold genérico original), dejando el repo con una sola estructura `WebApiAdmisiones`.

#### Decisión inferida

Extraer y especializar un backend dedicado a Admisiones (`api-admisiones`/`WebApiAdmisiones`) a partir de una plantilla/backend genérico multi-sistema compartido (`NewApi`), en lugar de mantener un único código base sirviendo a varios sistemas de ORT (Admisiones, Empleos, Funcionarios, Gestión) distinguidos en runtime por "sistema origen".

#### Motivación y alternativas

**Motivación explícita en el propio mensaje de commit** (no inferida): reducir el alcance y la complejidad del código y la documentación, sirviendo un único sistema (Admisiones) en vez de cuatro. Alternativa implícita descartada: seguir manteniendo el backend multi-sistema y agregar Admisiones como un "sistema origen" más — se optó por lo opuesto, achicar el alcance.

#### Consecuencias observadas

- Superficie de seguridad reducida: CORS y CurrentUserService ya no distinguen ni permiten otros sistemas de origen, solo Admisiones.
- Se pierde (para este repo) el posible beneficio de un backend compartido entre varios sistemas ORT — si Empleos/Funcionarios/Gestión necesitaban su propio backend, no queda evidencia en este repo de si se les extrajo un repo equivalente o si `NewApi` (o su origen) sigue viviendo en otro lado.
- CI/CD, Dockerfile y documentación quedan alineados a un solo propósito (`WebApiAdmisiones`), simplificando pipelines.

#### Gaps y validación requerida

- No verificable desde este repo: si `Empleos`/`Funcionarios`/`Gestión` obtuvieron cada uno su propio repo extraído de la misma plantilla `NewApi`, o si esos sistemas siguen viviendo en el repo/plantilla original. Preguntar al equipo si existe un repo `NewApi` (o similar) del que este código haya sido "forkeado".
- Sin gap de base de datos ni de análisis funcional específico en este candidato (es alcance/estructura de API, no esquema ni reglas de negocio).
- Confianza `high`: motivación y decisión están explícitas en el propio mensaje de commit de `0324a3b4`. Candidato para ADR retrospectivo `draft`.

### ARCH-HIST-006 — Frontend `admisiones` bootstrapped desde un starter interno `angular-template`

```yaml
status: investigating # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-04-08
domain: estructura-frontend
commits: [aa3b22c9ed4b8f457d96fdc9f2d4b4bb4406f61d, 5df9ec371fcbeee8723c6a7ca73ef614bb41753d, 7ce3b0bd, 5f5535e7]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- El commit inicial de `admisiones` (`aa3b22c9`, 2026-04-08) trae ya scaffolding completo (devcontainer, CI/CD, labeler, issue templates) idéntico en forma al de un starter reusable, no un `ng new` vacío.
- `5df9ec37` (mismo día, 11 minutos después): *"Rename project from 'angular-template' to 'admisiones' in configuration files"* — confirma explícitamente que el punto de partida fue un repo/starter interno llamado `angular-template`.
- `7ce3b0bd`/`5f5535e7` (mismo día): ajustan README y notificaciones de CI para reflejar el nuevo nombre.

#### Decisión inferida

Igual que en el backend (`ARCH-HIST-005`, plantilla `NewApi`), el frontend se bootstrapeó desde un **starter interno reutilizable de ORT** (`angular-template`) con CI/CD, labeler y convenciones ya resueltas, en vez de arrancar desde cero o desde el CLI de Angular sin plantilla propia.

#### Motivación y alternativas

No hay PR/issue. La motivación (reuso de convenciones/CI ya resueltas entre proyectos ORT) es plausible y consistente con el patrón visto en el backend, pero no está declarada explícitamente como en `ARCH-HIST-005` — es inferencia por patrón repetido, no testimonio ni mensaje explícito.

#### Consecuencias observadas

Ninguna negativa aparente; siguiente commits del mismo día ajustan referencias de nombre sin fricción.

#### Gaps y validación requerida

- Confirmar con el equipo si `angular-template` es un starter mantenido centralmente (análogo a `NewApi` en backend) y si sigue en uso para nuevos frontends.
- Confianza `medium`: conservar candidato, no promover a ADR sin validación (la decisión de reusar un starter interno es de bajo impacto por sí sola; se agrupa aquí solo para dejar registro del patrón organizacional, no amerita ADR salvo que el equipo confirme que es una convención formal a documentar).

### ARCH-HIST-007 — Integración service-to-service con la API interna "Inscripciones y Pagos" vía JWT de servicio

```yaml
status: promoted # discovered | investigating | validated | rejected | promoted
confidence: high
decision_date: 2026-04-22
domain: integraciones-servicios-internos
commits: [e802a607db1e4be31f46d1074f568de0f853c95b, 25309f8e, 8853eee8, d8f8816207d4c9354cf7a7965b884a4651c3f207]
pull_requests: []
issues: []
files: []
adr: ADR-003
```

#### Hechos verificados

- `e802a607` (2026-04-22, `luchomila`): agrega `InscripcionesApiClient`, un `TokenServiceInternalApi` para generar **JWT de servicio a servicio** (no de usuario), un `ServiceAuthenticationHandler` que inyecta tokens de usuario + servicio + headers de trace en cada request saliente, y documentación extensa sobre diseño, códigos de error, escalabilidad y pasos de integración (mensaje de commit detalla explícitamente estas decisiones).
- `d8f88162` (2026-04-27): integra endpoints reales bajo prefijo `ORTSecure/...`, agrega `OfertasInscripcionService`, un controller de ejemplo (`EjemploOfertasController`) y un documento `EJEMPLO_INTEGRACION_API_INTERNA.md` (375 líneas) con el flujo de integración documentado dentro del propio repo.
- El diseño es explícitamente **atómico y sin reintentos** ("atomic (no-retry) methods"), con manejo de errores vía `OperationResult`, según el propio mensaje de commit.

#### Decisión inferida

`api-admisiones` consume una API interna separada ("Inscripciones y Pagos", que maneja inscripciones y pagos de postulantes) mediante llamadas HTTP autenticadas con **JWT de servicio a servicio** generado internamente (no reutiliza el JWT de usuario de `ARCH-HIST-004`), con validación delegada a la API destino, timeouts de 30s, y una política deliberada de no reintentos automáticos.

#### Motivación y alternativas

**Motivación y diseño explícitos en los mensajes de commit y en `EJEMPLO_INTEGRACION_API_INTERNA.md`** (documentación contemporánea al cambio, no inferida): separar la autenticación de servicio de la de usuario, delegar validación al destino, documentar el patrón para que otras integraciones internas lo repliquen. No hay evidencia de alternativas descartadas (p. ej. mTLS, API keys estáticas, cola de mensajes en vez de HTTP síncrono).

#### Consecuencias observadas

- Nueva superficie de integración crítica: pagos e inscripciones dependen de disponibilidad síncrona de una API externa al repo, sin reintentos automáticos (falla visible al usuario si la API destino no responde en 30s).
- Nuevo secreto de configuración (`SECRET_KEY_API_INSCR_PAGOS`) para firmar/validar el JWT de servicio — relevante para el baseline de seguridad (gestión de secretos).
- Sienta un patrón documentado (`EJEMPLO_INTEGRACION_API_INTERNA.md`) para futuras integraciones internas — vale la pena confirmar si se reutilizó en integraciones posteriores.

#### Gaps y validación requerida

- **Falta contexto funcional**: qué reglas de negocio rigen "Inscripciones y Pagos" (motor de pagos, pasarela, moneda, reversibilidad) no está documentado en este repo — es una API externa a este monorepo. Preguntar al equipo funcional/de pagos.
- **Falta contexto de base de datos**: si "Inscripciones y Pagos" tiene su propia base de datos (separada de la de `api-admisiones`/Devart de `ARCH-HIST-003`), no es verificable desde aquí.
- Confianza `high`: diseño y motivación están documentados explícitamente por el propio equipo en el momento del cambio. Promovido a `ADR-003` (`draft`, pendiente de validación del equipo antes de `accepted`).

### ARCH-HIST-008 — Reconocimiento de documentos de identidad vía Azure Document Intelligence + Azure Face (prototipado en sandbox frontend, productivizado en backend)

```yaml
status: promoted # discovered | investigating | validated | rejected | promoted
confidence: high
decision_date: 2026-05-18
domain: tratamiento-datos-personales
commits: [33ea7abc8a202559775b9d61d5d74a46dff5798a, 4af236ef04e0ff4fce13f80e536b7de4518ed89e, 90ceb97f17de9b114da89287b44a0cabadf6c295, 975e3d8ad26b55f9d4ab053401edacecd94b52b6, 56f45263]
pull_requests: []
issues: []
files: []
adr: ADR-004
```

#### Hechos verificados

- `33ea7abc` (2026-04-10, frontend): feature "sandbox" que sube un archivo de documento de identidad (cédula, pasaporte, etc.) y muestra campos reconocidos (`campos`) y una bandera `requiereRevision` — prototipo exploratorio, sin backend real todavía (llamaba a un servicio no confirmado en ese momento).
- `4af236ef` (2026-05-18, backend, `rubino-f`): agrega `AzureService` (vía submódulo `Core`) y el endpoint `POST /AnalizarAdjunto` en `RegistroController`, explícitamente para "reconocimiento de documentos con Azure Document Intelligence".
- `90ceb97f` (mismo día): configuración explícita de **Azure Document Intelligence** (modelo `prebuilt-idDocument` + `prebuilt-read`) y **Azure Face** (`detection_03`), con `DeleteAnalyzeResult: true` (no retiene el resultado del análisis en Azure).
- `975e3d8a` (mismo día): rate limiting específico para el endpoint de reconocimiento (5 req/min, máx. 2MB por archivo).

#### Decisión inferida

Usar los servicios cognitivos de **Azure (Document Intelligence + Face)** como motor de reconocimiento automático de documentos de identidad y verificación facial de postulantes, integrados desde `api-admisiones` (vía el submódulo `Core` compartido), reemplazando el prototipo exploratorio que existía en el frontend (`sandbox`, `ARCH-HIST-008` inicial).

#### Motivación y alternativas

Motivación explícita en los mensajes de commit (dar de alta reconocimiento de documentos como parte del flujo de registro) pero **sin justificar la elección de Azure** sobre otros proveedores (AWS Textract/Rekognition, Google Document AI, OCR propio). No hay evidencia de alternativas evaluadas.

#### Consecuencias observadas

- Se envían **documentos de identidad y datos biométricos faciales de postulantes a un servicio cloud de terceros (Microsoft Azure)** — alto impacto en privacidad/protección de datos personales. `DeleteAnalyzeResult: true` sugiere una decisión consciente de minimizar retención en Azure, lo cual es una buena señal, pero no está documentado como tal.
- Rate limiting y límite de tamaño de archivo aplicados desde el día 1 de la integración — buena práctica de la propia decisión.
- Reemplaza al prototipo "sandbox" del frontend; habría que confirmar si ese sandbox sigue vivo como herramienta de prueba interna o si se retiró.

#### Gaps y validación requerida

- **Falta contexto funcional y de cumplimiento normativo**: no hay documentación en el repo sobre base legal para procesar datos biométricos/documentos de identidad (relevante bajo normativa de protección de datos de Uruguay), tiempo de retención real, ni si hubo una evaluación de impacto de privacidad (DPIA). Esto es exactamente el tipo de "análisis funcional faltante" que señaló el autor — escalar al equipo legal/DPO, no asumir.
- Confirmar por qué se eligió Azure (contrato marco de ORT con Microsoft, costo, u otra razón) — no inventar.
- Confianza `high` en el **qué** (la integración y su configuración están documentadas en el propio código/config); confianza más baja en el **por qué** (elección de proveedor). Promovido a `ADR-004` igual, dado el alto impacto en datos personales — se prioriza dejarlo visible para revisión aunque falte esa parte de la motivación.

**Nota adicional (refuerza gap de `ARCH-HIST-003`):** el commit `ecea9100` (2026-05-19, "added HASH_TOKEN_PASSWORD in t_persona") agrega una columna nueva (`HashTokenPassword`) directamente en el DTO/convertidor Devart de `Persona`, sin ninguna migración ni script SQL visible en el repo. Es evidencia concreta (no solo sospecha) de que el esquema de la base de datos se modifica **fuera** de este repositorio y el código simplemente se regenera/ajusta a mano para reflejarlo — confirma el gap de gobernanza de base de datos ya anotado en `ARCH-HIST-003`.

### ARCH-HIST-009 — Endurecimiento de seguridad de API pública (rate limiting, CORS credentials, CAPTCHA condicional por ambiente) — con reversión de CORS en 2 días

```yaml
status: investigating # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-05-19
domain: seguridad-api
commits: [cf9f380c0a695dd99f26992dbf8b1540c5ba4e3d, 39190874deb0e61292ac15103e8dd8e5e2a1cb59, 7208065d, 146174d7, ae1bffa8]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `cf9f380c` (2026-05-19): cambia CORS de `AllowCredentials()` a `DisallowCredentials()` "enhancing security". El propio mensaje aclara que el submódulo `Core` quedó en estado *dirty* (cambios sin commitear) al momento de este commit.
- `39190874` (2026-05-21, **2 días después**): agrega rate limiting a `Login` (5 intentos/15min por IP) y `AnalizarAdjunto` (5 req/min), middleware `RateLimiter` global, métricas Prometheus de rechazos — y **revierte el cambio anterior**, volviendo a permitir credentials en CORS ("Updated CORS policy to allow credentials for token-based authentication").
- `7208065d`/`146174d7`/`ae1bffa8` (mayo): CAPTCHA en endpoints de registro pasa por varios estados en pocos días (deshabilitado, luego exigido en producción con salto en dev).

#### Decisión inferida

Una serie de ajustes de endurecimiento de seguridad de la API pública (rate limiting en login y reconocimiento de documentos, CAPTCHA obligatorio en producción con bypass en desarrollo), aplicados de forma iterativa y, en el caso de CORS credentials, con una **reversión completa en 2 días** sin explicación de por qué la primera decisión no funcionaba.

#### Motivación y alternativas

Motivación general explícita (mensajes hablan de seguridad y monitoreo), pero el ida-y-vuelta de CORS credentials no está explicado — es una señal de iteración rápida bajo presión más que de una decisión estable. **Cosa extraña a señalar al equipo**: además de la reversión de 2 días, `cf9f380c` documenta explícitamente un submódulo `Core` dirty (buena práctica de transparencia en el mensaje, pero indica un flujo de trabajo con cambios de `Core` no siempre comiteados prolijamente antes de referenciarlos).

#### Consecuencias observadas

- La superficie pública de la API queda con rate limiting y CAPTCHA, medibles vía Prometheus.
- El comportamiento final de CORS credentials es "permitir" (post-reversión) — confirmar que esta es efectivamente la configuración vigente hoy, no algo revertido de nuevo después.

#### Gaps y validación requerida

- Preguntar al equipo qué rompió `DisallowCredentials()` en esos 2 días (¿el frontend dependía de cookies cross-origin?).
- Confianza `medium`: patrón y cambios claros, pero la motivación de la reversión es inferencia, no hecho confirmado.

### ARCH-HIST-010 — Login pasa de `codigoPersona` a `tipoDocumento`+`documento`, con recuperación/activación de contraseña por link JWT (reemplaza reset directo vía LDAP)

```yaml
status: investigating # discovered | investigating | validated | rejected | promoted
confidence: medium
decision_date: 2026-05-19
domain: autenticacion-autorizacion
commits: [9f725f4c8ede1f1a75d5f6e99d43ab8aa69acf6f, 0b7ff878, 5a2a83779a04d28538ccc0dc08afea9f15271ef3, a11e9724, 902fd2d3]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `9f725f4c`/`0b7ff878` (2026-05-20): `AuthService`/`AuthController` dejan de requerir `codigoPersona` y pasan a requerir `tipoDocumento`+`documento` para autenticar.
- `5a2a8377` (2026-05-20): `AuthService` usa `IPasswordActivationService` para enviar un link de recuperación de contraseña **basado en JWT por email**, "replacing direct LDAP resets" (cita textual del commit).
- `a11e9724` (2026-05-19): agrega un flujo de activación de contraseña seguro para onboarding/reset.

#### Decisión inferida

El modelo de identidad de login se independiza de `codigoPersona` (identificador interno, probablemente ligado a LDAP/legacy) y pasa a usar el documento de identidad como credencial de login — consistente con la existencia de un flujo de auto-registro de postulantes que no necesariamente tienen un `codigoPersona` asignado de antemano. En paralelo, el reset de contraseña deja de depender de LDAP y pasa a un flujo propio (JWT + email).

#### Motivación y alternativas

Motivación explícita para el cambio de reset de contraseña (cita textual: "replacing direct LDAP resets"). Motivación del cambio de login (`codigoPersona`→documento) no explicada en el commit, pero consistente con el flujo de auto-registro de postulantes documentado en commits cercanos — inferencia razonable, no confirmada.

#### Consecuencias observadas

- El login ya no depende exclusivamente de tener un `codigoPersona` previo, habilitando auto-registro de postulantes nuevos.
- El reset de contraseña deja de pasar por LDAP, reduciendo el acoplamiento con ese sistema para ese flujo puntual (aunque LDAP se sigue usando para autenticación, ver `ARCH-HIST-004`).

#### Gaps y validación requerida

- Falta de análisis funcional: no está documentado qué pasa con personas que sí tienen `codigoPersona` (staff/usuarios legacy) — ¿siguen pudiendo loguearse igual, o este cambio los afecta? Preguntar al equipo funcional.
- Confianza `medium`: cambio y motivación parcial claros; falta confirmar impacto sobre usuarios existentes.

## Guardrails

- No inventar motivaciones, alternativas ni reglas de negocio.
- No convertir cada commit en ADR ni modificar código de aplicación.
- No sobrescribir, renumerar ni aceptar ADR automáticamente.
- Seguridad crítica requiere evidencia verificable y revisión especializada/humana.
- No reproducir secretos ni datos sensibles hallados en el historial; registrar el hallazgo y escalarlo. Si aparece un secreto, recomendar rotación por un canal seguro.

## Criterio de finalización

- Todos los commits alcanzables anteriores al baseline están clasificados o explícitamente excluidos.
- Cada candidato conserva evidencia y nivel de confianza.
- Los ADR promovidos están ordenados, enlazados y marcados como retrospectivos.
- Las ambigüedades permanecen como gaps.
- El checkpoint permite auditar y reanudar sin repetir lotes.
