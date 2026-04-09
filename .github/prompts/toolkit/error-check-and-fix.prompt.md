---
name: error-check-and-fix
description: "Analiza fallos de test o build y aplica la correccion minima viable."
agent: ask
tools:
  - read/terminalLastCommand
  - search/codebase
  - search/usages
  - edit
argument-hint: "[comando] [salida de error opcional]"
---
<!-- ai-toolkit:toolkit profile=testing path=.github/prompts/toolkit/error-check-and-fix.prompt.md -->

Analiza y corrige el error de `${input:command:build, test o comando}` con el cambio minimo viable.

## Entrada sugerida

- `npm test`

## Enfoque

- Reproduce o triangula el fallo antes de editar.
- Prioriza la causa raiz; no tapes problemas de entorno, datos o configuracion con cambios cosmeticos.
- Mantene el fix acotado al area afectada y evita refactors laterales.
- Despues del arreglo, verifica el mismo comando o una comprobacion equivalente.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
