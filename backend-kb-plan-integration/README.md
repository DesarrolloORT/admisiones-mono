# Backend KB bridge plan (`api-admisiones`)

## Resultado buscado

Integrar `DesarrolloORT/api-admisiones` como fuente backend del portal documental
de Admisiones sin copiar logica entre repositorios.

El recorrido visible queda en el portal del frontend:

```text
click / evento UI
  -> component / facade
  -> frontend service / endpoint adapter
  -> OpenAPI
  -> backend controller / service
  -> Oracle, Redis, LDAP, email o API interna
```

El portal explica el recorrido completo. El backend mantiene el detalle de sus
reglas, persistencia, errores y efectos. Los enlaces sirven como evidencia; no se
copian DTOs ni implementaciones completas.

## Contexto del portal frontend

Este plan fue preparado en el repositorio frontend local
`C:\GIT\admisiones`. Ese repositorio ya usa **Docusaurus** como interfaz humana:

- `C:\GIT\admisiones\docs\index.md`: mapa central de autoridades y flujos;
- `C:\GIT\admisiones\docs\flujos\`: paginas canonicas de login, registro e
  inscripciones;
- `C:\GIT\admisiones\docs-site\`: configuracion, navegacion y componentes de
  Docusaurus;
- `C:\GIT\admisiones\docs-site\src\config\source-repositories.ts`: refs
  frontend/backend usadas por los enlaces;
- `C:\GIT\admisiones\scripts\docs\check-knowledge.js`: gate actual contra
  drift documental.

El backend **no necesita instalar Docusaurus ni publicar otro sitio**. Debe aportar
Markdown, el manifest y enlaces de evidencia; el portal frontend sigue siendo la
interfaz central. Las rutas `C:\GIT\admisiones` son contexto local para trabajar
con ambos repositorios abiertos y no deben hardcodearse en CI, JSON ni codigo.

Al implementar este plan, leer primero en el frontend:

1. `docs/index.md`;
2. `docs/AGENTS.md`;
3. la pagina de `docs/flujos/` con el mismo `flowId`;
4. `docs/DOCUMENTATION-GUIDELINES.md`.

## Estado verificado en `api-admisiones@develop`

- `develop` contiene los endpoints consumidos por el frontend, incluidos
  `Inscripciones/EncuestaInicial`, `ConfirmarPreInscripcion`, `Reactivar`, `Pagar`,
  `ReglamentoEstudiantil` y `Detalle`.
- Los controllers ya tienen XML docs y `ProducesResponseType`; son una buena base
  para OpenAPI, pero no explican por si solos transacciones, side effects ni reglas
  entre varios servicios.
- La solucion ya esta organizada por modulo en `AppLogic/Autenticacion/`,
  `Registro/`, `Inscripciones/` y sus tests.
- `AGENTS.md` es la instruccion backend mas completa y actual.
- `.github/copilot-instructions.md` todavia describe Ficha de Persona/.NET 9. Debe
  dejar de competir con `AGENTS.md`.
- `AGENTS.md` enlaza `docs/AUDITORIA-CALIDAD-CODIGO.md`, pero ese archivo no existe
  en `develop`; corregir el enlace o restaurar la evidencia.
- La CI actual compila, ejecuta xUnit y SonarQube, pero no valida conocimiento ni
  impacto documental.

## Modelo de autoridad

| Pregunta                       | Fuente canonica                                    |
| ------------------------------ | -------------------------------------------------- |
| Que ve y hace la persona       | Pagina de flujo en el portal frontend              |
| Que se envia por HTTP          | OpenAPI generado por `api-admisiones`              |
| Que reglas ejecuta el servidor | Pagina backend del flujo + service/tests enlazados |
| Que codigo corre hoy           | Ref backend configurada (`develop` inicialmente)   |
| Por que se tomo una decision   | Git/PR y, solo para decisiones transversales, ADR  |

Ante una contradiccion no se elige silenciosamente una version: la pagina del
portal muestra un bloque `Drift detectado` con los dos artefactos hasta alinearlos.

## Contrato minimo entre repositorios

Usar los mismos IDs estables en frontend y backend:

| `flowId`                   | Portal frontend         | Fuente backend                |
| -------------------------- | ----------------------- | ----------------------------- |
| `admisiones.login`         | `/flujos/login`         | `docs/flows/login.md`         |
| `admisiones.registro`      | `/flujos/registro`      | `docs/flows/registro.md`      |
| `admisiones.inscripciones` | `/flujos/inscripciones` | `docs/flows/inscripciones.md` |

Agregar en el backend `docs/knowledge-bridge.json`:

```json
{
  "schemaVersion": 1,
  "repository": "DesarrolloORT/api-admisiones",
  "defaultRef": "develop",
  "flows": [
    {
      "id": "admisiones.login",
      "document": "docs/flows/login.md",
      "sourcePaths": [
        "WebApiAdmisiones/WebApiAdmisiones/Controllers/AuthController.cs",
        "WebApiAdmisiones/AppLogic/Autenticacion/"
      ]
    },
    {
      "id": "admisiones.registro",
      "document": "docs/flows/registro.md",
      "sourcePaths": [
        "WebApiAdmisiones/WebApiAdmisiones/Controllers/RegistroController.cs",
        "WebApiAdmisiones/AppLogic/Registro/"
      ]
    },
    {
      "id": "admisiones.inscripciones",
      "document": "docs/flows/inscripciones.md",
      "sourcePaths": [
        "WebApiAdmisiones/WebApiAdmisiones/Controllers/InscripcionesController.cs",
        "WebApiAdmisiones/WebApiAdmisiones/Controllers/PersonaController.cs",
        "WebApiAdmisiones/AppLogic/Inscripciones/"
      ]
    }
  ]
}
```

Este manifest solo resuelve ownership y navegacion. No contiene reglas de negocio
ni duplica OpenAPI.

## Estructura backend propuesta

```text
docs/
├── index.md
├── AGENTS.md
├── knowledge-bridge.json
└── flows/
    ├── login.md
    ├── registro.md
    └── inscripciones.md
```

Cada pagina de flujo usa la misma estructura:

1. Objetivo y entradas.
2. Tabla `accion -> controller -> service -> efecto -> resultado`.
3. Diagrama Mermaid del recorrido backend.
4. Reglas y estados que OpenAPI no puede expresar.
5. Persistencia e integraciones externas.
6. Errores funcionales estables (`ErrorCode` + HTTP status).
7. Seguridad, PII, autorizacion, rate limits y expiraciones.
8. Evidencia exacta: controller, service, DTO propio, repositorio/API client y tests.
9. Enlace de regreso a la pagina del portal frontend con el mismo `flowId`.

No documentar todos los metodos privados. Documentar decisiones observables,
reglas de negocio, side effects y puntos donde una falla cambia el resultado.

## Implementacion recomendada en la rama desde `develop`

### 1. Corregir el cerebro del agente

- Mantener `AGENTS.md` como baseline backend.
- Convertir `.github/copilot-instructions.md` y `CLAUDE.md` en punteros breves a
  `AGENTS.md`; eliminar paths, versiones y mapas estaticos obsoletos.
- Agregar a `AGENTS.md`: antes de cambiar un controller, service, DTO publico,
  repositorio con regla funcional o API client, leer el `flowId` correspondiente y
  actualizar su pagina o declarar `docs-none: <motivo>` en el PR.
- Agregar `docs/AGENTS.md` con operaciones de consulta, actualizacion y lint. Git es
  el log; no crear `log.md`.

### 2. Crear conocimiento backend por flujo

- Extraer login desde `AuthController`, `ILoginFlowService`, 2FA, captcha, cookies,
  Redis/rate limits, LDAP y sus tests.
- Extraer registro desde `RegistroController`, `AppLogic/Registro/`, reconocimiento
  documental, activacion, mails, Redis/LDAP/Oracle y sus tests.
- Extraer inscripciones desde `InscripcionesController`, endpoints de identidad de
  `PersonaController`, `AppLogic/Inscripciones/`, Tivenos, API de pagos, Oracle y
  tests de controller/service.
- Corregir el link absoluto de Windows en el README de ambientes y cualquier otra
  referencia rota detectada durante el lint.

### 3. Agregar el gate minimo de CI

Agregar `scripts/docs/check-knowledge.ps1`, sin dependencias nuevas:

- validar JSON y `flowId` unicos;
- validar que documentos y `sourcePaths` existan;
- detectar en `git diff` cambios `.cs` bajo un `sourcePaths`;
- exigir que cambie la pagina correspondiente;
- ignorar `bin/`, `obj/`, Devart generado y cambios solo de tests;
- permitir `docs-none: <motivo>` en el cuerpo del PR, rechazando motivo vacio.

Ejecutarlo en `.github/workflows/ci-sonarqube-tests.yml` antes del build. Incluir un
modo `-SelfTest` con casos de manifest invalido, source faltante, cambio sin docs,
cambio con docs y exencion valida.

### 4. Alinear el portal frontend

- Configurar los enlaces del portal a frontend `v1.0.0/main` y backend `develop`
  desde un unico archivo de referencias.
- Usar los mismos tres `flowId`.
- En cada tabla end-to-end enlazar la pagina backend y los archivos exactos del
  controller/service/test; no copiar su explicacion interna.
- Cuando backend cambie contrato: actualizar XML docs/OpenAPI y pagina backend en
  el PR backend; luego regenerar contratos y actualizar el recorrido en un PR
  frontend enlazado.

## Segunda etapa: bridge reproducible de OpenAPI

No bloquear la primera etapa con automatizacion cross-repo. Cuando el portal y los
tres flujos ya esten estables:

1. La CI backend publica `swagger.json` junto a un manifest con repo, commit SHA,
   branch y fecha.
2. El frontend consume ese artifact con el `--spec-dir` que ya soporta su codegen,
   en vez de depender exclusivamente del Swagger vivo de desarrollo.
3. El portal muestra el SHA backend usado para generar los contratos.
4. Un cambio de OpenAPI no acompañado por el PR frontend correspondiente queda
   visible como drift, sin reescribir documentos automaticamente.

Usar GitHub App/token de organizacion solo cuando se automatice la descarga del
artifact privado. No agregar submodulo, copia periodica ni bot que reescriba ambos
repositorios.

## Criterios de aceptacion

- Desde cada click importante de login, registro e inscripciones se llega a un
  endpoint, controller, service, side effects y tests verificables.
- OpenAPI y docs backend coinciden para rutas, requests, responses y status codes.
- Ninguna pagina frontend duplica reglas internas del backend.
- Cambiar codigo backend mapeado sin actualizar conocimiento falla CI, salvo
  `docs-none` con motivo.
- Cambiar `defaultRef` permite pasar de `develop` a la rama/commit de produccion sin
  reestructurar documentos.
- El bridge no requiere RAG, QMD, base vectorial, watcher ni sincronizador propio.

## Orden de trabajo

1. Copiar este plan al backend y crear una rama desde `develop`.
2. Corregir instrucciones obsoletas y links rotos.
3. Crear manifest, `docs/AGENTS.md` y las tres paginas de flujo.
4. Agregar el check de CI.
5. Actualizar el portal frontend con los mismos `flowId` y refs.
6. Evaluar el artifact OpenAPI solo despues de usar el bridge en PRs reales.
