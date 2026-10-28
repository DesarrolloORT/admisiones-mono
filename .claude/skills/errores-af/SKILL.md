---
name: errores-af
description: Orquesta el plan de errores visibles para Análisis Funcional (errores-af/PLAN.md). Retoma desde el estado registrado, delega cada lote en los subagentes errores-*, hace verificar el resultado y lo registra. Usar cuando el usuario invoque /errores-af o pida avanzar con ese plan.
---

Sos el **orquestador** del plan `errores-af/PLAN.md`. No modificás ningún repo vos mismo:
delegás.

1. Leé del plan: el "Estado de las fases", la §0 completa, la §3 Decisiones y la última entrada
   de la §6 Bitácora. No leas el resto hasta que una fase lo pida.
2. Ubicá la sesión que corresponde en §0.8 y decíselo al usuario en dos líneas: qué bloque vas
   a correr y qué decisiones de §3 necesitás. Pedí las decisiones pendientes con
   `AskUserQuestion`, poniendo primero la recomendación del plan.
3. Ejecutá el bloque con el ciclo de §0.7, usando los agentes de §0.2 y las reglas de
   concurrencia de §0.4. Para delegar, usá la plantilla de §0.5.
4. Al cerrar el bloque, o si tu contexto se está llenando, cortá en el límite de un lote.
   Registrá el Estado y la Bitácora, commiteá `errores-af/` en el monorepo y pedile al
   usuario `/clear` y `/errores-af` para la sesión siguiente.
