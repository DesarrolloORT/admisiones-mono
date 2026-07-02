---
name: frontend-ui
description: 'Implementacion de UI guiada por design system, accesibilidad y patrones existentes.'
applyTo: '**/*.tsx, **/*.jsx, **/*.vue, **/*.svelte, **/*.astro, **/*.html, **/*.css, **/*.scss, **/*.less'
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
- En SCSS, antes de entregar, valida contra `.stylelintrc.json`: no uses `px` en `font-size`, `line-height`, `width`, `height`, `margin`, `padding` ni `gap`; usa tokens del design system o `rem` permitido.
- Si falta el componente exacto y el repo declara `designSystem.figmaMcpServer`, consulta Figma MCP antes de inventar variantes.
- Si no encuentras el token exacto, revisa docs, themes, tokens y reglas de estilo antes de introducir un valor nuevo.
- Si no hay docs ni Figma, reutiliza primitives, tokens, layouts y patrones locales; explicita el supuesto y cualquier gap.
- Si hay drift entre una implementacion ad hoc y el design system, prevalece el design system y el gap debe quedar explicitado.

## Accesibilidad

- Toda UI nueva debe cumplir WCAG 2.2 AA: semantica correcta, labels, teclado, foco visible, estados, errores, contraste y feedback claro.
- Prefiere HTML semantico y componentes base accesibles antes que wrappers opacos o divs sin rol.
- No sacrifiques accesibilidad por estilo visual ni por velocidad.
- Si falta soporte accesible en `@desarrolloort/components`, no parches la app ni uses overrides contra DOM o clases internas de ORT. Deja `TODO(a11y-ort-component): ...` en el punto de uso y documenta el gap en `docs/ACCESSIBILITY.md` para elevarlo a la libreria.
- Para `OrtErrorSummary`, habilita links al campo solo si el componente expone un target publico estable. En controles ORT actuales usa `ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED` y no dependas de ids/clases internas generadas por ORT.
- Si la UI toca un flujo critico, agrega o actualiza Playwright `@smoke`/`@regression` segun `docs/E2E-GUARDRAILS.md`. Para features grandes nuevas, usa acceptance-first desde Figma e historia funcional.
