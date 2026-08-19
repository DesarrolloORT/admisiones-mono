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
  `createScholarshipApplicationForm()`. Editá el array para agregar, quitar o
  reordenar pasos.
- **`facades/scholarship-process.ts`** — el único objeto con scope de página.
  Es dueña del flow (`createProcessFlow`) y de **todos los `FormGroup`**: los
  steps se destruyen al navegar, así que si los forms fueran suyos, volver atrás
  perdería lo cargado. No hay capa `store/`; los forms salen de las factories de
  `models/`.
- **`facades/scholarship-personal.ts` y `facades/scholarship-proposal.ts`** — las
  fachadas de sección. Cada step **se las provee a sí mismo**, así que solo viven
  mientras ese paso está montado. Validan lo suyo y, si está OK, llaman a
  `process.continue()`. Por eso la fachada de proceso no las conoce: cuando ella
  se crea, todavía no existen.
- **`pages/scholarship-process/steps/scholarship-*-step`** — los steps, dentro de
  la page que los renderiza (igual que `enrollments/pages/steps/`). `components/`
  queda solo para presentación sin `inject()`. Las secciones que usa un único
  paso cuelgan de él, en `<step>/sections/`.

## Una page para las cuatro becas

`fbr`, `fexa`, `fcl` y `fbc` son la misma pantalla: cuatro rutas apuntando a
`ScholarshipProcess`, que recibe cuál es por `data.kind` y de ahí deriva la
`ScholarshipVariant` que decide qué secciones y validadores aplican. `fexa` es la
excepción: se parte en `fexaCon`/`fexaSin` según el modo de postulación que se
elige en el paso 1, así que su variante sale del formulario, no de la ruta.

Para agregar una beca: una entrada en `scholarships.routes.ts`, un valor en
`ScholarshipVariant` y sus casos en `getVisiblePersonalSections()` y
`SCHOLARSHIP_REQUIREMENTS_CONFIG`. No se crea una page nueva.

## Qué falta

Los endpoints de becas ya existen en el spec (ver `.api-spec/contracts/becas.contract.json`,
que es la autoridad de estas pantallas) y todavía no tienen consumidor. Cada uno se
agrega como un método de `api/scholarships.api.ts`:

| Endpoint                                | Para qué                                                                                                      |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `GET /scholarships/available`           | reemplaza el array estático de la page y el `isEnrolled` derivado: trae las cards y `requiresPriorEnrollment` |
| `GET /scholarships/application-options` | opciones del paso 1 (inscripciones, modalidades y pruebas) para un `scholarshipTypeId` de la card             |
| `POST /scholarships/applications`       | el alta de la postulación (`testId` + `enrollmentId`)                                                         |

- Resolvers de precarga, si la postulación necesita datos antes de activar la ruta.
