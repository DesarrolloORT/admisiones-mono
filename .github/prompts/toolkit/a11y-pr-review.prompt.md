---
name: a11y-pr-review
description: "Revisa cambios con foco en WCAG 2.2 AA, semantica, teclado y estados."
agent: ask
tools:
  - search/codebase
  - search/usages
argument-hint: "[archivos o diff]"
---
<!-- ai-toolkit:toolkit profile=base path=.github/prompts/toolkit/a11y-pr-review.prompt.md -->

Revisa `${input:scope:diff, archivos o flujo}` desde accesibilidad.

Usa [Frontend UI](../../instructions/toolkit/frontend-ui.instructions.md) como referencia base.

## Entrada sugerida

- `diff de modal de checkout y formulario de pago`

## Enfoque

- Prioriza teclado, foco, semantica, labels, nombre/rol/valor, errores y estados.
- Separa findings confirmados en codigo de dudas que solo puedan validarse en runtime o con lector de pantalla.
- Si el resultado depende de un componente base, browser o framework, explicita el supuesto.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
