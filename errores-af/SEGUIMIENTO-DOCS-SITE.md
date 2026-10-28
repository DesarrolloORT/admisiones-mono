# Seguimiento — catálogo de pantallas, textos y errores

Fuente de alcance: [PLAN-DOCS-SITE.md](PLAN-DOCS-SITE.md).

Estados: `pendiente`, `en curso`, `bloqueado externo`, `completo`.

## Resumen

Baseline medido: **460 entradas** en el inventario legado, distribuidas entre Auth, Inicio,
Inscripciones, Becas, sesión y catálogos. La migración se hace por flujo para conservar IDs y
validar cada referencia; no se copia el texto resuelto al CSV nuevo.

| Entrega | Alcance | Estado | Salida verificable |
| --- | --- | --- | --- |
| P0 | Login y catálogo local | completo | `/docs/`, `AUTH-LOGIN`, catálogo generado y tests |
| P1 | Auth completo y capturas | completo | 16 estados Auth capturados y `docs:capture -- --check` |
| P2 | Inicio e Inscripciones | completo | Inicio completo (11 estados); Inscripciones completa (18 estados y 89 IDs backend) |
| P3 | Becas, vista global y CI | en curso | Vista global completa; primer lote de Becas capturado (7 estados y 47 entradas locales) |
| P4 | Publicación IIS y Microsoft List | bloqueado externo | Deploy validado y primera propuesta trazable |

## Trabajo ejecutable en repositorio

### 1. Inventario y catálogo

- [x] Modelo `pantallas.csv` y `textos-visibles.csv`.
- [x] Piloto `AUTH-LOGIN` con estados `default`, `validation-errors` y `api-error`.
- [x] Inventariar todas las páginas y estados visibles de Auth.
- [x] Inventariar Inicio.
- [x] Inventariar pasos y estados de Inscripciones.
- [ ] Inventariar Becas, agrupando variantes visualmente equivalentes.
  - Avance: catálogo (`requires-enrollment`, `available`, `empty`) y onboarding (`fbr`, `fexa`,
    `fbc`, `fcl`) inventariados en su texto local; restan los textos dinámicos del catálogo y los
    pasos académico, personal, confirmación y éxito.
- [ ] Migrar todos los IDs vigentes de `errores-visibles.csv` sin duplicar el texto.
- [ ] Marcar estados retirados o inalcanzables como `no-aplica`, con motivo.

### 2. Resolución y guardarraíles

- [x] Resolver referencias frontend `*-es.json`.
- [x] Resolver claves backend `.resx`.
- [x] Detectar IDs duplicados, estados inexistentes y placeholders sin explicación.
- [x] Impedir mensajes literales nuevos en errores públicos de Auth.
- [x] Detectar filas sin captura o exclusión justificada.
- [ ] Detectar errores públicos backend sin pantalla o justificación.
- [ ] Detectar textos visibles literales que deberían vivir en `*-es.json`.

### 3. Capturas reproducibles

- [x] Definir fixtures deterministas y sin datos personales.
- [x] Implementar `docs:capture` y `docs:capture -- --check`.
- [x] Capturar Auth (16 estados desktop).
- [x] Capturar Inicio (11 estados desktop).
- [x] Capturar Inscripciones (18 estados desktop).
- [ ] Capturar Becas.
  - Avance: 7 capturas reproducibles del catálogo y onboarding; restan los pasos del proceso.

### 4. Portal

- [x] `/docs/` muestra únicamente el catálogo funcional.
- [x] Componente por `screen_id`, selector de estado, fuentes y versiones.
- [x] Crear una página MDX por pantalla de Auth y agrupar el sidebar por flujo.
- [x] Agregar búsqueda/filtros por texto, flujo, pantalla, tipo, origen y código.
- [x] Agregar descarga CSV global.
- [ ] Agregar enlace `Proponer cambio` cuando exista la URL de Microsoft List.

### 5. Automatización

- [x] `docs:catalog`, `docs:catalog:check` y test del parser.
- [x] Build Docusaurus bajo `/docs/`.
- [ ] Integrar catálogo, capturas, knowledge checks y build en CI.
- [ ] Excluir `/docs` del fallback Angular y dejar artefacto IIS listo.
- [ ] Documentar staging, reemplazo atómico y rollback.

## Bloqueos externos

- [ ] **URL/IIS:** confirmar aplicación hija `/docs`, autenticación Windows y responsable del deploy.
- [ ] **Microsoft 365:** confirmar licencia SharePoint/Lists de analistas y equipo.
- [ ] **Microsoft List:** definir sitio propietario, grupos, permisos y URL del formulario.
- [ ] **AF:** validar el formato del piloto y aprobar cada lote.
- [ ] **Operación:** ejecutar smoke tests y rollback en el IIS real.

## Comandos de control

```powershell
npm run docs:catalog:check
npm run docs:catalog:test
npm run check:knowledge
npm run test:knowledge
npm run docs:build
npm run docs:capture:check
```

`docs:capture` regenera las capturas documentadas; `docs:capture:check` valida que cada
fixture y PNG declarado exista sin hacer comparación pixel a pixel.
