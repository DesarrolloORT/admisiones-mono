# Iteración ASVS: un requisito por sesión

## Objetivo y límites

Revisar OWASP ASVS 5.0.0 punto por punto, dejando contexto persistente en este monorepo. La fase actual revisa **solo requisitos L1**: 70 requisitos. Cada pedido de «seguir» procesa **un único requisito L1**, o retoma un L1 que quedó en curso. El objetivo general L2 conserva sus 253 requisitos L1/L2, pero los 183 L2 quedan reservados para una fase posterior, que requiere un pedido explícito del usuario. Las revisiones L2 ya documentadas se conservan; los 92 L3 permanecen en la cola como FUERA_L2.

Esta fase autoriza lectura, análisis y Markdown. No implementar fixes, modificar tests/código/configuración/dependencias, regenerar contratos o Devart, actualizar submódulos, publicar ramas ni ejecutar scans activos. Las propuestas de remediación se escriben para una fase posterior. No comenzar la revisión al cargar este plan sin un pedido de continuar.

## Cómo retomar después de resetear el contexto

El usuario puede pegar:

> Seguí el plan security/asvs/planning/ITERACION.md. Leé AVANCE.md, retomá el requisito en curso o el siguiente y revisá solamente ese punto. Documentá evidencia y pendientes, actualizá el avance y no apliques fixes.

El agente debe leer, en este orden:

1. Las instrucciones AGENTS.md aplicables y el estado Git del monorepo.
2. Este archivo y [AVANCE.md](./AVANCE.md).
3. Si existe, la ficha del requisito activo en revisiones/. Leer únicamente las fichas previas que el requisito cite como dependencias.
4. El requisito exacto en el [JSON fuente](../source/OWASP_Application_Security_Verification_Standard_5.0.0_en.json), su sección y las notas de [catalogo.md](./catalogo.md).
5. El plan del componente pertinente: [frontend](./admisiones.md), [API](./api-admisiones.md) o [Core](./Core.md). Leer instrucciones locales antes de inspeccionar cada componente.

El chat anterior no es la fuente de avance. Si el puntero, la cola y las fichas discrepan, reconstruir el estado desde las fichas y Git antes de avanzar; no asumir que un requisito fue revisado.

## Orden y selección del punto

Entre los requisitos L1 seleccionables, seguir el orden original del JSON: capítulo, sección y requisito según sus Ordinal. No ordenar los IDs como strings: 1.2.10 no antecede a 1.2.2.

La cola completa vive en AVANCE.md y conserva todas sus filas en el orden canónico. Para seleccionar, filtrar solo L1: elegir primero un L1 EN_CURSO; si no hay uno, el primer L1 POR_REVISAR. Saltar todos los L2 sin cambiar sus estados, resultados o fichas. Si existiera un L2 EN_CURSO, conservar su checkpoint para la fase posterior sin retomarlo en esta fase. Al agotar los L1, detenerse y registrar que la revisión documental L1 está completa; no comenzar L2 automáticamente. Los L3 permanecen FUERA_L2, lo que no significa NOT_APPLICABLE. El orden canónico entre los L1 reemplaza, para esta fase, el orden sugerido por riesgo en los planes por componente. Una observación urgente de otro punto se anota como dependencia o pendiente sin abrir una segunda revisión.

Un requisito puede abarcar varios componentes: tratar sus partes en la misma ficha y sesión. Si no alcanza el contexto, guardar lo alcanzado y dejarlo EN_CURSO, con una instrucción exacta para retomar.

## Procedimiento por iteración

1. **Fijar el punto.** Registrar ID, texto oficial, nivel y pregunta verificable. Marcar EN_CURSO en la cola y actualizar el puntero antes de investigar.
2. **Fijar la referencia.** Registrar HEAD completo, estado local relevante, ruta y versión Core consumida. Evaluar por defecto los subtrees del monorepo y su Core fijado; no mezclar resultados con clones originales que tienen otras refs o cambios sin commit. Si se inspecciona un archivo modificado, registrar ese hecho y su hash; la evidencia ya no corresponde solo a HEAD. Validar que el JSON conserva el SHA-256 BCDBEC214D70ABCFAD9284A31D4F9E5134305831D628AAD3AA85D7E26626CB35.
3. **Delimitar aplicabilidad.** Determinar flujos, entradas, boundaries y componentes reales. El reparto por capítulo es preliminar. No excluir GraphQL/OAuth/WebRTC u otras tecnologías sin inventario que sustente la exclusión.
4. **Localizar implementación.** Usar CodeGraph antes de búsquedas/lecturas de código en cada componente que tenga .codegraph/. Si no está indexado, usar rg. Seguir el flujo pertinente y sus llamadas; no inferir seguridad únicamente de nombres, frameworks o documentación histórica.
5. **Reunir evidencia acotada.** Enlazar archivos y líneas, símbolos, callers y tests existentes. Distinguir código leído, pruebas existentes no ejecutadas y resultados reproducibles realmente obtenidos. Evitar ejecutar builds que regeneren Devart o cambien archivos. Si hace falta runtime, infraestructura, credenciales o un scan, documentar la evidencia faltante y el método propuesto, sin ejecutarlo en esta fase.
6. **Escribir la ficha.** Registrar observaciones por componente, contradicciones, dependencias, límites de cobertura y resultado sustentado. Si hay una posible remediación, describirla en Markdown en el repo responsable cuando su rama fix/owasp esté disponible; la ficha central debe enlazar esa copia y fijar su ref. Si el worktree no existe, conservar la propuesta central y registrar distribución pendiente, sin recrear repos ni cambiar checkouts por suposición.
7. **Cerrar el checkpoint.** Validar IDs, enlaces locales, consistencia de cola/ficha/punteros y git diff --check. Dejar los Markdown de esta iteración sin commit, preservando cualquier cambio o staging ajeno, y registrar archivos y comando de continuación explícitos en AVANCE.md. Por instrucción vigente del usuario, no hacer commits ni pushs salvo una autorización posterior explícita.
8. **Entregar y detenerse.** Informar ID revisado, resultado, evidencia/gaps y próximo ID. No empezar el siguiente requisito hasta un nuevo «seguir».

No cargar capítulos enteros ni reauditar todo el proyecto en cada sesión. Una referencia previa solo se reutiliza si se conserva su commit/ambiente y demuestra lo exigido por el requisito actual.

## Estados: avance documental y seguridad

| Campo | Valores y significado |
| --- | --- |
| Avance | POR_REVISAR: no iniciado; EN_CURSO: checkpoint parcial; DOCUMENTADO: análisis de este punto registrado; FUERA_L2: L3 conservado sin revisión dentro del objetivo |
| Resultado | PENDING: todavía sin evaluación; NEEDS_REVIEW: revisión parcial o falta evidencia determinística; PASS/FAIL: únicamente con evidencia reproducible suficiente; NOT_APPLICABLE: exclusión justificada; ACCEPTED_RISK: requiere excepción aprobada y vigente |

DOCUMENTADO no significa cumplido ni cerrado desde seguridad. Puede quedar NEEDS_REVIEW con gaps, o FAIL con remediación pendiente. El siguiente punto avanza cuando el análisis documental está completo, aunque requiera validación posterior; los gaps quedan en la ficha. Si la investigación está incompleta, conservar EN_CURSO.

Según [ADR-002](../../adr/ADR-002-asvs-evidence-model.md), una revisión de IA sin evidencia determinística solo produce NEEDS_REVIEW. Un test existente no ejecutado o un scanner sin alertas no demuestra PASS global. No transferir estos resultados documentales al piloto, al JSON oficial o a los reportes automáticos en esta fase.

## Ficha de cada requisito

Crear revisiones/<ID>.md al comenzar el punto; por ejemplo revisiones/v5.0.0-1.1.1.md. Usar esta estructura:

### Identificación

- ID y nivel mínimo.
- Texto oficial íntegro y separado de la interpretación local.
- Pregunta concreta que debe quedar respondida.
- Avance documental y resultado de seguridad.
- Fecha de revisión.

### Referencias evaluadas

| Componente | Ruta/repositorio | Commit completo | Cambios locales relevantes / hash | Ambiente |
| --- | --- | --- | --- | --- |

Fijar explícitamente Core consumido y cualquier clone original inspeccionado. Si solo se leyó código, indicar «revisión estática; runtime no evaluado».

### Aplicabilidad y alcance

Justificar componentes incluidos/excluidos, flujos y boundaries. Enumerar el alcance no inspeccionado que limite la conclusión.

### Evidencia y observaciones

| Ref | Componente | Archivo:línea / símbolo / evidencia | Método y ejecución real | Qué demuestra y qué no |
| --- | --- | --- | --- | --- |

La evidencia no debe incluir secretos, PII, tokens ni credenciales. Distinguir hechos verificados de hipótesis y contradicciones con KB/ADR.

### Resultado por componente y conclusión

Explicar qué se puede concluir y qué falta para una evaluación suficiente. Un resultado positivo en un componente no demuestra el requisito compartido completo.

### Hallazgos y propuesta documental

Para cada hallazgo: condición concreta, impacto, evidencia, responsable candidato, propuesta de cambio y validación futura. No atribuir vulnerabilidades confirmadas ni severidad definitiva si solo existe una sospecha.

### Pendientes y dependencias

Registrar evidencia faltante, quién puede aportarla, método para obtenerla y IDs relacionados. Enlazar propuestas distribuidas a otros repos; si no se distribuyeron, dejarlo explícito.

### Checkpoint para retomar

Qué se leyó, qué queda, próxima acción exacta, y si corresponde continuar este mismo ID o pasar al siguiente. Mantener este bloque aun al finalizar.

## Fuente y persistencia

El JSON oficial estaba sin commit al preparar el plan. Debe existir y conservar el hash indicado al retomar; si falta o cambió, detener la evaluación y comunicar el problema, sin descargar o sustituir otra versión silenciosamente.

El catálogo y los planes son contexto de preparación; AVANCE.md y las fichas son la autoridad del avance. No es necesario reescribir las 345 filas de catalogo.md con cada resultado.

Las ramas locales fix/owasp de admisiones, api-admisiones y Core se crearon en los clones originales. Sus worktrees iniciales están bajo el directorio temporal admisiones-owasp-20260916. Las rutas temporales pueden desaparecer: comprobar git worktree list y git branch antes de utilizarlas. Los cambios ya committed permanecen en los repos originales aunque desaparezca un worktree. El submódulo Core no fue actualizado.
