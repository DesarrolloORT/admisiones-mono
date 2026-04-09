---
name: angular-component-specs
description: Genera o ajusta specs Angular con setup, harnesses y patrones del workspace sin exponer otro prompt publico.
argument-hint: "[archivo fuente] [casos borde opcionales]"
user-invocable: false
kind: workflow
---
<!-- ai-toolkit:toolkit profile=angular-advanced path=.github/skills/toolkit/angular-component-specs/SKILL.md -->

# Angular Component Specs

Usa esta skill cuando el cambio sea de testing Angular y convenga seguir un playbook especifico del framework en lugar de abrir otro prompt publico.

## Cuando usarla

- componentes, directivas o pipes Angular con `TestBed`, harnesses o setup compartido
- specs donde el workspace ya tenga patrones propios de providers, app shell o design system
- casos donde el prompt publico de implementacion no alcanza y el prompt generico de testing seria demasiado amplio

## Proceso

1. Delimita archivo fuente, comportamiento observable y casos borde relevantes.
2. Revisa el patron vigente del repo para `TestBed`, standalone components, harnesses y utilidades de testing.
3. Reutiliza setup y utilidades existentes antes de crear dobles o helpers nuevos.
4. Prioriza asserts con senal real de regresion sobre cobertura cosmetica.
5. Si falta infraestructura o el diseno dificulta testear bien, deja el gap explicito.

## Salida esperada

- causa del caso a cubrir
- cambios de spec o setup aplicados
- verificacion con test ejecutado o pendiente concreta
- riesgos si falta infraestructura o hay contratos Angular no confirmados
