---
name: update-docs-from-diff
description: "Clasifica el impacto documental de un cambio y actualiza solo la documentacion minima necesaria."
agent: ask
tools:
  - search/codebase
  - search/usages
  - edit
argument-hint: "[diff o cambio]"
---

<!-- ai-toolkit:toolkit profile=base path=.github/prompts/toolkit/update-docs-from-diff.prompt.md -->

Analiza `${input:scope:diff o cambio}` y actualiza solo la documentacion minima necesaria.

## Entrada sugerida

- `diff de release y CLI`

## Instrucciones

- Clasifica el cambio como `docs-none`, `docs-light`, `docs-required` o `docs-technical-decision`.
- Justifica la clasificacion en una frase y determina que documentos tocar.
- Si falta un documento base, propon su creacion con alcance acotado.
- No inventes decisiones tecnicas no respaldadas por el diff o por el repo.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
