# Avance ASVS 5.0.0

Actualizado: 2026-09-16. Fase: análisis y documentación exclusivamente L1; fixes no autorizados.

## Checkpoint actual

- Requisito en curso: ninguno.
- Siguiente requisito: v5.0.0-1.3.2 (L1).
- Último requisito documentado: v5.0.0-1.3.1 (L1), NEEDS_REVIEW.
- Selección vigente: solo L1, en orden oficial; L2 reservado para una fase posterior con pedido explícito del usuario.
- Progreso L1: 6 DOCUMENTADO, 0 EN_CURSO, 64 POR_REVISAR (total 70).
- L2 reservado: 2 DOCUMENTADO y 181 POR_REVISAR (total 183); se preservan sus estados y fichas.
- Progreso general L1/L2 conservado: 8 DOCUMENTADO, 0 EN_CURSO, 245 POR_REVISAR.
- L3: 92 FUERA_L2; no se consideran NOT_APPLICABLE.
- Próxima acción: con un nuevo pedido de seguir, revisar únicamente v5.0.0-1.3.2 (L1), verificar Git/fuente y fijar refs antes de investigar.
- v5.0.0-1.1.1 documentado por revisión estática de frontend/API/Core; faltan equivalencia esquema/binding/sanitización, runtime y contrato de pagos. Evidencia y métodos pendientes en su ficha. No se aplicaron fixes.

- v5.0.0-1.1.2 documentado por revisión estática: codificación/serialización observada en DOM, HTTP/JSON y correo; pendientes receptor de pagos, correo entregado, runtime y cobertura de otros sinks. Ver ficha; no hay cumplimiento global demostrado.
- Checkpoint 1.2.2 sin commit por instrucción del usuario: AVANCE.md y revisiones/v5.0.0-1.2.2.md modificados/creados; copia Markdown API en worktree temporal fix/owasp (ref en ficha). No hacer commits ni pushs. Staging previo preservado; HEAD cambió externamente de 75b80a958b3a57d94625cd93461a72b90fa9fff8 a cd3f71e70e2a748afc2e405ce4054d72b4c1c9e6 incorporando solo documentación 1.2.1; el agente no hizo commits. Inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.2.md`.

- v5.0.0-1.2.1 documentado por revisión estática de contextos HTML/HTTP y proxy SOAP/XML. Pendientes: DOM y librería SVG, correo entregado, envelope SOAP, headers efectivos y otros consumidores Core; ver ficha. HEAD evaluado: 75b80a958b3a57d94625cd93461a72b90fa9fff8; Core consumido verificado: 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Estado inicial: solo graph.json sin seguimiento, staging vacío; cambios de esta iteración limitados a dos Markdown, sin fixes/commits/pushs.

- v5.0.0-1.2.2 documentado por revisión estática de URLs en frontend/API/Core: paths/query con escape y formulario de pago limitado a http/https. Pendientes: configuración de activación/fragmentos, proveedor de pagos, mailto, router/framework y protocolos/polling Azure. Resultado NEEDS_REVIEW; evidencia, límites y propuesta distribuida API en ficha. Sin fixes ni ejecución de tests/runtime.

- v5.0.0-1.2.3 documentado: serialización JavaScript/JSON en frontend/API/Core; pendientes bytes/round trip, parser externo de pagos, consumidores de SerializeJsonLog y visor de logs. Resultado NEEDS_REVIEW. HEAD evaluado f41cba032b800ba3cd17525e6608274644ea7e55; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Checkpoint sin commit por instrucción del usuario: AVANCE.md, ficha 1.2.3 y dos copias Markdown en worktrees API/Core fix/owasp (rutas/refs en ficha). Staging vacío preservado. Sin fixes, builds, tests, commits ni pushs. Para inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.3.md`.

- v5.0.0-1.2.4 documentado por revisión estática de consultas EF/LINQ, secuencias SQL y Redis API/Core; callers string encontrados usan constantes, pero las variantes genéricas no validan localmente el identificador. Resultado NEEDS_REVIEW; pendientes binding Devart, esquema/procedimientos/triggers Oracle, otros consumidores Core y persistencia externa de pagos. HEAD evaluado 7fea09ee286b932b579ff720d90726e9e0de72d2; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Archivos sin commit: AVANCE.md, revisiones/v5.0.0-1.2.4.md y dos propuestas docs/security/ASVS-v5.0.0-1.2.4-propuesta.md en worktrees API/Core fix/owasp (rutas/refs en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y cambios ajenos preservados. Inspeccionar: `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.4.md`. Continuar solo con un nuevo pedido: «Seguí security/asvs/planning/ITERACION.md y revisá únicamente v5.0.0-1.2.5». No hacer commits ni pushs.

- v5.0.0-1.2.5 DOCUMENTADO / NEEDS_REVIEW: no se localizaron sinks OS directos en C# API/Core; herramientas frontend leídas pasan arrays sin shell. Pendientes wrappers externos, selección de agentes con claves heredadas, runtime/hosting y servicios externos. HEAD evaluado 6c7c865482f1d5f1f401d6c5f23d43b1c1a036c1; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Sin commit: AVANCE.md, revisiones/v5.0.0-1.2.5.md y propuesta frontend temporal fix/owasp (ruta/ref en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.2.5.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-1.3.1». No hacer commits ni pushs.

- v5.0.0-1.3.1 DOCUMENTADO / NEEDS_REVIEW: sin editor HTML enriquecido ni sinks HTML explícitos localizados en frontend propio; API registra HtmlSanitizer global pero el filtro no recorre explícitamente elementos de colecciones. Listas reales de texto de encuesta llegan a entidades con normalización; falta demostrar un receptor HTML para confirmar impacto/aplicabilidad. Tests existentes leídos, no ejecutados; tests de registro solo comprueban DI no vacío. Pendientes componentes externos, versión/política resuelta, pipeline/runtime y otros consumidores Core/correo. HEAD 11a3319b053bde4744a47552af70596d22ea767c; Core 01239cdf6054dc5a450dcdaa3a867ae3204b61e7. Sin commit: AVANCE.md, revisiones/v5.0.0-1.3.1.md y propuesta API temporal fix/owasp (ruta/ref en ficha). Sin fixes/builds/tests/scans/commits/pushs; staging vacío y graph.json ajeno preservados. Inspeccionar `git diff -- security/asvs/planning/AVANCE.md` y `Get-Content security/asvs/planning/revisiones/v5.0.0-1.3.1.md`. Continuación solo con nuevo pedido: «Seguí ITERACION.md y revisá únicamente v5.0.0-1.3.2. No apliques fixes, commits ni pushs».

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
| v5.0.0-1.3.2 | L1 | POR_REVISAR | PENDING | — |
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
| v5.0.0-1.5.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-1.5.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-1.5.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-2.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-2.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-2.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-2.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-2.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-2.4.2 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.1.1 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.2.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.3.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.5 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.6 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-3.4.7 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.4.8 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-3.5.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.5.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-3.5.3 | L1 | POR_REVISAR | PENDING | — |
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
| v5.0.0-4.1.1 | L1 | POR_REVISAR | PENDING | — |
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
| v5.0.0-4.4.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-4.4.4 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.1.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.2.4 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.2.5 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.2.6 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.3.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-5.3.2 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-5.3.3 | L3 | FUERA_L2 | PENDING | — |
| v5.0.0-5.4.1 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.4.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-5.4.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.1.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.1.2 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.1.3 | L2 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.1 | L1 | POR_REVISAR | PENDING | — |
| v5.0.0-6.2.2 | L1 | POR_REVISAR | PENDING | — |
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
