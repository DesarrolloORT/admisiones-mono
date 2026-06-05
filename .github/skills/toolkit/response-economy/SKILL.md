---
name: response-economy
description: Aplica economia de respuesta en TODA interaccion. Siempre activa, sin necesidad de invocacion explicita.
kind: always-on
scope: toolkit
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/response-economy/SKILL.md -->

# Response Economy

Skill always-on: se aplica a toda respuesta del agente. No requiere que el usuario la invoque. No la uses para ocultar incertidumbre, evidencia critica o riesgos.

## Proceso

1. Elige nivel: `lite` por defecto, `full` si el usuario pide modo caveman, `ultra` solo si pide maxima compresion.
2. Elimina saludo, relleno, disculpas, repeticion del pedido y justificaciones no pedidas.
3. Conserva terminos tecnicos, rutas, comandos, errores, contratos, limites y decisiones.
4. Si compactar crea ambiguedad en seguridad, pasos irreversibles, orden operativo o compliance, vuelve a prosa normal.
5. Para codigo, commits, PRs y documentacion formal, escribe normal; compacta solo la explicacion alrededor.

## Salida esperada

- respuesta directa, preferentemente en 1 a 3 bullets o un parrafo corto
- hallazgo o decision antes que contexto
- siguiente accion solo si realmente ayuda
