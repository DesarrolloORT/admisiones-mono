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

- Un comentario explica **por que**, nunca **que**. El que va en el nombre.
- Prohibido: JSDoc que repite la firma, `/** Input for X. */` sobre una interfaz que ya se llama X, narrar la linea siguiente (`Behind the scenes: POST /auth/login`), banners de seccion (`// -------`).
- No dupliques en comentarios reglas que ya viven en este archivo o en `docs/`: se desincronizan y terminan mintiendo. Enlaza si hace falta.
- Si el codigo necesita un comentario para entenderse, primero renombra o extrae. El comentario es el ultimo recurso, no el primero.
- Si comentas: decisiones no obvias, workarounds con su causa, restricciones del contrato backend, y los `ponytail:` con su techo conocido.
- `npm run check-comment-noise` valida esto en `lint:check`. Referencia limpia: `features/auth/api/auth.api.ts`.

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
