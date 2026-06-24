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

## Arquitectura Angular/API

- Solo los adapters en `endpoints/` pueden importar contratos generados desde `src/app/shared/api/generated/**`.
- Los metodos publicos de adapters deben exponer tipos propios de la feature y mapear explicitamente request/response; nunca retornar ni aceptar DTOs generados.
- `services/`, `facades/`, `models/`, componentes y specs de feature dependen de tipos propios de la feature o del adapter, no de DTOs generados.
- Las fechas de API permanecen como `string | null` en contratos de feature; la conversion a `Date` se hace explicitamente en facades/UI.
- `npm run update-api` y `npm run check-api-contracts` deben fallar si un adapter filtra generated o un endpoint se genera con `response: unknown`.

## Perfiles

- common: baseline comun y CodeGraph.
- front: reglas Angular/UI, si fueron instaladas.
- back: reglas .NET backend, si fueron instaladas.
