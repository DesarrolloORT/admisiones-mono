# Becas — cómo integrar un endpoint de punta a punta

Esta feature es el **ejemplo mínimo copiable** de la forma actual: tres conceptos
en el camino de datos (`api/` → `models/` → page o facade). Si tenés que exponer
algo del backend en pantalla, copiá el recorrido de este README.

Para el patrón de proceso paso a paso (stepper, `continue()` / `back()`), ver
[`docs/arquitectura/flujo-pasos.md`](../../../../docs/arquitectura/flujo-pasos.md).
Para qué hace la pantalla y por qué, ver la página canónica
[`docs/flujos/becas.md`](../../../../docs/flujos/becas.md).

---

## 0. Los endpoints no se escriben a mano

No hay URLs sueltas en el código. El camino es:

```
backend  →  .api-spec/swagger.json  →  npm run update-api  →  src/app/shared/api/generated/
                                                               ├── endpoints/*.endpoints.ts
                                                               └── models/*.ts
```

- `npm run api-spec:refresh` baja el swagger y los contratos del backend a `.api-spec/`.
- `npm run update-api` regenera `endpoints/` y `models/` desde ese snapshot y valida
  que compile.
- `npm run check-api-contracts` falla si un adapter filtra un tipo generado o si un
  endpoint quedó con `response: unknown`.

**Si el endpoint que necesitás no aparece en `generated/endpoints/`, el paso es
refrescar el spec — nunca escribir la URL a mano.** Si tampoco está en el swagger,
falta del lado del backend.

Además del swagger están los **contratos de negocio**: `.api-spec/contracts/becas.contract.json`
es la autoridad de estas pantallas. Explica lo que el OpenAPI no dice (qué significa
cada campo, de qué vista de base sale, qué limitaciones tiene). Léelo antes de tocar
lógica de becas.

---

## El recorrido completo: `GET /scholarships/available`

Este endpoint da las becas que se ven en `/becas` y decide si la persona puede
postularse. Son **2 archivos nuevos y 3 ediciones**.

### 1. El tipo de la feature — `models/available-scholarship.interface.ts`

Los DTO generados tienen **todos** los campos opcionales porque el swagger los
declara así. El tipo de la feature no: la nullability se colapsa una sola vez, en
el adapter, y de ahí para adentro los campos existen. Las fechas quedan como
`string | null` (la conversión a `Date` es explícita en la UI).

```ts
export interface AvailableScholarship {
  scholarshipTypeIds: number[];
  name: string;
  description: string;
  requiresTest: boolean;
}

export interface AvailableScholarships {
  requiresPriorEnrollment: boolean;
  scholarships: AvailableScholarship[];
}
```

> **Nombre del archivo:** los archivos **solo de tipos** terminan en
> `*.interface.ts`. Ese sufijo está exento del spec obligatorio en
> `test-generator.config.json`; con cualquier otro nombre,
> `npm run check-missing-tests` te va a pedir un `.spec.ts`.

### 2. El adapter — `api/scholarships.api.ts`

Es la **única** capa que puede importar `shared/api/generated/**` y
`ApiHttpClient` (lo enforza `eslint.config.js` y `check-api-contracts`). Llama al
endpoint generado y mapea a los tipos de arriba.

```ts
public getAvailableScholarships(): Observable<AvailableScholarships> {
  return this.api.data(getScholarshipsAvailableEndpoint).pipe(
    map(response => ({
      requiresPriorEnrollment: response?.requiresPriorEnrollment ?? true,
      scholarships: (response?.scholarships ?? []).map(item => ({
        scholarshipTypeIds: item.scholarshipTypeIds ?? [],
        name: item.name ?? '',
        // …
      })),
    }))
  );
}
```

**Qué método de `ApiHttpClient` usar:**

| Método                   | Cuándo                                             | Ejemplo en esta feature      |
| ------------------------ | -------------------------------------------------- | ---------------------------- |
| `api.data(...)`          | la respuesta es **un objeto**                      | `getAvailableScholarships()` |
| `api.list(...)`          | la respuesta es **un array**                       | `getConfirmedEnrollments()`  |
| `api.request(...)`       | POST / PUT / PATCH / DELETE, con `body`            | `createApplication()`        |
| `api.requestWithMessage` | solo si además necesitás el `message` del envelope | —                            |

`api.list` normaliza `null`, objeto suelto o array a un array y aplica el mapper
por item: **no escribas ese chequeo a mano**. Los tres desenvuelven el
`OperationResult`, así que el mapper recibe el DTO de negocio, no el sobre.

El default ante ausencia de dato se elige, no se improvisa: acá
`requiresPriorEnrollment` cae en `true` porque es el caso **restrictivo** — un
backend caído nunca debe habilitar una postulación.

### 3. Cómo se llama a un POST — `createApplication()`

Mismo adapter, `api.request` con `body`. Si el payload de la feature ya coincide
en forma con el request generado, pasalo tal cual (`body: payload`) en lugar de
reescribir campo por campo.

```ts
public createApplication(
  payload: ScholarshipApplicationPayload
): Observable<ScholarshipApplication> {
  return this.api
    .request(postScholarshipsApplicationsEndpoint, {
      body: { enrollmentId: payload.enrollmentId, testId: payload.testId },
      showLoader: true, // prende el loader global mientras viaja
    })
    .pipe(map(response => ({ applicationId: response?.applicationId ?? 0, /* … */ })));
}
```

Este método **todavía no tiene consumidor**: queda listo para que lo dispare
`facades/scholarship-proposal.ts` al confirmar la postulación. Un POST se dispara
desde una facade (no desde el template) y el patrón es siempre el mismo — el
ejemplo vivo es `enrollments/facades/enrollment-proposal.ts::continue()`:

```ts
this.scholarships
  .createApplication(payload)
  .pipe(
    finalize(() => this.submitting.set(false)), // pase lo que pase, se apaga
    takeUntilDestroyed(this.destroyRef) // si el paso se desmonta, se corta
  )
  .subscribe({
    next: () => this.process.flow.next(),
    error: () => this.error.set('No se pudo registrar la postulación.'),
  });
```

### 4. El spec del adapter — `api/scholarships.api.spec.ts`

Cubre el mapeo, los defaults de nullability, la lista vacía y el body exacto del
POST. Se stubea solo `request` sobre el prototipo real de `ApiHttpClient`, así
`list()` y `data()` corren su implementación real y el spec sigue cubriendo la
normalización:

```ts
const apiMock = Object.assign(Object.create(ApiHttpClient.prototype), {
  request: requestMock,
}) as ApiHttpClient;
```

### 5. Lo que la API no sabe — `models/scholarship-catalog.ts`

`/available` devuelve el **texto** de cada beca, pero no a qué ruta lleva su card
ni si exige inscripción previa: el backend no conoce las rutas de Angular. Eso es
conocimiento del front y vive en un solo archivo.

```ts
export function resolveScholarshipCatalogEntry(
  scholarshipTypeIds: readonly number[],
  name: string
): ScholarshipCatalogEntry | null;
```

Ojo con la regla de dominio: **una card no es un fondo**, es un `NOMBRE_PARA_FRONT`
que puede agrupar varios `ID_TIPO_BECA` (Excelencia Académica son dos fondos, 33 y
57, en una sola card). Por eso el parámetro es un array.

Si no reconoce la beca devuelve `null` y la card se muestra **sin acción**:
esconder un fondo vigente es peor que ofrecerlo sin botón.

> **TODO abierto:** solo están confirmados los ids de Excelencia Académica. Los de
> reválidas, concursables y capacitación laboral hay que pedírselos a backend;
> mientras tanto hay un fallback por palabra clave del `name` marcado para borrar.

### 6. Cómo se mapea a pantalla — `pages/scholarships/scholarships.ts`

La page inyecta **el adapter de su feature**. No hay service de reenvío ni store:
si el método solo iba a hacer `return this.api.mismoMetodo(...)`, no existe.

```ts
private readonly scholarshipsApi = inject(ScholarshipsApi);

// Observable → signal, con fallback explícito si el backend falla.
private readonly available = toSignal(
  this.scholarshipsApi.getAvailableScholarships().pipe(catchError(() => of(EMPTY_CATALOG))),
  { initialValue: EMPTY_CATALOG }
);

protected readonly isEnrolled = computed(() => !this.available().requiresPriorEnrollment);

// El único lugar donde se une el texto de la API con la navegación del front.
protected readonly cards = computed<ScholarshipCardModel[]>(() =>
  this.available().scholarships.map(s => {
    const entry = resolveScholarshipCatalogEntry(s.scholarshipTypeIds, s.name);
    return { title: s.name, /* … */ route: entry?.route ?? null };
  })
);
```

Y el template consume signals con control flow, sin `*ngIf` ni `*ngFor`:

```html
@if (!hasScholarships()) {
<p>No hay becas disponibles en este momento.</p>
} @else { @for (scholarship of cards(); track scholarship.title) {
<app-scholarship-card [isEnrolled]="isEnrolled()" [scholarship]="scholarship" />
} }
```

Fijate el `track scholarship.title` en vez de `track $index`: con `$index`, si la
lista se reordena Angular reusa el DOM equivocado.

La card (`components/scholarship-card/`) es **presentacional pura**: solo `input()`,
sin `inject()`. No sabe que existe una API.

Agregá una **facade** cuando haya orquestación real (varias llamadas, estado
compartido entre pasos), no por trámite. Agregá un **service** solo si tiene
comportamiento propio (combina llamadas, guarda estado, prepara archivos).

### 7. El mock e2e — `e2e/support/api-mocks.ts`

Los tests de Playwright no pegan al backend. Cada endpoint nuevo necesita su
handler ahí; el de `/scholarships/available` devuelve las dos ramas según
`options.scholarshipEnrollments`. Buena prueba de que el mapeo funciona: cambiá un
`description` en el mock y la pantalla lo refleja sin tocar código de la page.

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
`ScholarshipVariant`, su entrada en `models/scholarship-catalog.ts` y sus casos en
`getVisiblePersonalSections()` y `SCHOLARSHIP_REQUIREMENTS_CONFIG`. No se crea una
page nueva.

## Qué falta

| Endpoint                                | Estado                                                                          |
| --------------------------------------- | ------------------------------------------------------------------------------- |
| `GET /scholarships/available`           | ✅ consumido por la page de becas                                               |
| `GET /scholarships/enrollments`         | ✅ en el adapter (hoy sin consumidor: `/available` ya responde la habilitación) |
| `POST /scholarships/applications`       | ✅ en el adapter — falta dispararlo desde `facades/scholarship-proposal.ts`     |
| `GET /scholarships/application-options` | ⬜ **el próximo**: opciones del paso 1 (inscripciones, modalidades y pruebas)   |

Para `application-options`, leé el `frontHint` de ese endpoint en
`.api-spec/contracts/becas.contract.json` antes de escribir código: dice
exactamente cómo se filtran las pruebas y de dónde sale el `testId` que necesita
el POST. Recordá: **el front no calcula el `testId`**, lo elige de `tests[]`.

Pendiente aparte: los `ID_TIPO_BECA` de reválidas, concursables y capacitación
laboral, para completar `models/scholarship-catalog.ts` y borrar el fallback por
nombre.
