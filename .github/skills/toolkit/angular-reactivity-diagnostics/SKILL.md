---
name: angular-reactivity-diagnostics
description: Diagnostica errores runtime de reactividad Angular con signals, zoneless change detection, timing y diferencias entre navegadores como Safari Mobile.
argument-hint: '[ruta o componente] [errores opcionales]'
---

<!-- ai-toolkit:toolkit profile=angular path=.github/skills/toolkit/angular-reactivity-diagnostics/SKILL.md -->

# Angular Reactivity Diagnostics

Usa esta skill cuando ya exista un bug runtime de reactividad Angular. Para codigo nuevo o refactors normales, las defaults viven en [Angular Reactivity](../../../instructions/toolkit/angular-reactivity.instructions.md).

## Cuando usarla

- `NG0100` o `ExpressionChangedAfterItHasBeenCheckedError`
- `NG0203` por `effect()` fuera de injection context
- `NG0600` por escrituras reactivas incorrectas
- diferencias entre Chrome y Safari Mobile
- componentes o servicios con `signals`, `computed`, `effect`, `afterNextRender` o `zoneless`

## Proceso

1. Delimita el error observable, el navegador afectado y el punto de ciclo de vida donde aparece.
2. Revisa la [guia de patrones](./references/patterns.md) antes de proponer cambios.
3. Clasifica el problema: contexto de inyeccion, estado derivado mal modelado, escritura sensible a render, loop reactivo o timing dependiente del navegador.
4. Prefiere reemplazar sincronizacion imperativa por `computed()` cuando el valor sea derivable.
5. Si el `effect()` es necesario, asegurate de que viva en un injection context valido y que solo haga side effects reales.
6. Si el bug es especifico de Safari Mobile, revisa orden de render, acoplamiento DOM/estado y dependencias de timing fragiles.

## Salida esperada

- Lista corta de anti-patrones encontrados
- Propuesta de correccion por archivo o componente
- Riesgos residuales si el problema depende del runtime o del navegador
