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
- `services/`, `facades/`, `models/`, componentes y specs de feature deben depender de tipos propios de la feature o del adapter correspondiente, no de DTOs generados.
- Si un contrato backend necesita exponerse fuera de `endpoints/`, define un tipo/mapper local de feature y mantén el DTO generado encapsulado en el endpoint adapter.

## Perfiles

- common: baseline comun y CodeGraph.
- front: reglas Angular/UI, si fueron instaladas.
- back: reglas .NET backend, si fueron instaladas.
