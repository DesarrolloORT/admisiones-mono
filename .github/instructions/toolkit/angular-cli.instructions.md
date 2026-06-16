---
name: angular-cli
description: 'Implementacion Angular alineada con Angular CLI y patrones del workspace.'
applyTo: '**/*.ts, **/*.html, **/*.scss, **/*.sass, **/*.css, **/*.routes.ts, **/*.route.ts, **/*.config.ts'
---

<!-- ai-toolkit:toolkit profile=angular path=.github/instructions/toolkit/angular-cli.instructions.md -->

# Angular CLI

- **No uses sufijos de tipo en nombres de archivo.** Este repo usa `nombre.ts` en vez de `nombre.component.ts`, `nombre.service.ts`, `nombre.guard.ts`, `nombre.interceptor.ts`, `nombre.directive.ts` o `nombre.pipe.ts`. Lo mismo para specs: `nombre.spec.ts`.
- Sigue naming, estructura y separacion de archivos del workspace Angular.
- Prefiere standalone si el workspace ya lo adopto; usa NgModules solo cuando siga vigente.
- Respeta selectors, templates, estilos, tests y scaffolding del repo.
- Si el repo ya usa control flow moderno, signals o providers funcionales, extiendelo; si no, no lo introduzcas.
- Trata cada componente Angular como integracion del workspace.
- Para design system, tokens, accesibilidad y drift, sigue `frontend-ui`.
- Para `signals`, `computed()` y `effect()`, sigue [Angular Reactivity](./angular-reactivity.instructions.md).
- Consume componentes y variantes Angular del design system antes de HTML y estilos custom.
- Mantiene la logica de presentacion y estado en el componente; extrae integracion o reglas compartidas a servicios o stores existentes.
- En formularios, sigue el patron del repo y exige labels, errores, foco, teclado y feedback claros.
- No inventes wrappers o variantes Angular nuevas del design system sin gap y justificacion.
- Asume impacto documental si cambian exports publicos, rutas compartidas, contratos reutilizados o componentes publicos.
- Usa comentarios tecnicos solo cuando agreguen valor real.
