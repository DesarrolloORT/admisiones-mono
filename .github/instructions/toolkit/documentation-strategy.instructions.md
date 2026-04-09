---
description: Estandar documental canonico para decidir que documentar, donde hacerlo y con que nivel de detalle.
applyTo: "**/*.md, README.md, CONTRIBUTING.md, CHANGELOG.md, docs/**"
---

<!-- ai-toolkit:toolkit profile=base path=.github/instructions/toolkit/documentation-strategy.instructions.md -->

# Documentation Strategy

- Mantener una base documental minima, correcta y sostenible.
- Documentos base: `README.md`, `docs/SETUP.md` o equivalente, `docs/WORKFLOW.md`, `docs/ARCHITECTURE.md`, `CONTRIBUTING.md` y `CHANGELOG.md` si aplica.
- Clasifica el impacto como `docs-none`, `docs-light`, `docs-required` o `docs-technical-decision`.
- `docs-light`: setup, workflow, scripts, variables de entorno o troubleshooting.
- `docs-required`: UX visible, comportamiento, contratos publicos o integracion.
- `docs-technical-decision`: decision tecnica transversal.
- `docs-light`: actualiza `README.md`, `docs/SETUP.md`, `docs/WORKFLOW.md` o `CONTRIBUTING.md`.
- `docs-required`: actualiza changelog y la guia funcional, tecnica o de consumo relevante.
- `docs-technical-decision`: actualiza `docs/ARCHITECTURE.md` o un registro de decision tecnica.
- Adapta el detalle al tipo de repo: app frontend, template, libreria de componentes, design system o AI toolkit.
- La documentacion se actualiza junto con el cambio.
- Mantener poca documentacion correcta es mejor que mucha documentacion desactualizada.
- Antes de escribir, decide si el documento es tutorial, how-to, reference o explanation.
- Usa nombres exactos de rutas, scripts, comandos, archivos y contratos.
- Si cambian nombres de scripts, rutas, archivos, comandos o contratos, la documentacion debe reflejar exactamente los nuevos nombres.
- Si se usa una exencion documental, deja el motivo visible.
- Evita README gigantes, explicaciones duplicadas y comportamiento obsoleto.
