---
name: create-technical-decision-record-from-change
description: "Convierte un cambio tecnico relevante en un registro de decision tecnica claro y mantenible."
agent: ask
tools:
  - search/codebase
  - search/usages
  - edit
argument-hint: "[cambio o decision]"
---

<!-- ai-toolkit:toolkit profile=base path=.github/prompts/toolkit/create-technical-decision-record-from-change.prompt.md -->

Convierte `${input:scope:cambio, diff o decision}` en un registro de decision tecnica breve.

## Entrada sugerida

- `migracion a vitest y consolidacion del comando de test`

## Instrucciones

- Identifica la decision tecnica central y el contexto que la obliga.
- Resume alternativas solo si agregan valor real.
- Deja titulo, estado, contexto, decision y consecuencias; agrega alternativas solo si aclaran tradeoffs.
- Si el equipo usa la sigla ADR (`Architecture Decision Record`), tratalo como nombre alternativo para el mismo tipo de registro.
- Si el repo no usa archivos individuales para decisiones, integra la decision en `docs/ARCHITECTURE.md`.
- No agregues historia irrelevante ni decisiones no respaldadas por el cambio.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
