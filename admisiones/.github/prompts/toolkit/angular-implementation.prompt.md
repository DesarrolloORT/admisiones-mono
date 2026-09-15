---
name: angular-implementation
description: 'Implementa cambios Angular de happy path usando patrones del workspace, design system e instrucciones del perfil.'
agent: ask
tools:
  - search/codebase
  - search/usages
  - edit
argument-hint: '[ruta, flujo o feature] [alcance opcional]'
---

<!-- ai-toolkit:toolkit profile=angular path=.github/prompts/toolkit/angular-implementation.prompt.md -->

Implementa el cambio Angular indicado con el menor alcance coherente.

## Entrada sugerida

- `src/app/orders agregar loading y empty state en el listado`

## Reglas

- Usalo para cambios chicos o medianos en componente, template, estilos, formulario, wiring o flujo cercano.
- Revisa primero patrones Angular equivalentes del workspace y reutiliza el design system antes de proponer markup o estilos nuevos.
- Mantene standalone, signals, control flow y formularios alineados con el estilo ya adoptado por el proyecto.
- Si el cambio se expande a varios archivos coordinados, rutas, servicios o testing cruzado, divide el trabajo en pasos pequeños y explicita el plan antes de editar.
- Si aparece un bug runtime de `signals`, timing o diferencias entre navegadores, escala a [Angular Reactivity Diagnostics](../../skills/toolkit/angular-reactivity-diagnostics/SKILL.md).
- Si falta una primitive o variante del design system, explicita el gap en lugar de inventarla silenciosamente.

## Salida esperada

- `Causa`: objetivo, hallazgo o contexto que explica la respuesta.
- `Cambio`: cambio aplicado, propuesta concreta o findings priorizados.
- `Verificacion`: checks ejecutados, evidencia usada o pendiente concreta.
- `Riesgos`: gaps, supuestos o impacto no validado.
