---
name: angular-reactivity
description: "Reactividad Angular predecible con signals, computed(), effect() y timing estable."
applyTo: "**/*.ts, **/*.routes.ts, **/*.route.ts, **/*.config.ts"
---

<!-- ai-toolkit:toolkit profile=angular path=.github/instructions/toolkit/angular-reactivity.instructions.md -->

# Angular Reactivity

- Si el repo no usa signals, sigue el modelo reactivo ya adoptado y no las introduzcas por inercia.
- Si el repo usa signals, usa `computed()` para estado derivado y deja `effect()` solo para side effects reales.
- No uses `effect()` para espejar una signal en otra ni para sincronizar estado que pueda derivarse declarativamente.
- Crea `effect()` solo con injection context valido; evita inicializarlo tarde en hooks si puede vivir en un contexto explicito y estable.
- Evita escrituras reactivas durante el mismo ciclo de render o change detection si pueden disparar `NG0100`, loops o diferencias entre navegadores.
- Si el orden de render importa, prefiere flujo de datos determinista y handlers explicitos antes que depender de timing fragil.
- Si el problema ya existe en runtime, usa [angular-reactivity-diagnostics](../../skills/toolkit/angular-reactivity-diagnostics/SKILL.md).
