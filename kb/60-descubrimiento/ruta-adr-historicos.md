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
last_batch: LOTE-01
last_commits_reviewed:
  - 4bb98603
  - 06142d7d
  - 0dbd9a10
  - 2ec7478944611098c95ad30f718ba47c0c8383cd
next_candidate_id: ARCH-HIST-003
next_adr_id: ADR-002
status: in-progress
```

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
status: discovered
confidence: low
decision_date: null
domain: control-de-versiones
commits: [56c5536c, 66a19be4, 825fd59a47f66e31da32ce4f7710043540d8d47a]
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

- `git rev-list --all --max-parents=0` muestra dos commits raíz distintos y **no relacionados** (`git merge-base --is-ancestor` confirma que ninguno es ancestro del otro) que ambos terminan siendo ancestros del backend: `56c5536c` ("Initial commit", historia principal con entidades generadas por Devart, servicios, LDAP, etc.) y `66a19be4` ("Initialize develop").
- Se unen recién en el commit `825fd59a` ("merge main with develop"), con padres `[66a19be4, ba7f26f0…]`.

#### Decisión inferida

Ninguna decisión de arquitectura — es evidencia de una anomalía de control de versiones: la rama `develop` del backend se inicializó en algún momento como historia huérfana (sin compartir ancestro con `main`) y luego se reconcilió con un merge. Causa no determinada (reset de rama, recreación tras pérdida de historia, migración de otro sistema de control de versiones, etc.).

#### Motivación y alternativas

No recuperable del historial local. Requiere a alguien del equipo que haya estado en el proyecto en esa época (fecha exacta no determinada aún, está muy cerca de los primeros commits de `api-admisiones`).

#### Consecuencias observadas

Ninguna funcional aparente; el merge no descarta código (a validar en el próximo lote al leer el diff completo de `825fd59a`).

#### Gaps y validación requerida

- No se determinó aún la fecha ni el diff completo de `825fd59a`; queda para el lote que cubra el inicio de `api-admisiones`.
- Preguntar al equipo si recuerdan una recreación/reset de la rama `develop` en el backend.
- Candidato de **investigación únicamente** por ahora (confianza `low`); no se propone ADR salvo que el próximo lote revele una decisión real detrás del reset.

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
