# Documentación de Admisiones

El proyecto Docusaurus se publica en IIS desde este repositorio. ECO-ORT solo debe enlazar la URL publicada; no contiene ni despliega este sitio.

## Ejecutar localmente

```bash
npm ci --prefix docs-site
npm run build --prefix docs-site
npm run start --prefix docs-site
```

El build local usa `http://localhost/docs/admisiones/` y genera `docs-site/build/deployment.json` con valores `local` cuando GitHub no aporta metadata.

## Configuración de GitHub

Configurar estas variables de repositorio o del environment `desarrollo`:

| Variable              | Valor inicial               |
| --------------------- | --------------------------- |
| `DOCS_PUBLISH_BRANCH` | `v1.0.0/main`               |
| `DOCS_SYSTEM_SLUG`    | `admisiones`                |
| `DOCS_SERVER_ROOT`    | `D:\Sites\EcoDocs`          |
| `DOCS_SITE_ORIGIN`    | `https://<dominio-eco-ort>` |
| `DOCS_BASE_URL`       | `/docs/admisiones/`         |
| `DOCS_SOURCE_BRANCH`  | `v1.0.0/main`               |

La URL pública es `${DOCS_SITE_ORIGIN}${DOCS_BASE_URL}` y el directorio físico es `${DOCS_SERVER_ROOT}\${DOCS_SYSTEM_SLUG}`. La rama publicada actualmente es `v1.0.0/main`.

El workflow se puede ejecutar manualmente desde **Actions**. Siempre construye la rama seleccionada, pero solo publica si coincide con `DOCS_PUBLISH_BRANCH`.

## Verificar y revertir

Luego de publicar, verificar `${DOCS_SITE_ORIGIN}${DOCS_BASE_URL}deployment.json`; contiene sistema, repositorio, rama, commit y fecha. El workflow valida además `index.html`, `deployment.json` y `web.config` antes y después del cambio.

La publicación usa `admisiones.__staging` y conserva `admisiones.__backup` hasta completar el reemplazo. Para rollback manual, renombrar el directorio activo a una ubicación segura y restaurar `admisiones.__backup`, o descargar un artifact anterior y publicarlo con el mismo procedimiento de staging.

## Migración futura a main

1. Confirmar que `main` contiene la documentación vigente.
2. Cambiar `DOCS_PUBLISH_BRANCH` a `main`.
3. Cambiar `DOCS_SOURCE_BRANCH` a `main`.
4. Ejecutar manualmente el workflow desde `main`.
5. Verificar `deployment.json`.
6. No modificar el workflow.
