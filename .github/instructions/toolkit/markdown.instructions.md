---
name: 'Markdown Documentation Standards'
description: 'Convenciones para prompts, instrucciones y documentacion Markdown.'
applyTo: '.github/**/*.md'
---

<!-- ai-toolkit:toolkit profile=base path=.github/instructions/toolkit/markdown.instructions.md -->

# Markdown y customizaciones

- Usa frontmatter solo cuando el tipo de archivo lo soporte y solo con campos validos para VS Code.
- Prefiere listas cortas y enlaces relativos en lugar de repetir bloques largos de contexto.
- Cuando una customizacion depende de otra, enlazala por Markdown en vez de duplicar las reglas.
- En prompts y customizaciones con herramientas, define solo las herramientas necesarias para la tarea.
- En skills, deja claro cuando aplicar la skill, el procedimiento y los recursos locales que puede consultar.
