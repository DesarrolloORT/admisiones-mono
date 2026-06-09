---
name: generate-project-map
description: 'Genera un mapa estructural del repositorio optimizado para agentes AI. Fallback ligero cuando no se usa CodeGraph MCP.'
agent: ask
tools:
  - search/codebase
  - edit
argument-hint: '[profundidad: shallow | deep]'
---

<!-- ai-toolkit:toolkit profile=base path=.github/prompts/toolkit/generate-project-map.prompt.md -->

Genera un mapa estructural (project map) para este repositorio y escribelo como `.github/instructions/toolkit/project-map.instructions.md`.

> **Nota**: si el repo usa [CodeGraph MCP](https://github.com/colbymchenry/codegraph) (perfil `mcp-codegraph`), este archivo es innecesario — el MCP server provee navegacion semantica en tiempo real. Usa este prompt solo como fallback estatico.

## Formato del archivo generado

```markdown
---
name: project-map
description: 'Mapa estructural del repositorio para navegacion rapida del agente.'
applyTo: '**'
---

# Project Map

(tree aqui)
```

## Reglas de generacion

1. Analiza la estructura real del workspace (src/, lib/, app/, packages/, etc.).
2. Incluye solo directorios y archivos relevantes para un agente: entrypoints, routers, modelos, servicios, configuracion principal.
3. Omite archivos generados, node_modules, dist, coverage, assets estaticos triviales.
4. Agrega un comentario breve (#) solo a archivos cuyo proposito no sea obvio por el nombre.
5. Profundidad maxima: 4 niveles. Si hay mas, agrupa con `...`.
6. Si el repo tiene `AGENTS.md` o `CLAUDE.md`, actualiza tambien la seccion `## Codegraph` en esos archivos con el mismo tree.
7. Consulta la guia oficial de CodeGraph para alinearte con su formato: [https://github.com/colbymchenry/codegraph](https://github.com/colbymchenry/codegraph)

## Salida esperada

- Archivo `.github/instructions/toolkit/project-map.instructions.md` creado/actualizado.
- Si existen `AGENTS.md` y/o `CLAUDE.md`, la seccion `## Codegraph` sincronizada.

