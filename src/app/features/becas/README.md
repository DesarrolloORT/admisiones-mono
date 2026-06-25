# Becas — guía de arranque del flujo paso a paso

Esta feature replica el patrón de proceso paso a paso de
[inscripciones](../inscripciones/README.md). **Antes de tocar nada, leé ese
README**: explica la anatomía completa (capas, cómo se pasa de paso, dónde hacer
cada cosa). Acá solo se documenta el **estado actual de becas** y **qué falta**.

---

## Mapa de equivalencias con inscripciones

| Concepto             | Inscripciones                                         | Becas                                                         |
| -------------------- | ----------------------------------------------------- | ------------------------------------------------------------- |
| Definición de pasos  | `models/inscripcion-process.ts` (`INSCRIPCION_STEPS`) | `models/scholarship-process.ts` (`SCHOLARSHIP_STEPS`) ✅      |
| Motor de pasos       | `shared/process-flow/process-flow.ts` (compartido)    | el mismo ✅                                                   |
| Store del proceso    | `store/inscripcion-process.ts`                        | `store/scholarship-process.ts` ✅                             |
| Fachada orquestadora | `facades/inscripcion-process.ts`                      | `facades/scholarship-process.ts` ✅                           |
| Fachadas de sección  | `proposal` / `survey` / `payment`                     | _pendiente_ ⬜                                                |
| Store de formularios | `store/inscripcion-forms.ts`                          | _pendiente_ ⬜                                                |
| Services + endpoints | `services/` + `endpoints/`                            | _pendiente_ ⬜                                                |
| Resolvers            | `resolvers/`                                          | _pendiente_ ⬜                                                |
| Persistencia (draft) | `services/inscripcion-draft.ts`                       | _pendiente_ ⬜                                                |
| Página contenedora   | `pages/inscripcion`                                   | `pages/fbr` ✅ (ya usa el flow)                               |
| Componentes de paso  | `components/inscripcion-*-step`                       | `components/scholarship-*-step` ✅ (UI; sin wiring de avance) |

✅ = ya armado como andamiaje · ⬜ = lo armás vos siguiendo el patrón de inscripciones.

---

## Qué ya está armado (andamiaje)

El **motor de pasos real** ya funciona. La página `fbr` dejó de usar un array
estático de pasos y ahora pasa por el flow:

- **`models/scholarship-process.ts`** — `ScholarshipStep` + `SCHOLARSHIP_STEPS`
  (`info-postulacion` → `info-personal` → `confirmacion`). Editá este array para
  agregar/quitar/reordenar pasos.
- **`store/scholarship-process.ts`** — `ScholarshipProcessStore`, envuelve
  `createProcessFlow(SCHOLARSHIP_STEPS, 'info-postulacion')`. Fuente de verdad del
  paso actual.
- **`facades/scholarship-process.ts`** — `ScholarshipProcessFacade`, expone al
  template `currentStep` / `stepItems` / `stepLabel` / `canGoBack` y centraliza
  `continue()` / `back()`.
- **`pages/fbr`** — provee store + fachada, monta `app-process-layout` con las
  señales de la fachada y elige el componente del paso con
  `@switch (process.currentStep())`.

El stepper, el subtítulo ("Paso N de 3 …") y el botón Volver ya salen del flow.

---

## Qué falta

1. **Wiring de avance por paso.** Hoy los botones "Continuar" de los componentes
   de paso (p. ej. `scholarship-personal-step`) no avanzan el flujo. Conectalos
   a `process.continue()` / `process.back()` (vía `output()` del componente o
   inyectando la fachada). **Regla:** el avance lo decide una fachada, nunca el
   template directamente.
2. **Fachadas de sección.** Por cada paso con lógica propia (validar, llamar a la
   API), creá una fachada como `proposal` / `survey` / `payment` en inscripciones
   e inyectala en `ScholarshipProcessFacade`; cada `case` de `continue()` delega
   en ella y, si valida, llama a `this.process.flow.next()`.
3. **Formularios.** Si los pasos comparten estado de forms, creá un
   `ScholarshipFormsStore` espejo de `InscripcionFormsStore`.
4. **Datos.** `endpoints/` (adapters, única capa que ve `shared/api/generated/**`)
   → `services/` → fachadas. Mapeá request/response a tipos propios de becas.
5. **Step de confirmación.** Creá `components/scholarship-confirmation-step` y
   montalo en el `@case ('confirmacion')` de `fbr.html` (hoy es un punto de
   extensión vacío).
6. **(Opcional) Persistencia y resolvers**, si el flujo necesita retomarse, igual
   que `inscripcion-draft` y los resolvers de inscripciones.

---

## Recordá

- El template nunca cambia el paso a mano: siempre vía `process.continue()` /
  `process.back()`.
- Solo `endpoints/` importa de `shared/api/generated/**`.
- Fechas como `string | null` en contratos; conversión a `Date` explícita en
  fachadas/UI.
- Store y fachadas se proveen en la página `fbr`, no en root.
