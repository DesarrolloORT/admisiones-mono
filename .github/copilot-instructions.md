---
applyTo: "**"
---

<!-- ai-toolkit:toolkit profile=base path=.github/copilot-instructions.md -->

# DesarrolloORT AI Baseline

## Prioridades globales

1. Resuelve con el menor contexto, cantidad de pasos y salida posibles para completar bien la tarea.
2. Si la tarea exige exploracion amplia, analisis profundo o una respuesta extensa, consulta al usuario antes de seguir.
3. El codigo generado debe seguir clean code basico y alinearse con patrones, convenciones y arquitectura del proyecto.
4. Todo artefacto identificable del design system del repo es la fuente de verdad para UI; si hay drift, prevalece el design system y se explicita el gap.
5. La seguridad es obligatoria; ahorrar tokens nunca justifica omitir validacion, controles o una variante segura.

- Trabaja con cambios pequenos y verificables.
- Reutiliza el patron actual del repo antes de introducir uno nuevo.
- Carga instructions, prompts y skills solo cuando aporten contexto real.
- Si el pedido cambia diseno, contrato publico o una decision tecnica compartida, aclara el supuesto antes de expandir.
- Prioriza codigo, tests y verificacion por encima de reportes largos.

Consulta las reglas especificas por stack:

- [Markdown](./instructions/toolkit/markdown.instructions.md)
- [Frontend UI](./instructions/toolkit/frontend-ui.instructions.md)
- [Secure Code](./instructions/toolkit/secure-code.instructions.md)

Para cambios con impacto documental, aplica este baseline:

- [Documentation Strategy](./instructions/toolkit/documentation-strategy.instructions.md)
- [Documentation Writing](./instructions/toolkit/documentation-writing.instructions.md)
