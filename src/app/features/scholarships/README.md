# Becas — cómo integrar un endpoint de punta a punta

Esta feature es el **ejemplo mínimo copiable** de la forma actual: tres conceptos
en el camino de datos (`api/` → `models/` → page o facade). Si tenés que exponer
algo del backend en pantalla, copiá el recorrido de este README.

Para el patrón de proceso paso a paso (stepper, `continue()` / `back()`), ver
[`docs/arquitectura/flujo-pasos.md`](../../../../docs/arquitectura/flujo-pasos.md).

---

## El recorrido completo: `GET /scholarships/enrollments`

Este endpoint decide si la persona ya tiene una inscripción confirmada, y con eso
la pantalla de becas muestra el catálogo completo o solo la beca sin inscripción
previa. Son **3 archivos nuevos y 1 edición**.

### 1. El tipo de la feature — `models/confirmed-enrollment.interface.ts`

El DTO generado tiene los 16 campos opcionales y nullables. El tipo de la feature
no: se colapsa la nullability en el adapter y de acá para adentro los campos
existen. Las fechas quedan como `string | null` (la conversión a `Date` es
explícita en la UI).

```ts
export interface ConfirmedEnrollment {
  enrollmentId: number;
  enrollmentDate: string | null;
  status: string;
  degreeProgramName: string;
  // …
}
```

> **Nombre del archivo:** los archivos **solo de tipos** terminan en
> `*.interface.ts`. Ese sufijo está exento del spec obligatorio en
> `test-generator.config.json`; con cualquier otro nombre,
> `npm run check-missing-tests` te va a pedir un `.spec.ts`.

### 2. El adapter — `api/scholarships.api.ts`

Es la **única** capa que puede importar `shared/api/generated/**` y
`ApiHttpClient` (lo enforza `eslint.config.js` y `check-api-contracts`). Llama al
endpoint generado y mapea. `api.list` normaliza `null`, objeto suelto o array a
un array y aplica el mapper por item: no escribas ese chequeo a mano.

```ts
@Injectable({ providedIn: 'root' })
export class ScholarshipsApi {
  private readonly api = inject(ApiHttpClient);

  public getConfirmedEnrollments(): Observable<ConfirmedEnrollment[]> {
    return this.api.list(getScholarshipsEnrollmentsEndpoint, item => ({
      enrollmentId: item.enrollmentId ?? 0,
      enrollmentDate: item.enrollmentDate ?? null,
      status: item.enrollmentStatus ?? '',
      degreeProgramName: item.productFullName ?? '',
      // …
    }));
  }
}
```

Recetas de `ApiHttpClient`: `api.data(endpoint, options?)` para un item,
`api.list(endpoint, mapper?, options?)` para arrays,
`api.requestWithMessage(...)` solo si necesitás el `message` del envelope. Si el
payload de la feature ya coincide en forma con el request generado, pasalo tal
cual (`body: payload`) en lugar de reescribir campo por campo.

### 3. El spec del adapter — `api/scholarships.api.spec.ts`

Cubre el mapeo, los defaults de nullability y la lista vacía. Se stubea solo
`request` sobre el prototipo real de `ApiHttpClient`, así `list()` corre su
implementación real y el spec sigue cubriendo la normalización:

```ts
const apiMock = Object.assign(Object.create(ApiHttpClient.prototype), {
  request: requestMock,
}) as ApiHttpClient;
```

### 4. El consumidor — `pages/scholarships/scholarships.ts`

La page inyecta **el adapter de su feature**. No hay service de reenvío ni store:
si el método solo iba a hacer `return this.api.mismoMetodo(...)`, no existe.

```ts
private readonly scholarshipsApi = inject(ScholarshipsApi);

private readonly confirmedEnrollments = toSignal(
  this.scholarshipsApi.getConfirmedEnrollments().pipe(catchError(() => of([]))),
  { initialValue: [] }
);

protected readonly isEnrolled = computed(() => this.confirmedEnrollments().length > 0);
```

Agregá una **facade** cuando haya orquestación real (varias llamadas, estado
compartido entre pasos), no por trámite. Agregá un **service** solo si tiene
comportamiento propio (combina llamadas, guarda estado, prepara archivos).

---

## Estado del flujo paso a paso

- **`models/scholarship-process.ts`** — `SCHOLARSHIP_STEPS`
  (`application-info` → `personal-info` → `confirmation`) y
  `createScholarshipAcademicForm()`. Editá el array para agregar, quitar o
  reordenar pasos.
- **`facades/scholarship-process.ts`** — dueña del estado del proceso
  (`createProcessFlow`) y de la navegación. Se provee en `pages/fbr`.
  `continue()` pregunta `canContinue()` a la fachada de la sección activa y
  **es el proceso el que avanza** el flow.
- **`facades/scholarship-proposal.ts`** — fachada del paso 1: dueña del
  `FormGroup` y de `canContinue()`. Las secciones nunca llaman a `flow.next()`,
  y el template nunca avanza el flow: siempre `process.continue()`.
- **`pages/fbr/steps/scholarship-*-step`** — los steps. Viven dentro de la page
  que los renderiza (igual que `enrollments/pages/steps/`), porque inyectan la
  fachada del proceso: `components/` queda solo para presentación sin `inject()`.
  Las secciones que usa un único paso cuelgan de él, en `<step>/sections/`.
  El paso 3 es el punto de extensión: montarlo en el `@switch` de
  `pages/fbr/fbr.html`.

## Qué falta

- Endpoints de postulación (crear, guardar borrador, confirmar): se agregan como
  métodos de `api/scholarships.api.ts`.
- Fachadas de las secciones 2 y 3, con su `canContinue()`.
- Resolvers de precarga, si la postulación necesita datos antes de activar la ruta.
