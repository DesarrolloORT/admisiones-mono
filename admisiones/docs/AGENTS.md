# Knowledge maintainer

Estas reglas aplican a la documentación canónica de Admisiones.

## Autoridad

- `docs/index.md` es el catálogo central.
- `docs/flujos/*.md` explica comportamiento observable de negocio.
- OpenAPI define rutas y shapes HTTP.
- Código y tests enlazados son evidencia de la implementación vigente.
- `docs-site/src/config/source-repositories.ts` fija las refs frontend/backend usadas por los enlaces actuales.

Una contradicción se marca como **Drift detectado**; no se inventa una conciliación.

## Consultar

1. Leer `docs/index.md`.
2. Abrir la página con el `businessId` correspondiente.
3. Usar CodeGraph para localizar la implementación frontend actual.
4. Seguir los enlaces backend/OpenAPI antes de afirmar reglas del servidor.

## Actualizar

1. Editar la página existente; no crear otra si el flujo actual puede contener el cambio.
2. Actualizar comportamiento, errores, efectos y evidencia en el mismo PR que el código.
3. No copiar DTOs completos ni métodos privados.
4. Mantener `businessId` estable y ajustar `sourcePaths` cuando cambie el ownership.
5. Si el cambio no altera comportamiento, usar `docs-none: <motivo>` en el PR.

## Validar

```bash
npm run check:knowledge
npm run test:knowledge
npm run docs:build
```

Git es el historial de mantenimiento; no crear un `log.md`.
