# Accessibility

> Tipo: standards

El objetivo del portal es cumplir WCAG 2.2 nivel AA en los flujos visibles de
admision: login, registro, recuperacion, creacion de contrasenia, inicio, datos
personales y cambio de contrasenia.

## Checklist WCAG 2.2 AA

- Landmarks: cada ruta debe exponer un `main` identificable con
  `id="main-content"` para el skip link global.
- Navegacion: el flujo debe completarse con teclado, toque y lector de pantalla.
- Foco: todo control interactivo debe tener foco visible y orden logico.
- Dialogos y menus: usar `aria-expanded`, `aria-controls`, `aria-modal`, cierre
  con `Escape`, trap de foco y restauracion del foco al disparador.
- Formularios: cada campo debe tener label visible, hints claros si el valor
  visual no alcanza, errores por campo y resumen con `OrtErrorSummary`.
- Errores: no depender solo de color. Los errores deben anunciarse con
  `role="alert"` o el mecanismo accesible del componente ORT.
- Contrasenias: los toggles deben anunciar accion dinamica, por ejemplo
  `Mostrar contraseña` y `Ocultar contraseña`.
- Iconos e imagenes: marcar decorativos con `aria-hidden="true"` o `alt=""`.
  Las imagenes informativas deben tener texto alternativo util.
- Contraste: texto, iconos funcionales, bordes de foco y estados deben cumplir
  contraste AA.
- Responsive: el contenido debe poder completarse en pantallas pequenias sin
  solapamientos ni perdida de controles.

## Validaciones automaticas

Antes de abrir un PR ejecutar:

```bash
npm run lint:check
npm run test:ci
npm run build
npm run test:a11y
npm run test:e2e:smoke
```

`npm run test:a11y` corre Playwright con `@axe-core/playwright` en desktop y
mobile. Los tests mockean API para cubrir pantallas publicas y protegidas sin
depender del backend.

`npm run test:e2e:smoke` cubre regresion funcional rapida con mocks. Debe pasar
en PR junto con a11y para asegurar que los flujos criticos se siguen pudiendo
completar con interacciones reales.

## Como funciona Playwright + axe

Playwright es el runner de navegador. En este repo abre la app real en Chromium,
navega por rutas y ejecuta acciones de usuario programadas desde
`e2e/a11y.spec.ts`.

`@axe-core/playwright` inyecta axe-core en la pagina ya renderizada. Axe revisa
el DOM, estilos computados y atributos accesibles contra reglas WCAG. Sirve para
detectar problemas como:

- campos sin label accesible;
- botones o links sin nombre accesible;
- roles ARIA invalidos o incompletos;
- errores de contraste de color que axe puede calcular;
- landmarks/headings mal estructurados;
- contenido que queda oculto de forma incompatible con asistencia tecnica.

Importante: axe no reemplaza una prueba manual con teclado ni lector de
pantalla. Axe no recorre solo el flujo apretando Tab ni confirma que el orden de
foco sea el esperado. Para eso hay que escribir pasos Playwright explicitos, por
ejemplo `page.keyboard.press('Tab')`, `expect(locator).toBeFocused()` o
`page.getByRole('button', { name: '...' }).press('Enter')`.

En resumen:

- Playwright abre la app, navega, hace clicks, escribe, presiona teclas y
  verifica estados.
- Axe analiza accesibilidad automatizable en la pantalla renderizada.
- Las pruebas manuales siguen cubriendo lector de pantalla, criterio humano,
  orden de foco fino y experiencia real del flujo.

## Como correr los tests a11y

Primera vez en una maquina local:

```bash
npx playwright install chromium
```

Ejecucion normal, headless:

```bash
npm run test:a11y
```

Para depurar visualmente cualquier caso Playwright:

```bash
npm run test:e2e:ui
```

Para una corrida puntual de accesibilidad con navegador visible:

```bash
npx playwright test --config playwright.config.ts --grep @a11y --headed --workers=1
```

Ver el reporte HTML despues de una corrida:

```bash
npm run test:e2e:report
```

Si hace falta investigar un flujo con datos sensibles, usar `.env.e2e.local`
con variables locales o un comando `npx playwright test` puntual. No versionar
cedulas reales, emails personales ni passwords.

La configuracion esta en `playwright.config.ts`:

- levanta `ng serve` automaticamente en `http://127.0.0.1:4200`;
- reutiliza un servidor local si ya esta corriendo fuera de CI;
- corre dos proyectos: `chromium-desktop` y `chromium-mobile`;
- guarda trace cuando falla una prueba;
- genera reporte HTML para inspeccionar resultados;
- si `E2E_BASE_URL` esta definido, no levanta servidor local y prueba esa URL.

## Como leer `e2e/a11y.spec.ts`

El archivo tiene tres bloques principales:

- `publicPages`: rutas publicas que se abren sin sesion.
- `protectedPages`: rutas protegidas; antes de navegar se inserta una sesion
  falsa en `localStorage`.
- pruebas especificas de teclado/foco para errores de formulario y menu de
  perfil.

Los mocks comunes viven en `e2e/support/api-mocks.ts`: interceptan llamadas HTTP
a `/Auth`, `/Catalogos`, `/Persona` y `/Registro` para responder datos
controlados sin backend real.

Cada test hace una verificacion minima de que la pantalla correcta esta visible
y luego llama a `expectNoAxeViolations(page)`. Esa funcion ejecuta axe con tags
WCAG:

```ts
await new AxeBuilder({ page })
  .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
  .analyze();
```

Si axe encuentra un problema, el test falla mostrando:

- `id`: regla incumplida, por ejemplo `label` o `color-contrast`;
- `impact`: severidad aproximada;
- `help`: explicacion corta;
- `nodes`: selector del nodo afectado.

## Como agregar pruebas de teclado

Cuando un flujo depende de teclado o foco, agregar un test Playwright especifico
ademas del escaneo axe. Ejemplo:

```ts
test('closes the profile menu with Escape and restores focus', async ({ page }) => {
  await addAuthenticatedSession(page);
  await page.goto('/inicio');

  const menuButton = page.getByRole('button', { name: 'Abrir menú de usuario' });
  await menuButton.focus();
  await page.keyboard.press('Enter');

  await expect(page.getByRole('dialog', { name: 'Menú de usuario' })).toBeVisible();
  await page.keyboard.press('Escape');

  await expect(page.getByRole('dialog', { name: 'Menú de usuario' })).toBeHidden();
  await expect(menuButton).toBeFocused();
});
```

Usar siempre queries por rol/nombre accesible (`getByRole`) cuando sea posible.
Eso fuerza que el test mire la app como la percibe un lector de pantalla, no por
clases CSS internas.

## Relacion con E2E/regression

Accesibilidad y regresion funcional se complementan:

- `test:a11y` detecta problemas automatizables de WCAG y algunos flujos de
  teclado/foco.
- `test:e2e:smoke` valida que los flujos criticos sigan completandose rapido en
  PR.
- `test:e2e:regression` recorre casos completos y bordes conocidos antes de
  releases, hotfixes delicados o cambios en registro/login/datos personales.
- El workflow `e2e-nightly` corre contra preprod de forma semanal o manual para
  adelantar errores por API, datos semilla o integraciones.

La estrategia completa esta en [docs/E2E-GUARDRAILS.md](./E2E-GUARDRAILS.md).

## Criterios manuales

Las validaciones automaticas no reemplazan la revision manual. Para cambios de
UI revisar:

- Tab, Shift+Tab, Enter, Space y Escape en el flujo modificado.
- Lectura con lector de pantalla de labels, hints, errores y cambios de estado.
- Foco inicial y restauracion de foco en menus, dialogos y drawers.
- Zoom del navegador al 200% y viewport mobile.
- Contraste real en estados normal, hover, focus, disabled, error y success.
- Formularios incompletos: el resumen de errores debe recibir foco y anunciar
  los campos invalidos. Los links desde el resumen al campo solo se habilitan
  cuando el componente expone un target publico y estable.

## ORT Components

Regla: si `@desarrolloort/components` no expone soporte accesible necesario, no
se parchea en la app, no se toca `node_modules` y no se escriben overrides
contra DOM o clases internas de ORT.

En esos casos:

1. Usar el componente ORT existente si cubre el caso base.
2. Dejar un comentario `TODO(a11y-ort-component): ...` en el punto de uso.
3. Registrar el gap en esta seccion.
4. Elevarlo al equipo responsable de la libreria.

### Gaps de ORT Components

- `OrtInput`, `OrtSelect`, `OrtCedulaInput` y `OrtRadioGroup`: no exponen un
  target publico estable para que `OrtErrorSummary` pueda renderizar links que
  lleven el foco al campo invalido. En la app se usa
  `ORT_COMPONENT_ERROR_SUMMARY_LINKS_UNSUPPORTED`, que mantiene el resumen
  anunciado y enfocado pero omite anchors rotos. El gap queda marcado en
  `src/app/shared/forms/form-error-summary.ts` con
  `TODO(a11y-ort-component)`.
- `OrtInput` en el paso de identidad del registro: al alternar dinamicamente
  entre entrada de cedula y entrada de documento no-CI, el input no-CI no
  conserva de forma confiable el nombre accesible del `OrtFormField`. En
  `register-identity-step.html` se informa el label explicito con el input
  publico `_ariaLabel` de ORT y queda marcado con
  `TODO(a11y-ort-component)`.

### Reglas de uso app-level

- Prefijos visuales como `+598` son decorativos para ORT; el dato debe estar en
  el label o en un hint accesible.
- Si el componente ORT ya tiene modulo para el control, usarlo antes que crear
  controles custom. Ejemplo: `OrtRadioModule` para radio cards.
- Los estilos app-level pueden ordenar layout o spacing del punto de uso, pero
  no deben depender de clases internas del componente ORT.
