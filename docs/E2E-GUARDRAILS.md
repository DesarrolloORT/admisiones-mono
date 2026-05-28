# E2E Guardrails

> Tipo: standards

Este proyecto usa Playwright para regresion funcional, accesibilidad automatizada
y ejecuciones visuales locales. La regla principal es separar lo deterministico
de lo que depende de datos privados o ambientes reales.

## Capas de pruebas

- `@smoke`: suite rapida y bloqueante para PR. Usa mocks de API y valida que los
  flujos criticos sigan vivos.
- `@a11y`: Playwright + axe y checks automatizables de teclado/foco. No reemplaza
  revision manual con lector de pantalla.
- `@regression`: casos funcionales completos que protegen flujos ya
  implementados. Se corre manualmente antes de releases, hotfixes delicados o
  cambios en registro/login/datos personales.
- `@nightly`: corre contra preprod con datos semilla controlados desde el
  workflow semanal o manual.
- `@real`: tests contra un ambiente real. Requieren configuracion explicita y no
  forman parte de los scripts publicos de `package.json`.

## Comandos

```bash
npm run test:e2e:smoke
npm run test:e2e:regression
npm run test:e2e:ui
npm run test:e2e:report
```

Para ver una ejecucion en vivo, usar `test:e2e:ui`. Permite elegir un test,
correrlo paso a paso, inspeccionar locators y abrir el trace. Para necesidades
puntuales, usar `npx playwright test` con los flags de Playwright.

## Datos de prueba

- Los PR usan mocks en `e2e/support/api-mocks.ts`; no dependen del backend.
- Los escenarios versionados viven en `e2e/support/test-data/`.
- Los datos sensibles o imposibles de hardcodear se cargan en `.env.e2e.local`.
- `.env.e2e.local` esta ignorado. Usar `.env.e2e.example` como plantilla.
- No escribir cedulas reales, emails personales, passwords ni tokens en specs,
  fixtures o traces versionados.

## Nightly

El workflow [`.github/workflows/e2e-nightly.yml`](../.github/workflows/e2e-nightly.yml)
corre los lunes a las 04:00 de Montevideo, y tambien puede dispararse
manualmente contra preprod si existe `vars.E2E_BASE_URL`.

Configuracion requerida en el ambiente `preprod`:

- variable `E2E_BASE_URL`: URL del frontend desplegado en preprod.
- secret `E2E_NIGHTLY_DOCUMENT_NUMBER`: documento de una cuenta semilla.
- secret `E2E_NIGHTLY_PASSWORD`: password de esa cuenta semilla.

Nightly no usa produccion. Si en el futuro se quieren datos parecidos a
produccion, deben ser anonimizados y aprobados antes de agregarlos a esta suite.

## Acceptance-first para features nuevas

Para becas, inscripciones u otros flujos grandes, el guardrail es
acceptance-first:

1. A partir de Figma e historia funcional, escribir los escenarios observables
   en Playwright.
2. Mockear APIs y datos necesarios para que el caso sea deterministico.
3. Implementar la feature hasta que pasen smoke, a11y y regression aplicables.
4. Agregar un caso `@nightly` solo si existe dato semilla estable en preprod.

Los tests no validan pixel-perfect contra Figma. Validan comportamiento,
accesibilidad, errores, estados y navegacion real del usuario.

## Webwright

Webwright puede usarse como complemento exploratorio para descubrir caminos,
generar borradores de tests o investigar flujos. No es gate de CI y sus scripts
deben revisarse antes de entrar a `e2e/`.
