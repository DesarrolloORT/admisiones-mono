---
name: docs-generate-readme
description: "Genera o reestructura un README accionable, verificable y orientado a la audiencia."
agent: ask
tools:
  - search/codebase
  - search/usages
  - edit
argument-hint: "[ruta del paquete o modulo]"
---
<!-- ai-toolkit:toolkit profile=base path=.github/prompts/toolkit/docs-generate-readme.prompt.md -->

Genera o reestructura el `README.md` de `${input:scope:paquete, modulo o carpeta}`.

Respeta [Markdown](../../instructions/toolkit/markdown.instructions.md).

## Entrada sugerida

- `packages/ai-catalog`

## Enfoque

- Usa solo informacion verificable del workspace.
- Ajusta el README a la audiencia principal si puede inferirse; si no, asume usuarios tecnicos y mantenedores.
- Explica setup, uso rapido, comandos reales, limites y donde seguir leyendo.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
