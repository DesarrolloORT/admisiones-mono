---
slug: /arquitectura/flujo-pasos
title: Anatomía del flujo paso a paso
description: Responsabilidades de stores, fachadas y componentes en una inscripción.
---

# Inscripciones — anatomía del flujo paso a paso

Esta feature es la **referencia** del patrón de "proceso paso a paso" del repo.
Si vas a trabajar en otro flujo multi‑paso (p. ej. [becas](../becas/README.md)),
leé esto primero: explica qué hace cada capa, cómo se pasa de un paso al
siguiente y dónde tocar para cada tipo de cambio.

> TL;DR del recorrido de una acción del usuario:
> **componente de paso → fachada → store (`ProcessFlow`) → la señal del paso
> cambia → la página re‑renderiza el componente del paso nuevo.**
> Nadie cambia el paso "a mano" desde el template; todo pasa por una fachada.

---

## Las capas, de afuera hacia adentro

```
routes  ──►  page (pages/inscripcion)  ──►  ProcessLayout (stepper + chrome)
                     │
                     ├─ providers: store + fachadas + forms store
                     │
                     ▼
              ProcessFacade  ◄── orquesta ──►  facades de sección
              (inscripcion-process)            (proposal / survey / payment)
                     │                                  │
                     ▼                                  ▼
              ProcessStore  ──► createProcessFlow   FormsStore (form groups)
              (paso actual, señales del flujo)      services ──► endpoints ──► API
```

### 1. `models/` — tipos y la definición de los pasos

- **`inscripcion-process.ts`** es el corazón del flujo: define el union
  `InscripcionStep` y la constante `INSCRIPCION_STEPS` (array **ordenado** de
  `ProcessStepDefinition`). **El orden de ese array es el orden del proceso.**
  Para agregar/quitar/reordenar pasos, editás solo este array.
- El resto de `models/` son tipos de la feature (formularios, payloads, respuestas
  de API mapeadas) y helpers puros (`inscripcion-flow-mappers`,
  `inscripcion-flow-policy`, `inscripcion-flow-options`). Regla del repo: las
  fechas viajan como `string | null` en los contratos y se convierten a `Date`
  explícitamente en fachadas/UI.

### 2. `shared/process-flow/process-flow.ts` — el motor genérico

No vive en la feature: es **compartido**. `createProcessFlow(definiciones, pasoInicial)`
devuelve un `ProcessFlow` con señales (`currentStep`, `currentIndex`, `stepItems`,
`canGoBack`, `canGoNext`) y acciones (`next`, `previous`, `goTo`, `reset`).
Avanza por **índice** sobre el array de definiciones. No sabe nada de
inscripciones ni de validación: solo "en qué paso estoy y cómo me muevo".

### 3. `store/` — estado del proceso

- **`inscripcion-process.ts`** (`InscripcionProcessStore`) envuelve
  `createProcessFlow(INSCRIPCION_STEPS, 'propuesta')` y le suma estado propio del
  flujo: `preEnrollmentResponse` y `checkpoint` (un contador que dispara el
  autoguardado del borrador). Es la **fuente de verdad del paso actual**.
- **`inscripcion-forms.ts`** (`InscripcionFormsStore`) crea y guarda todos los
  `FormGroup` y el `sectionConfig`. Las fachadas leen los forms desde acá.

Ambos stores se proveen **a nivel de página** (no en root), así cada inscripción
tiene su propio estado.

### 4. `facades/` — orquestación (acá vive la lógica de "pasar de paso")

- **`inscripcion-process.ts`** (`InscripcionProcessFacade`) es la fachada que la
  página conoce. Expone al template lo que el stepper necesita (`currentStep`,
  `stepItems`, `stepLabel`, `canGoBack`) y centraliza `continue()` / `back()`.
  `continue()` hace un `switch (currentStep())` y **delega** en la fachada de la
  sección activa; también maneja el borrador (autoguardado en `sessionStorage`,
  restauración, retomar desde el panel).
- **`inscripcion-proposal.ts` / `inscripcion-survey.ts` / `inscripcion-payment.ts`**
  son las fachadas de cada paso. Cada una valida su sección, llama a los services
  y, **cuando la sección está OK, llama a `this.process.flow.next()`** (o avanza
  de sub‑sección). Ahí es donde "se pasa de paso".

> **Quién decide el avance:** la fachada de sección, no el template. El template
> solo invoca `process.continue()` / `process.back()`.

### 5. `services/` + `endpoints/` — datos

- **`endpoints/`** son los adapters que importan los contratos generados
  (`shared/api/generated/**`). **Solo acá** se permiten esos imports. Mapean
  request/response a tipos propios de la feature.
- **`services/`** orquestan endpoints y exponen Observables con tipos de la
  feature. `inscripcion-draft.ts` persiste el borrador en `sessionStorage`.

### 6. `resolvers/` — precarga antes de entrar

Resuelven datos antes de activar la ruta (estado de encuesta inicial, detalle
para "retomar"). La página/fachada los lee de `route.snapshot.data`.

### 7. `pages/` + `components/` — UI

- **`pages/inscripcion`** es el contenedor: declara los `providers` (stores y
  fachadas), monta `app-process-layout` y, con un `@switch (process.currentStep())`,
  muestra el componente del paso actual. No tiene lógica de negocio.
- **`components/inscripcion-*-step`** son los pasos visuales. Hablan con su
  fachada y disparan `continue()` / `back()`.

---

## Cómo se pasa de un paso al siguiente (paso a paso)

1. El usuario completa el paso y toca **Continuar** en el componente del paso.
2. El componente llama a la fachada (directa o vía `ProcessFacade.continue()`).
3. La fachada de sección valida. Si falla, marca los forms y corta.
4. Si valida, hace su efecto (llamada a API si corresponde) y al confirmar llama
   a `this.process.flow.next()` (o avanza de sub‑sección dentro del mismo paso,
   como hace `survey` con sus secciones).
5. `createProcessFlow` actualiza la señal `currentIndex` → cambian `currentStep`
   y `stepItems`.
6. La página, al ser reactiva a `process.currentStep()`, renderiza el componente
   del paso nuevo; `ProcessLayout` actualiza el stepper.

`back()` es el espejo: retrocede sub‑sección si la hay, si no `flow.previous()`.

---

## "¿Dónde hago X?"

| Quiero…                                           | Voy a…                                                                                                                          |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| Agregar / quitar / reordenar un paso              | `models/inscripcion-process.ts` → `INSCRIPCION_STEPS` (y el `@switch` de la página + el `switch` de `ProcessFacade.continue()`) |
| Cambiar el título/overline de un paso             | `INSCRIPCION_STEPS`                                                                                                             |
| Cambiar cuándo se puede avanzar/volver            | la fachada de la sección (validación) y `canGoBack` en `ProcessFacade`                                                          |
| Cambiar la lógica de avance de un paso            | la fachada de ese paso (`proposal` / `survey` / `payment`)                                                                      |
| Agregar un campo a un formulario                  | `models/inscripcion-flow-forms.ts` (form) + el componente del paso                                                              |
| Llamar a un endpoint nuevo                        | `endpoints/` (adapter) → `services/` → la fachada                                                                               |
| Tocar el contrato con la API                      | **solo** en `endpoints/` (única capa que ve `generated/**`)                                                                     |
| Cambiar el chrome (header, stepper, botón cerrar) | `shared/ui/process-layout`                                                                                                      |
| Tocar el motor de pasos genérico                  | `shared/process-flow/process-flow.ts` (afecta a todas las features)                                                             |
| Persistir/retomar avances                         | `services/inscripcion-draft.ts` + `ProcessFacade` (draft)                                                                       |

---

## Reglas que el patrón da por sentadas

- El template **nunca** cambia el paso directamente: siempre vía una fachada.
- Solo los `endpoints/` importan de `shared/api/generated/**`; el resto usa tipos
  propios de la feature.
- Las fechas son `string | null` en los contratos; la conversión a `Date` es
  explícita en fachadas/UI.
- Stores y fachadas se proveen **en la página**, no en root.
