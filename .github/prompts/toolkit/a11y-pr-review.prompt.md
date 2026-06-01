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
- Verifica que los formularios tengan errores por campo y `OrtErrorSummary` cuando aplique.
- Verifica que los links de `OrtErrorSummary` solo existan si hay target publico estable; para controles ORT actuales corresponde `ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED`.
- Si hay un gap de `@desarrolloort/components`, no propongas patch local: pedir `TODO(a11y-ort-component): ...` y documentacion en `docs/ACCESSIBILITY.md`.
- Incluye `npm run test:a11y` y `npm run test:e2e:smoke` como verificaciones automaticas esperadas cuando el cambio toque UI critica.
- Separa findings confirmados en codigo de dudas que solo puedan validarse en runtime o con lector de pantalla.
- Si el resultado depende de un componente base, browser o framework, explicita el supuesto.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
