---
name: document-release-change
description: Resume cambios con impacto de release para consumidores y mantenedores.
argument-hint: "[diff o version]"
kind: workflow
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/document-release-change/SKILL.md -->

# Document Release Change

Usa esta skill cuando un cambio deba quedar reflejado en `CHANGELOG.md`, notas de release o documentacion de versionado.

## Reglas

1. Describe impacto, no solo archivos tocados.
2. Agrupa por tipo de cambio.
3. Explicita breaking changes y migraciones requeridas.
4. Excluye ruido interno si no agrega valor a consumidores.
