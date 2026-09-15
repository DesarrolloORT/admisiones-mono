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
- Commit base observado: `4bb986030f9efe8b0e24d33a79366d0593e97716`
- Baseline de seguridad local: [`../../security/`](../../security/)
- Dirección de descubrimiento: desde el baseline hacia atrás.
- Orden de publicación de ADR: desde la decisión más antigua hacia la más reciente.
- Historial importado: el DAG contiene historiales separados de frontend y backend unidos por merges. Recorrer todas las ramas alcanzables, no solamente el primer padre.
- Exclusiones: código generado, vendorizado, lockfiles aislados, formato, merges sin cambio propio, renombres mecánicos y actualizaciones rutinarias sin consecuencia arquitectónica.

> El baseline de seguridad aún puede estar sin commit. Antes de una ejecución definitiva, reemplazar `baseline_commit` por el commit que lo incorpore. Nunca analizar commits posteriores a ese límite.

## Estado reanudable

```yaml
baseline_commit: 4bb986030f9efe8b0e24d33a79366d0593e97716
traversal: all-reachable-topological
publication_order: oldest-first
last_batch: null
last_commits_reviewed: []
next_candidate_id: ARCH-HIST-001
next_adr_id: ADR-001
status: not-started
```

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
| TODO | TODO | 0 | 0 | 0 | pendiente |

## Registro de candidatos

### ARCH-HIST-XXX — Título provisional

```yaml
status: discovered # discovered | investigating | validated | rejected | promoted
confidence: low # low | medium | high
decision_date: YYYY-MM-DD
domain: TODO
commits: []
pull_requests: []
issues: []
files: []
adr: null
```

#### Hechos verificados

TODO

#### Decisión inferida

TODO

#### Motivación y alternativas

TODO; indicar expresamente cuando no sean recuperables.

#### Consecuencias observadas

TODO

#### Gaps y validación requerida

TODO

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

