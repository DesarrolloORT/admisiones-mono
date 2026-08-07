---
slug: /arquitectura/flujo-pasos
title: Anatomía del flujo paso a paso
description: Responsabilidades de stores, fachadas y componentes en una inscripción.
---

# Inscripciones — anatomía del flujo paso a paso

Esta feature es la **referencia** del patrón de "proceso paso a paso" del repo.
Si vas a trabajar en otro flujo multi‑paso (p. ej. [becas](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/src/app/features/becas/README.md)),
leé esto primero: explica qué hace cada capa, cómo se pasa de un paso al
siguiente y dónde tocar para cada tipo de cambio.

> TL;DR del recorrido de una acción del usuario:
> **componente de paso → fachada → store (`ProcessFlow`) → la señal del paso
> cambia → la página re‑renderiza el componente del paso nuevo.**
> Nadie cambia el paso "a mano" desde el template; todo pasa por una fachada.

---

## Las capas, de afuera hacia adentro

```
routes  ──►  page (pages/layout)  ──►  ProcessLayout (stepper + chrome)
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
  flujo: `preEnrollmentResponse`. Es la **fuente de verdad del paso actual**.
- **`inscripcion-forms.ts`** (`InscripcionFormsStore`) crea y guarda todos los
  `FormGroup` y el `sectionConfig`. Las fachadas leen los forms desde acá.

Ambos stores se proveen **a nivel de página** (no en root), así cada inscripción
tiene su propio estado.

### 4. `facades/` — orquestación (acá vive la lógica de "pasar de paso")

- **`inscripcion-process.ts`** (`InscripcionProcessFacade`) es la fachada que la
  página conoce. Expone al template lo que el stepper necesita (`currentStep`,
  `stepItems`, `stepLabel`, `canGoBack`) y centraliza `continue()` / `back()`.
  `continue()` hace un `switch (currentStep())` y **delega** en la fachada de la
  sección activa. Es además el **único inicializador**: en su constructor lee la
  intención + encuesta resueltas, llama a `deriveInitialInscripcionState` y aplica
  el resultado con `applyInitialState` (un solo lugar, orden determinístico, el
  paso del flujo se posiciona al final). Las fachadas de sección **no** se
  posicionan solas ni leen la ruta.
- **`inscripcion-proposal.ts` / `inscripcion-survey.ts` / `inscripcion-payment.ts`**
  son las fachadas de cada paso. Cada una valida su sección, llama a los services
  y, **cuando la sección está OK, llama a `this.process.flow.next()`** (o avanza
  de sub‑sección). Ahí es donde "se pasa de paso".

  `inscripcion-survey.ts` cierra el paso en una única cadena: guarda Documento y
  Foto en paralelo, guarda EncuestaInicial solo si ambos terminan correctamente y
  finalmente confirma la preinscripción.

> **Quién decide el avance:** la fachada de sección, no el template. El template
> solo invoca `process.continue()` / `process.back()`.

### 5. `services/` + `endpoints/` — datos

- **`endpoints/`** son los adapters que importan los contratos generados
  (`shared/api/generated/**`). **Solo acá** se permiten esos imports. Mapean
  request/response a tipos propios de la feature.
- **`services/`** orquestan endpoints y exponen Observables con tipos de la
  feature.

### 6. `resolvers/` — precarga e **intención de entrada**

Resuelven datos antes de activar la ruta. `inscription-initial-survey.resolver`
trae el estado de encuesta inicial (por persona). `inscription-detail.resolver`
decide la **intención de entrada** (`resolveEntryIntent`) a partir de la URL —no
del backend— y devuelve `InscripcionEntryResolved`:

- `nueva`: sin query params. Paso 1 **siempre** virgen y editable.
- `retomar`: con `idProducto`+`idProceso` (desde el panel); carga el detalle.
- `reactivar`: agrega `modo=reactivar`. El botón del dashboard hace
  `POST /enrollments/reactivate`, guarda transitoriamente su respuesta y navega con
  esta intención. El resolver consume esa respuesta para iniciar el pago sin repetir
  `GET /enrollments/details`; si falta por recarga o acceso directo, carga el detalle
  como fallback.

Si el detalle de fallback falla, la intención se conserva con `detail:null` y el flujo
continúa con la precarga mínima de los parámetros.

### 7. `models/inscription-entry.ts` — derivación pura del estado inicial

`deriveInitialInscripcionState(context)` es una **función pura sin efectos** que,
dado `(intención, detalle/respuesta de reactivación, encuesta)`, devuelve el estado
inicial completo: paso, slice de encuesta, slice de pago y si reanuda `En proceso`. Es
la **única fuente de verdad** de "en qué estado arranca la inscripción". Su contrato de negocio es la
tabla ejecutable `inscription-entry.spec.ts` (intención × estado × encuesta). El
backend manda sobre los datos; la intención manda sobre presentación/navegación
(por eso `nueva` nunca precarga el paso 1 aunque exista una encuesta previa).

### 8. `pages/` — UI

- **`pages/layout`** es el contenedor: declara los `providers` (stores y
  fachadas), monta `app-process-layout` y, con un `@switch (process.currentStep())`,
  muestra el componente del paso actual. No tiene lógica de negocio.
- **`pages/steps/inscripcion-*-step`** son los pasos visuales. Hablan con
  su fachada y disparan `continue()` / `back()`. El árbol de carpetas refleja
  quién renderiza a quién: `inscripcion-personal-step/sections/` contiene las
  6 secciones que **solo** ese paso usa (para ver quién le pasa `orientation`
  a una sección, el padre está en la carpeta de arriba, no disperso entre 15
  hermanos). Los diálogos de confirmación (en `layout` y en
  `inscripcion-confirmation-step`) usan el `ort-dialog` compartido del design
  system en lugar de un componente propio de la feature.

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

Dentro de la encuesta no hay guardados automáticos por completar expansibles. El
guardado y la confirmación se ejecutan juntos al cerrar el paso.

`back()` no es el espejo exacto de `continue()`: en inscripciones el flujo **solo
avanza**, porque cada paso completado ya quedó registrado en el backend. `back()`
retrocede sub‑sección (o cierra el lector) y nunca llama a `flow.previous()`;
`canGoBack` es `false` fuera del paso 2, y el botón de volver se oculta cuando no hay
nada hacia atrás. Un proceso que sí admita retroceder entre pasos puede seguir usando
`flow.previous()`: el motor `ProcessFlow` lo soporta.

---

## "¿Dónde hago X?"

| Quiero…                                             | Voy a…                                                                                                                                              |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Agregar / quitar / reordenar un paso                | `models/inscripcion-process.ts` → `INSCRIPCION_STEPS` (y el `@switch` de la página + el `switch` de `ProcessFacade.continue()`)                     |
| Cambiar el título/overline de un paso               | `INSCRIPCION_STEPS`                                                                                                                                 |
| Cambiar cuándo se puede avanzar/volver              | la fachada de la sección (validación) y `canGoBack` en `ProcessFacade`                                                                              |
| Cambiar la lógica de avance de un paso              | la fachada de ese paso (`proposal` / `survey` / `payment`)                                                                                          |
| Agregar un campo a un formulario                    | `models/inscripcion-flow-forms.ts` (form) + el componente del paso                                                                                  |
| Llamar a un endpoint nuevo                          | `endpoints/` (adapter) → `services/` → la fachada                                                                                                   |
| Tocar el contrato con la API                        | **solo** en `endpoints/` (única capa que ve `generated/**`)                                                                                         |
| Cambiar el chrome (header, stepper, botón cerrar)   | `shared/ui/process-layout`                                                                                                                          |
| Tocar el motor de pasos genérico                    | `shared/process-flow/process-flow.ts` (afecta a todas las features)                                                                                 |
| Cambiar qué muestra cada intención/estado al entrar | `models/inscription-entry.ts` (`deriveInitialInscripcionState`) + su tabla `inscription-entry.spec.ts`; la aplica `ProcessFacade.applyInitialState` |
| Retomar/reactivar una inscripción desde el panel    | `resolvers/inscription-detail.resolver` (`resolveEntryIntent`) + la derivación de `inscription-entry.ts`                                            |

---

## Reglas que el patrón da por sentadas

- El template **nunca** cambia el paso directamente: siempre vía una fachada.
- Solo los `endpoints/` importan de `shared/api/generated/**`; el resto usa tipos
  propios de la feature.
- Las fechas son `string | null` en los contratos; la conversión a `Date` es
  explícita en fachadas/UI.
- Stores y fachadas se proveen **en la página**, no en root.
- Los constructores de las fachadas de sección **no posicionan el flujo ni leen la
  ruta**: el estado inicial lo deriva `deriveInitialInscripcionState` (pura) y lo
  aplica solo `ProcessFacade.applyInitialState`.
- La **intención de entrada** (`nueva`/`retomar`/`reactivar`) sale de la URL, nunca
  se infiere del estado del backend. El backend manda sobre los datos; la intención,
  sobre presentación y navegación.
