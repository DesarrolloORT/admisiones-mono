# Plan de implementación — catálogo de pantallas, textos y errores

## 1. Objetivo

Publicar en el mismo sitio IIS de Admisiones una documentación interna accesible en:

```text
https://<host-actual>/docs/
```

Para cada pantalla o estado visible, el portal debe mostrar:

- una captura reproducible;
- todos los textos visibles de ese estado;
- todas las validaciones y errores que pueden aparecer en esa pantalla, aunque no estén
  activos en la captura base;
- la condición que hace aparecer cada texto o error;
- su fuente de verdad en frontend o backend;
- un identificador estable con el que el analista funcional pueda proponer un cambio.

El portal documenta el producto; no se convierte en un CMS ni modifica textos en producción.

## 2. Punto de partida que se reutiliza

Ya existen piezas suficientes para no crear otra solución:

- Docusaurus en `admisiones/docs-site/`;
- documentación fuente en `admisiones/docs/`;
- inventario inicial en `admisiones/docs/inventarios/errores-visibles.csv`;
- textos frontend en `src/app/**/*-es.json`;
- mensajes backend en archivos `.resx` y códigos funcionales estables;
- Playwright, mocks y page objects en `admisiones/e2e/`;
- build bajo un `baseUrl` configurable, metadata de despliegue y `web.config` con
  autenticación Windows;
- validaciones `check:knowledge`, `test:knowledge` y `docs:build`.

La implementación extiende estas piezas. No se agrega base de datos, API de documentación,
editor WYSIWYG ni autenticación propia.

## 3. Decisiones de diseño

### 3.1 URL y publicación

- El sitio Angular conserva `/`.
- Docusaurus se compila con `DOCS_BASE_URL=/docs/`.
- IIS publica el build estático como aplicación hija `/docs`, con autenticación Windows.
- La regla de fallback de Angular debe excluir `/docs` para no responder con el
  `index.html` de la aplicación.
- La publicación usa staging, validación y reemplazo atómico. No requiere cambios en la API
  para servir el sitio.

### 3.2 Fuentes de verdad

| Contenido | Fuente de verdad | El portal hace |
| --- | --- | --- |
| Texto frontend | `*-es.json` | Resuelve la clave y muestra el valor vigente |
| Error backend | `.resx` + código funcional + status HTTP | Resuelve la clave y enlaza el uso |
| Condición y ubicación | inventario versionado | Explica cuándo y dónde se ve |
| Captura | escenario Playwright versionado | La genera con datos deterministas |
| Propuesta de cambio | Microsoft List | Solo enlaza; nunca cambia código directamente |

El texto resuelto se genera. No se mantiene una segunda copia editable en Markdown o CSV.

### 3.3 Unidad documental

La unidad es un **estado de pantalla**, no solo una ruta. Por ejemplo:

```text
AUTH-LOGIN/default
AUTH-LOGIN/validation-errors
AUTH-LOGIN/api-error
ENROLLMENT-DETAIL/loading-error
ENROLLMENT-SUCCESS/default
```

Una misma ruta puede tener varias capturas si cambia materialmente lo que la persona ve.
No se crea una captura por cada mensaje cuando cambia únicamente el texto del mismo
contenedor.

## 4. Modelo mínimo de datos

### 4.1 Pantallas

Crear `docs/inventarios/pantallas.csv`:

| Campo | Uso |
| --- | --- |
| `screen_id` | Identificador estable, por ejemplo `AUTH-LOGIN` |
| `state_id` | `default`, `validation-errors`, `api-error`, etc. |
| `flujo` | Login, Registro, Inicio, Inscripciones o Becas |
| `nombre` | Nombre funcional visible |
| `route` | Ruta Angular si existe |
| `source_path` | Page/componente dueño del estado |
| `fixture` | Escenario Playwright que lo reproduce |
| `screenshot` | Ruta de la imagen generada |
| `estado` | `documentada`, `pendiente` o `no-aplica` |

### 4.2 Textos y errores

Crear `docs/inventarios/textos-visibles.csv` y migrar allí
`errores-visibles.csv`:

| Campo | Uso |
| --- | --- |
| `id` | Identificador estable, conservando los IDs actuales de error |
| `screen_id` | Pantalla dueña |
| `state_id` | Estado donde aparece |
| `tipo` | `texto`, `ayuda`, `validacion`, `error`, `aviso`, `vacio`, `exito` |
| `campo` | Campo o región visual; vacío si es general |
| `condicion` | Regla funcional que lo hace visible |
| `origen` | `frontend` o `backend` |
| `source_ref` | JSON pointer o clave `.resx`; nunca una copia del texto |
| `codigo_backend` | Código público, si aplica |
| `http_status` | Status esperado, si aplica |
| `fuente_uso` | Componente/handler/endpoint que lo presenta |

El catálogo generado incluye además `texto`, commit frontend y commit backend.

## 5. Implementación por etapas

### E0 — Cerrar prerrequisitos operativos

1. Confirmar que la URL pública debe ser exactamente `/docs/` y no
   `/docs/admisiones/`.
2. Confirmar con administración de Microsoft 365 que el tenant y los analistas tienen una
   licencia que incluya SharePoint/Microsoft Lists. No asumir que Lists es gratuito fuera de
   una licencia existente.
3. Elegir el sitio SharePoint interno que será dueño de la lista y los grupos responsables
   de administrarla.

**Salida:** decisiones anotadas en este documento.

### E1 — Baseline de pantallas

1. Inventariar rutas y estados desde `app.routes.ts`, routes de feature y steps internos.
2. Dar `screen_id` a páginas y pasos visibles, empezando por:
   - autenticación: login, registro, recuperación, creación de contraseña, 2FA y
     confirmaciones;
   - inicio: dashboard, datos personales y cambio de contraseña;
   - inscripciones: carga fallida, datos académicos, datos personales, confirmación,
     reserva y resultado;
   - becas: listado y pasos de cada variante que cambie contenido.
3. Separar variantes solo cuando cambien textos, campos, reglas o errores. Las cuatro rutas
   de beca no generan cuatro páginas si comparten exactamente el mismo estado.
4. Registrar estados no alcanzables o retirados como `no-aplica`, con motivo.

**Verificación:** toda page/step visible queda asociada a al menos un `screen_id`; no se
documentan componentes internos como si fueran pantallas independientes.

### E2 — Capturas reproducibles en frontend

1. Reusar Playwright, sus API mocks y page objects.
2. Agregar un proyecto/script `docs:capture` que:
   - use viewport desktop fijo en la primera entrega;
   - congele fecha, datos y respuestas externas cuando afecten la UI;
   - oculte o reemplace datos personales;
   - navegue por `fixture` y escriba PNG en
     `docs/static/pantallas/<screen_id>/<state_id>.png`.
3. Capturar `default` y cada estado visual distinto: validación, error general, vacío,
   éxito o bloqueo. No duplicar imágenes cuando solo cambia el contenido del mismo mensaje.
4. Agregar un `--check` que falle por captura faltante, no por diferencia visual automática.
   La comparación pixel a pixel queda fuera del primer alcance porque vuelve frágil el CI.

**Verificación:** `npm run docs:capture -- --check` resuelve todos los fixtures y no contiene
datos reales.

### E3 — Catálogo generado desde frontend y backend

1. Implementar un único script `docs-site/scripts/build-ui-catalog.mjs` con:
   - `--write`: resuelve referencias y genera
     `docs-site/src/data/ui-catalog.json`;
   - `--check`: falla si el JSON está desactualizado o una referencia no existe.
2. Resolver textos frontend desde `*-es.json`.
3. Resolver mensajes backend desde los `.resx` de `api-admisiones`.
4. Mantener el CSV como mapa funcional, no como copia de textos.
5. Reportar:
   - claves inexistentes;
   - placeholders sin descripción en `condicion`;
   - errores públicos sin `screen_id` ni justificación;
   - filas sin captura/estado asociado;
   - textos visibles escritos directamente en HTML/TypeScript cuando deban estar en
     `*-es.json`.
6. Commitear el JSON generado: el build de Docusaurus no debe depender de clonar el backend.

**Backend necesario:** todo error visible debe tener código estable, status coherente y
texto localizado en `.resx`. Agregar un test liviano que impida nuevos errores públicos con
texto literal.

**Frontend necesario:** todo texto catalogado debe resolver a `*-es.json`; el handler debe
mostrar el mensaje backend o el fallback documentado, sin remapeos ocultos.

### E4 — Página por pantalla en Docusaurus

1. Crear una sección `Pantallas` en el sidebar, agrupada por flujo.
2. Crear un componente MDX reutilizable `ScreenCatalog` que reciba `screenId` y muestre:
   - selector de estado;
   - captura;
   - tabla de textos de ese estado;
   - tabla completa de validaciones y errores posibles en la pantalla;
   - código/status para errores backend;
   - enlaces a las fuentes vigentes;
   - versión de frontend y backend usada para generar la página.
3. Crear páginas MDX mínimas, una por `screen_id`; la tabla se alimenta del JSON generado.
4. Agregar búsqueda/filtros por texto, flujo, pantalla, tipo, origen y código.
5. Mantener una vista global descargable como CSV. XLSX solo se agrega si el canal elegido
   para AF realmente lo necesita.

**Verificación:** `npm run docs:build` sin links rotos y navegación directa a cualquier
pantalla bajo `/docs/pantallas/...`.

### E5 — Publicación IIS en `/docs`

1. Cambiar el valor de despliegue a `DOCS_BASE_URL=/docs/`.
2. Configurar `/docs` como aplicación hija o directorio virtual apuntando al build
   Docusaurus; preferir aplicación hija para aislar `web.config`.
3. Conservar autenticación Windows, `noindex` y headers de seguridad existentes.
4. Excluir `/docs` del rewrite/fallback SPA del frontend.
5. Publicar por staging y reemplazo atómico; conservar un backup recuperable.
6. Validar después del deploy:
   - `/docs/`;
   - una URL profunda de pantalla;
   - `/docs/deployment.json`;
   - un asset PNG;
   - acceso anónimo denegado;
   - aplicación Angular y API sin regresiones.

**No requiere:** endpoint nuevo, cambio de base de datos ni despliegue conjunto de la API.

### E6 — Circuito para propuestas del analista funcional

#### Decisión: Microsoft List interna

Los analistas no reciben acceso a GitHub. Cada fila de texto/error incluye
`Proponer cambio`, que abre el formulario nativo de una Microsoft List interna.

Crear la lista `Propuestas de textos de Admisiones` desde la plantilla de seguimiento de
problemas o como lista vacía, con estas columnas:

| Columna | Tipo | Regla |
| --- | --- | --- |
| `Título` | texto | Resumen corto obligatorio |
| `Screen ID` | texto | Obligatorio; valor visible en `/docs` |
| `Texto/Error ID` | texto | Obligatorio; valor visible en `/docs` |
| `URL documentación` | hipervínculo | Obligatoria |
| `Texto actual` | texto multilínea | Obligatorio |
| `Texto propuesto` | texto multilínea | Obligatorio |
| `Motivo / escenario` | texto multilínea | Obligatorio |
| `Prioridad` | elección | Baja, media, alta |
| `Estado` | elección | Nueva, en análisis, aceptada, rechazada, implementada |
| `Responsable` | persona | Lo completa el equipo de producto/desarrollo |
| `PR / versión` | hipervínculo o texto | Uso interno; AF no necesita acceso |
| `Adjuntos` | nativo | Captura opcional |

Permisos mínimos:

- AF: agregar propuestas y consultar/editar las propias;
- Producto: leer, comentar y cambiar estado;
- Desarrollo: leer y completar referencia de implementación;
- propietarios del sitio: administrar estructura y permisos.

La lista vive en un sitio SharePoint del equipo, no en `My lists`, para que no dependa de
una cuenta individual. Activar historial de versiones y una regla nativa que notifique al
grupo de triage cuando se cree una propuesta. No usar Power Automate en la primera versión.

Flujo:

1. AF abre el formulario desde `/docs`, copia los IDs visibles y crea la propuesta; no
   edita archivos ni fuentes productivas.
2. Producto valida el cambio y marca `en análisis`, `aceptada` o `rechazada`.
3. Desarrollo cambia la fuente correcta (`*-es.json` o `.resx`) y agrega/ajusta el test.
4. El PR regenera catálogo y, si corresponde, captura.
5. Desarrollo guarda el PR o versión en la lista sin compartir el repositorio con AF.
6. AF revisa el sitio publicado y la propuesta pasa a `implementada`.

El primer alcance usa el formulario nativo. El prellenado automático desde Docusaurus se
agrega solo si copiar los dos IDs resulta una fricción real; hacerlo desde el inicio exigiría
Power Apps, Microsoft Graph o código específico sin mejorar la trazabilidad.

#### Alternativa temporal

Permitir descarga CSV/XLSX y un script de importación que genere un reporte, pero nunca
aplicar automáticamente cambios al código. Sirve para una primera revisión masiva; no para
el mantenimiento diario.

### E7 — Guardarraíles y adopción

1. Agregar al CI:
   - `build-ui-catalog --check`;
   - `docs:capture --check`;
   - `check:knowledge` y su test;
   - build Docusaurus.
2. Cuando un PR cambie fuentes asociadas a una pantalla, exigir actualización del catálogo
   o `docs-none: <motivo>`.
3. Publicar primero Login y un step de Inscripciones como piloto.
4. Validar el formato y el formulario de Microsoft Lists con AF.
5. Completar el resto por flujo: Auth → Inicio → Inscripciones → Becas.

## 6. Definition of Done por pantalla

Una pantalla está **documentada** solo si cumple todo lo siguiente:

- [ ] Tiene `screen_id` estable, nombre funcional, flujo, ruta y fuente frontend.
- [ ] Cada estado materialmente distinto tiene `state_id`, fixture reproducible y captura
      vigente sin datos personales.
- [ ] La captura permite reconocer la pantalla y ubicar sus regiones principales.
- [ ] Están catalogados título, instrucciones, labels, placeholders, ayudas, botones,
      links, avisos, estados vacíos y mensajes de éxito visibles en ese estado.
- [ ] Están catalogadas todas las validaciones y errores que pueden aparecer en la pantalla,
      aunque no estén activos en la captura base.
- [ ] Cada entrada muestra texto exacto, condición, campo/región, origen y referencia a la
      fuente de verdad.
- [ ] Cada error backend muestra código público, status HTTP, clave `.resx`, endpoint y el
      fallback frontend cuando exista.
- [ ] Los placeholders y valores dinámicos están explicados con un ejemplo que no expone
      datos reales.
- [ ] No hay texto duplicado manualmente entre fuente, inventario y Markdown.
- [ ] El enlace `Proponer cambio` conserva los IDs y la URL de la pantalla.
- [ ] El escenario y las resoluciones de fuentes pasan los checks automáticos.
- [ ] AF revisó la página del piloto o del lote y dejó aprobación trazable.

## 7. Definition of Done global

- [ ] `/docs/` funciona en el IIS actual y las URLs profundas cargan directamente.
- [ ] El sitio exige autenticación Windows y no es indexable.
- [ ] El 100 % de los `screen_id` en alcance está `documentada` o tiene una exclusión con
      motivo y responsable.
- [ ] El 100 % de los errores públicos del backend está mapeado a una pantalla o justificado
      como no visible.
- [ ] El catálogo indica los commits exactos de frontend y backend.
- [ ] CI impide referencias rotas y catálogo desactualizado.
- [ ] Existe rollback probado del build estático.
- [ ] El canal de propuestas tiene responsable de triage y estados acordados.

## 8. Entregas sugeridas

| Entrega | Alcance | Criterio de salida |
| --- | --- | --- |
| P0 | Login + error de login + `/docs` local | AF valida formato e IDs |
| P1 | Auth completo + publicación IIS | URL, auth y deep links verificados |
| P2 | Inicio + Inscripciones | Catálogo front/back y capturas completos |
| P3 | Becas + vista global | Cobertura total y CI obligatorio |
| P4 | Microsoft List para AF | Primera propuesta recorre el flujo hasta producción |

## 9. Prerrequisito pendiente

Microsoft Lists queda elegido porque evita dar acceso al GitHub interno y aporta formulario,
permisos, adjuntos, vistas e historial con herramientas del tenant.

Antes de implementar, administración debe confirmar que las licencias Microsoft 365 actuales
incluyen SharePoint/Microsoft Lists para los usuarios participantes. Si ya está incluido, no
se espera un costo adicional específico por crear esta lista. Si no está incluido, volver a
la alternativa CSV/XLSX antes de comprar licencias o desarrollar una integración.

## 10. Referencias de Microsoft

- [Crear una lista desde Microsoft Lists](https://support.microsoft.com/en-us/sharepoint/lists/create-a-list-from-the-lists-app)
- [Plantillas de listas en Microsoft 365](https://support.microsoft.com/en-us/sharepoint/lists/data-and-lists/list-templates-in-microsoft-365)
- [Niveles de permisos de SharePoint](https://learn.microsoft.com/en-us/sharepoint/understanding-permission-levels)
- [Comparación vigente de planes empresariales de Microsoft 365](https://www.microsoft.com/content/dam/microsoft/final/en-us/microsoft-brand/documents/Modern-Work-Plan-Comparison-Enterprise-Jul26.pdf)
