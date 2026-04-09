---
name: document-repo-foundation
description: Ordena la base documental minima de un repo cuando esta dispersa, mezclada u obsoleta.
argument-hint: "[repo o area]"
kind: workflow
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/document-repo-foundation/SKILL.md -->

# Document Repo Foundation

Usa esta skill cuando un repositorio no tenga base documental suficiente o cuando la documentacion principal este dispersa.

## Proceso

1. Identifica el tipo de repo.
2. Releva scripts, workflows y estructura observable.
3. Separa contenido operativo, arquitectonico y de contribucion.
4. Reutiliza contenido valido antes de crear texto nuevo.
5. Elimina o consolida duplicados.
6. Deja pendientes visibles si falta informacion no inferible.

## Resultado esperado

- `README.md`
- `docs/SETUP.md` o equivalente
- `docs/WORKFLOW.md`
- `docs/ARCHITECTURE.md`
- `CONTRIBUTING.md`
- `CHANGELOG.md` si aplica
