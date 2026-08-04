---
applyTo: '**'
---

<!-- ai-toolkit:toolkit profile=core path=.github/copilot-instructions.md -->

# DesarrolloORT AI Baseline

## Prioridades globales

1. Aplica economia de respuesta: resuelve con el menor contexto, pasos y salida posibles para completar bien la tarea.
2. Si la tarea exige exploracion amplia o una respuesta extensa, consulta antes de seguir.
3. El codigo generado debe seguir clean code basico y los patrones del repo.
4. Reutiliza convenciones existentes antes de introducir una abstraccion nueva.
5. La seguridad es obligatoria; ahorrar tokens nunca justifica omitir validacion o controles.
6. Si el pedido es ambiguo, asume el escenario comun y dilo; pregunta solo cuando seguir seria riesgoso.

## Reglas siempre disponibles

- Ponytail (lazy senior dev): `.github/instructions/toolkit/ponytail.instructions.md`
- Respuesta breve: `.github/instructions/toolkit/response-economy.instructions.md`
- Markdown: `.github/instructions/toolkit/markdown.instructions.md`
- Seguridad: `.github/instructions/toolkit/secure-code.instructions.md`
- UI/SCSS: antes de entregar, respeta `.stylelintrc.json`; no uses `px` en `font-size`, `line-height`, `width`, `height`, `margin`, `padding` ni `gap`. Usa tokens `--ort-sys-*` o `rem` permitido.

## Arquitectura Angular/API

- Solo los adapters en `endpoints/` pueden importar contratos generados desde `src/app/shared/api/generated/**`.
- Los metodos publicos de adapters deben exponer tipos propios de la feature y mapear explicitamente request/response; nunca retornar ni aceptar DTOs generados.
- `services/`, `facades/`, `models/`, componentes y specs de feature dependen de tipos propios de la feature o del adapter, no de DTOs generados.
- Las fechas de API permanecen como `string | null` en contratos de feature; la conversion a `Date` se hace explicitamente en facades/UI.
- `npm run update-api` y `npm run check-api-contracts` deben fallar si un adapter filtra generated o un endpoint se genera con `response: unknown`.

## Perfiles

- Common: siempre disponible; incluye response economy, Markdown, seguridad y CodeGraph.
- Front: solo en repos frontend; usa `.github/instructions/toolkit/angular-cli.instructions.md`, `.github/instructions/toolkit/angular-reactivity.instructions.md`, `.github/instructions/toolkit/frontend-ui.instructions.md` y `.github/skills/toolkit/angular-developer/SKILL.md`.
- Back: solo en repos backend; usa `.github/instructions/toolkit/dotnet-backend.instructions.md`.

No mezcles front y back en el mismo repo consumidor.
