---
name: document-component-public-api
description: Documenta la superficie publica de componentes, contratos o APIs de frontend para consumo y mantenimiento.
argument-hint: "[componente o contrato]"
kind: workflow
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/document-component-public-api/SKILL.md -->

# Document Component Public API

Usa esta skill cuando se crea o modifica un componente, directiva, servicio o contrato publico de frontend.

## Contenido minimo

- para que sirve
- cuando usarlo
- ejemplo basico
- configuracion publica
- restricciones y edge cases
- compatibilidad o breaking changes si los hay

## Reglas

1. Escribe para consumidores del contrato, no para quien lo implemento.
2. Usa nombres exactos de inputs, outputs, eventos y tipos.
3. No ocultes estados invalidos, limites ni dependencias.
