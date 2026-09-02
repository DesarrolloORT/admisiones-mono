# DesarrolloORT AI Baseline

Usa .github/copilot-instructions.md como baseline compartido del equipo.

## Prioridades

1. Resuelve con el menor contexto y la menor salida util posible.
2. Sigue patrones existentes del repo antes de introducir abstracciones.
3. Trata seguridad como restriccion de generacion.
4. Carga instrucciones, prompts o skills solo cuando aporten contexto real.

## Reglas siempre activas

- Ponytail (lazy senior dev): .github/instructions/toolkit/ponytail.instructions.md
- Respuesta breve: .github/instructions/toolkit/response-economy.instructions.md
- Seguridad: .github/instructions/toolkit/secure-code.instructions.md
- UI/SCSS: antes de entregar, respeta `.stylelintrc.json`; no uses `px` en `font-size`, `line-height`, `width`, `height`, `margin`, `padding` ni `gap`. Usa tokens `--ort-sys-*` o `rem` permitido.

## Comentarios en codigo

- El codigo se explica solo. Un comentario es el ultimo recurso, nunca el primero.
- **Dentro del cuerpo de una funcion no va prosa.** Un comentario ahi es la senal de que falta un nombre: extrae la condicion a un predicado nombrado, el bloque a un metodo privado, el valor magico a una constante, las señales que observa un `effect` a un `computed` que las nombre. Si escribiste tres lineas de `//` para explicar cinco de codigo, el problema es el codigo.
- "Explica el por que" NO alcanza como excusa: un comentario correcto y bien escrito sobre codigo poco claro sigue siendo codigo poco claro. Primero arregla el codigo; despues fijate si el comentario todavia hace falta.
- Prohibido: JSDoc que repite la firma, `/** Input for X. */` sobre una interfaz que ya se llama X, narrar la linea siguiente (`Behind the scenes: POST /auth/login`), banners de seccion (`// -------`).
- El racional de dominio (por que el flujo funciona asi, que bug lo origino, que escenario cubre) va a `docs/flujos/` con su `businessId`, no al codigo: ahi se versiona, se busca y no se desincroniza.
- Sobrevive solo lo que no puede vivir ni en un nombre ni en `docs/`: una restriccion externa no evidente (quirk de un sistema legacy, contrato del backend) y los `ponytail:` con su techo conocido.
- `npm run check-comment-noise` lo valida en `lint:check`: maximo 2 lineas `//` seguidas. Referencia limpia: `features/auth/api/auth.api.ts`.

## Arquitectura Angular/API

- Solo los adapters en `api/` (`features/*/api/*.api.ts`) pueden importar contratos generados desde `src/app/shared/api/generated/**`.
- Los metodos publicos de adapters deben exponer tipos propios de la feature y mapear explicitamente request/response; nunca retornar ni aceptar DTOs generados.
- `services/`, `facades/`, `models/`, componentes y specs de feature dependen de tipos propios de la feature o del adapter, no de DTOs generados.
- Un service existe solo si aporta comportamiento; si un metodo solo reenvia al adapter, el consumidor (page, component, resolver o facade) inyecta el adapter. No hay capa `store/`: el estado del flujo vive en la facade provista por la page o en una factory de `models/`.
- Las fechas de API permanecen como `string | null` en contratos de feature; la conversion a `Date` se hace explicitamente en facades/UI.
- `npm run update-api` y `npm run check-api-contracts` deben fallar si un adapter filtra generated o un endpoint se genera con `response: unknown`.

## Knowledge hub

- `docs/index.md` es el catalogo central de comportamiento y autoridades.
- Antes de cambiar logica de login, registro o inscripciones, leer la pagina con el `businessId` correspondiente en `docs/flujos/`.
- Actualizar la pagina canonica en el mismo PR o declarar `docs-none: <motivo>`.
- Usar CodeGraph para codigo frontend y los enlaces configurados por el portal para evidencia backend; no duplicar DTOs ni reglas internas.

## Perfiles

- common: baseline comun y CodeGraph.
- front: reglas Angular/UI, si fueron instaladas.
- back: reglas .NET backend, si fueron instaladas.
