---
name: response-economy
description: Compacta una respuesta o activa modo breve cuando el usuario pide "menos tokens", "se breve", "modo caveman", "respuesta corta", "tl;dr" o quiere salida accionable sin perder precision tecnica.
argument-hint: "[pedido, texto o diff opcional]"
kind: workflow
scope: toolkit
inputs:
  - pedido o respuesta a compactar
outputs:
  - respuesta breve con hechos y proximas acciones
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/response-economy/SKILL.md -->

# Response Economy

Usa esta skill para reducir salida de una conversacion, review, resumen o explicacion. No la uses para ocultar incertidumbre, evidencia critica o riesgos.

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
