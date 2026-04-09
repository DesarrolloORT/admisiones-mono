---
name: frontend-ui
description: "Implementacion de UI guiada por design system, accesibilidad y patrones existentes."
applyTo: "**/*.tsx, **/*.jsx, **/*.vue, **/*.svelte, **/*.astro, **/*.html, **/*.css, **/*.scss, **/*.less"
---

<!-- ai-toolkit:toolkit profile=base path=.github/instructions/toolkit/frontend-ui.instructions.md -->

# Frontend UI

## Design System

- Trata cualquier pantalla o componente nuevo como una extension del design system del repo, no como una pieza aislada.
- Todo artefacto identificable del design system del repo es la fuente de verdad para UI.
- Reutiliza componentes, variantes y primitives existentes antes de bajar a HTML y estilos custom.

## Tokens y fallbacks

- Consulta primero las docs del design system declaradas por el repo o inferidas por convencion.
- Usa tokens, aliases semanticos o variables del design system para color, spacing, radius, typography, shadows y z-index cuando existan.
- No hardcodees valores visuales si el repo ya define un token equivalente.
- Si falta el componente exacto y el repo declara `designSystem.figmaMcpServer`, consulta Figma MCP antes de inventar variantes.
- Si no encuentras el token exacto, revisa docs, themes, tokens y reglas de estilo antes de introducir un valor nuevo.
- Si no hay docs ni Figma, reutiliza primitives, tokens, layouts y patrones locales; explicita el supuesto y cualquier gap.
- Si hay drift entre una implementacion ad hoc y el design system, prevalece el design system y el gap debe quedar explicitado.

## Accesibilidad

- Toda UI nueva debe cumplir WCAG 2.2 AA: semantica correcta, labels, teclado, foco visible, estados, errores, contraste y feedback claro.
- Prefiere HTML semantico y componentes base accesibles antes que wrappers opacos o divs sin rol.
- No sacrifiques accesibilidad por estilo visual ni por velocidad.
