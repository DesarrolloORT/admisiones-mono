<!-- ai-toolkit:toolkit profile=angular path=.github/skills/toolkit/angular-reactivity-diagnostics/references/patterns.md -->

# Patrones a revisar

## Anti-patrones frecuentes

- `effect()` en `ngOnInit` o `ngAfterViewInit` sin contexto de inyeccion valido.
- `effect()` que escribe en otra signal cuando bastaria con `computed()`.
- `afterNextRender(() => effect(...))` cuando el `effect()` pierde el contexto original.
- actualizaciones reactivas que alteran estado durante el mismo ciclo de deteccion.
- `effect()` con escrituras multiples que crean loops indirectos.

## Patrones preferidos

- Derivar estado con `computed()` cuando el valor es puro.
- Mantener `effect()` para side effects reales, no para sincronizacion de estado derivable.
- Aislar inicializacion imperativa en un punto de ciclo de vida claro.
- Confirmar el contexto de inyeccion cuando un `effect()` se declara fuera del constructor.
