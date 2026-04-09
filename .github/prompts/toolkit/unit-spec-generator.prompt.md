---
name: unit-spec-generator
description: "Genera o actualiza pruebas unitarias enfocadas, incluyendo gaps concretos de cobertura."
agent: ask
tools:
  - search/codebase
  - search/usages
  - edit
argument-hint: "[archivo fuente] [casos borde o gaps de cobertura opcionales] [archivo spec opcional]"
---
<!-- ai-toolkit:toolkit profile=testing path=.github/prompts/toolkit/unit-spec-generator.prompt.md -->

Genera o actualiza tests unitarios para `${input:source:archivo fuente}`.

## Entrada sugerida

- `src/lib/format-date.ts cubrir locale nulo y timezone invalida`
- `src/lib/math.ts lineas 48-63 sin cubrir en math.spec.ts`

## Enfoque

- Identifica API publica, contratos y comportamiento observable antes de escribir tests.
- Cubre happy path, errores esperados, bordes y regresiones probables.
- Si recibes lineas o ramas no cubiertas, traducelas a escenarios, entradas, mocks o estado concretos.
- Reutiliza utilidades, setup y estilo del repo; evita mocks y asserts innecesarios.
- Reutiliza el spec existente antes de crear archivos nuevos.
- Si una rama es dificil de cubrir por diseno, explica si conviene refactor en vez de test forzado.
- Si falta infraestructura o el diseno dificulta testear bien, deja el gap explicito.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
