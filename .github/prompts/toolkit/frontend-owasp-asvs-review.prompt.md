---
name: frontend-owasp-asvs-review
description: "Ejecuta una revision OWASP ASVS frontend y devuelve findings accionables."
agent: ask
tools:
  - search/codebase
  - search/usages
argument-hint: "[ruta, flujo o modulo]"
---
<!-- ai-toolkit:toolkit profile=security path=.github/prompts/toolkit/frontend-owasp-asvs-review.prompt.md -->

Realiza una revision de `${input:scope:ruta, flujo o modulo}` usando [frontend-owasp-asvs](../../skills/toolkit/frontend-owasp-asvs/SKILL.md).

## Entrada sugerida

- `src/app/auth y consumo del token de sesion`

## Enfoque

- Delimita el alcance y que superficies frontend quedan cubiertas.
- Prioriza findings confirmados en codigo; separa hipotesis o dependencias del backend.
- Clasifica cada hallazgo por severidad y explotabilidad.
- Sugiere remediaciones concretas y proporcionales.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
