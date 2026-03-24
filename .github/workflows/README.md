# DevOps Workflows — API Ficha de Persona (FDP)

Documentación completa de los flujos de CI/CD del repositorio. Todos los workflows se encuentran en `.github/workflows/`.

---

## Tabla de Contenidos

- [Resumen de Workflows](#resumen-de-workflows)
- [Ciclo de Vida DevOps (Visión General)](#ciclo-de-vida-devops-visión-general)
- [1. pr-validate-version — Validación de Convención de Versión](#1-pr-validate-version)
- [2. pr-open — Auto Documentación y Etiquetado de PR](#2-pr-open)
- [3. ci-sonarqube-tests — CI: Análisis de Calidad y Tests](#3-ci-sonarqube-tests)
- [4. pr-close — Actualización de Estado al Cerrar PR](#4-pr-close)
- [5. auto-release — Release Automático con Versionado Semántico](#5-auto-release)
- [6. cd-quality-gate-docker-publish — CD: Quality Gate y Publicación Docker](#6-cd-quality-gate-docker-publish)
- [7. wd-docker-image-builder — Builder Manual de Imágenes Docker](#7-wd-docker-image-builder)
- [Secretos Requeridos](#secretos-requeridos)

---

## Resumen de Workflows

| Archivo | Nombre | Trigger | Runner | Propósito |
|---|---|---|---|---|
| `pr-validate-version.yml` | Validate PR Version Convention | PR → `main` | `ubuntu-latest` | Valida que el PR incluya convención de versión |
| `pr-open.yml` | PR – Auto Document, Label & Summarize | PR abierto/editado/sync | `ubuntu-latest` | Etiqueta, documenta y resume el PR automáticamente |
| `ci-sonarqube-tests.yml` | CI - SonarQube and Tests | PR → `develop/testing/main`, push `main` | `sonar-runner` | Análisis estático, cobertura y tests unitarios |
| `pr-close.yml` | PR – Update Status on Close | PR cerrado | `ubuntu-latest` | Actualiza labels y genera reporte final |
| `auto-release.yml` | Auto Release (Semantic Versioning) | Push → `main` | `ubuntu-latest` | Crea tag y release de GitHub automáticamente |
| `cd-quality-gate-docker-publish.yml` | CI/CD - SonarQube, Tests and Docker Publish | Push → `main` | `sonar-runner` + `ubuntu-latest` | Quality Gate + Build y publicación de imagen Docker |
| `wd-docker-image-builder.yml` | Docker Image Builder | `workflow_dispatch` (manual) | `ubuntu-latest` | Construcción manual de imágenes Docker |

---

## Ciclo de Vida DevOps (Visión General)

Diagrama de alto nivel que muestra cómo interactúan todos los workflows a lo largo del ciclo de vida de un cambio.

```mermaid
flowchart TD
    DEV([👨‍💻 Developer<br/>Crea rama]) --> PR_OPEN[Abre Pull Request]

    PR_OPEN --> |"PR hacia main"| W_VALIDATE["🔖 pr-validate-version<br/>Valida convención de versión"]
    PR_OPEN --> W_PR_OPEN["📋 pr-open<br/>Etiqueta + Documenta + Resume"]
    PR_OPEN --> |"PR hacia develop/testing/main"| W_CI["🔬 ci-sonarqube-tests<br/>SonarQube + Tests + Cobertura"]

    W_VALIDATE --> |"✅ Convención válida"| REVIEW[Code Review]
    W_VALIDATE --> |"❌ Sin convención"| BLOCKED[PR Bloqueado]
    BLOCKED --> PR_OPEN

    W_PR_OPEN --> REVIEW
    W_CI --> |"✅ Tests OK<br/>Quality Gate en SonarQube"| REVIEW

    REVIEW --> MERGE{"¿Merge?"}

    MERGE --> |"❌ Cerrado sin merge"| W_PR_CLOSE["🏁 pr-close<br/>Label 'closed' + Reporte Final"]
    MERGE --> |"✅ Merge a main"| W_PR_CLOSE2["🏁 pr-close<br/>Label 'merged' + Reporte Final"]

    MERGE --> |"✅ Push a main"| W_CD["🚀 cd-quality-gate-docker-publish<br/>Quality Gate Check → Docker Push"]
    MERGE --> |"✅ Push a main"| W_RELEASE["🏷️ auto-release<br/>Calcula versión → Crea Tag + Release"]

    W_CD --> GHCR["📦 GHCR<br/>ghcr.io/desarrolloort/api-fdp"]
    W_RELEASE --> GITHUB_RELEASE["📝 GitHub Release<br/>vX.Y.Z"]

    MANUAL([👷 Operador]) --> |workflow_dispatch| W_MANUAL["🔧 wd-docker-image-builder<br/>Build manual desde cualquier rama"]
    W_MANUAL --> GHCR

    style BLOCKED fill:#f44,color:#fff
    style GHCR fill:#2496ed,color:#fff
    style GITHUB_RELEASE fill:#6f42c1,color:#fff
    style W_CI fill:#0e8a16,color:#fff
    style W_CD fill:#0e8a16,color:#fff
    style W_RELEASE fill:#6f42c1,color:#fff
```

---

## 1. pr-validate-version

**Archivo:** `pr-validate-version.yml`  
**Trigger:** Pull Request hacia `main` (opened, edited, reopened, synchronize)  
**Runner:** `ubuntu-latest`

### Qué hace

Valida que el título o cuerpo del PR incluya **exactamente una** convención de versión antes de permitir el merge a `main`. Actúa como gate de calidad de versionado.

**Convenciones aceptadas (exactamente una de las siguientes):**
- `vX.Y.Z` — versión explícita (ej: `v2.1.0`)
- `#major` — incremento de versión mayor
- `#minor` — incremento de versión menor
- `#patch` — incremento de versión de parche

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([PR abierto/editado<br/>hacia main]) --> STEP1[Leer título y cuerpo del PR]
    STEP1 --> STEP2{¿Contiene alguna<br/>convención?}

    STEP2 --> |"Ninguna encontrada"| FAIL1["❌ Error: PR debe especificar<br/>una convención de versión"]
    STEP2 --> |"Más de una encontrada"| FAIL2["❌ Error: Solo se permite<br/>una convención a la vez"]
    STEP2 --> |"Exactamente una"| PASS["✅ Convención de versión válida"]

    FAIL1 --> EXIT_FAIL([exit 1 — Check fallido])
    FAIL2 --> EXIT_FAIL
    PASS --> EXIT_OK([exit 0 — Check exitoso])

    style FAIL1 fill:#c32607,color:#fff
    style FAIL2 fill:#c32607,color:#fff
    style PASS fill:#0e8a16,color:#fff
```

---

## 2. pr-open

**Archivo:** `pr-open.yml`  
**Trigger:** Pull Request (opened, edited, synchronize) — cualquier rama  
**Runner:** `ubuntu-latest`  
**Concurrencia:** Un job por número de PR (cancela ejecuciones previas del mismo PR)

### Qué hace

Automatiza la gestión documental de cada PR en cuatro pasos secuenciales:

1. **Setup de Labels:** Crea o actualiza todas las etiquetas del repositorio con sus colores (capas de arquitectura, tipos de archivos, tamaños, estados).
2. **Auto Labeling:** Aplica etiquetas según las rutas de archivos modificados usando `.github/labeler.yml`.
3. **Label de Tamaño:** Calcula el total de líneas cambiadas y aplica `size:XS/S/M/L/XL`.
4. **Resumen y Documentación:** Publica un comentario automático en el PR con la lista de archivos modificados y la categorización por capa de arquitectura.

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([PR opened / edited / synchronize]) --> STEP0["Checkout<br/>(sin submódulos)"]

    STEP0 --> STEP1["🏷️ Setup Labels with Colors<br/>Crea etiquetas de arquitectura,<br/>tipo, tamaño y estado"]

    STEP1 --> STEP2["🤖 Auto Label PR<br/>Aplica labels según rutas<br/>de archivos (labeler.yml)"]

    STEP2 --> STEP3["📏 Label PR Size<br/>Calcula líneas cambiadas<br/>Additions + Deletions"]

    STEP3 --> SIZE{Total de<br/>líneas}
    SIZE --> |"< 100"| XS["size:XS"]
    SIZE --> |"100–499"| S["size:S"]
    SIZE --> |"500–1499"| M["size:M"]
    SIZE --> |"1500–2999"| L["size:L"]
    SIZE --> |"≥ 3000"| XL["size:XL"]

    XS & S & M & L & XL --> STEP4["📊 Generate PR Summary<br/>Lista archivos + líneas modificadas"]

    STEP4 --> STEP5["📝 Generate Change Documentation<br/>Categoriza archivos por capa:<br/>WebAPI / AppLogic / BusinessLogic<br/>DataAccess / Tests / CI-CD / Config"]

    STEP5 --> WARN{¿Archivos<br/>Devart o .Generated.cs?}
    WARN --> |"Sí"| STEP6A["⚠️ Agrega advertencia<br/>de archivos auto-generados"]
    WARN --> |"No"| STEP6B[Sin advertencia]

    STEP6A & STEP6B --> STEP7["💬 Post PR Summary & Documentation<br/>Publica comentario en el PR"]

    STEP7 --> END([Fin])

    style STEP1 fill:#28a745,color:#fff
    style STEP2 fill:#0e8a16,color:#fff
    style STEP3 fill:#f9a825,color:#000
    style STEP7 fill:#0075ca,color:#fff
```

---

## 3. ci-sonarqube-tests

**Archivo:** `ci-sonarqube-tests.yml`  
**Trigger:**
  - Pull Requests hacia `develop`, `testing`, `main` (opened, synchronize, reopened)
  - Push directo a `main`

**Runner:** `sonar-runner` (self-hosted con SonarScanner.MSBuild instalado)  
**Concurrencia:** Un job por workflow + ref (cancela ejecuciones previas)  
**Timeout:** 30 minutos

### Qué hace

Pipeline de integración continua completo que ejecuta análisis de calidad estático con SonarQube y los tests unitarios con cobertura de código.

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([PR a develop/testing/main<br/>o Push a main]) --> CHECKOUT["Checkout con submódulos<br/>(token SUBMODULES_TOKEN)"]

    CHECKOUT --> CACHE["♻️ Cache NuGet packages<br/>(clave por hash de .csproj)"]
    CACHE --> DOTNET["⚙️ Setup .NET 10 SDK<br/>(en runner temp)"]
    DOTNET --> PATH["Agregar .NET al PATH"]
    PATH --> VERIFY["Verificar versión dotnet"]

    VERIFY --> SONAR_BEGIN["🔬 Begin SonarQube Scan<br/>/k: ProjectKey<br/>/d: sonar.host.url<br/>Excluye: bin, obj, Devart*, Core/**<br/>Instruye cobertura OpenCover"]

    SONAR_BEGIN --> RESTORE["dotnet restore WebApiFDP.sln"]
    RESTORE --> BUILD["dotnet build --configuration Release --no-restore"]

    BUILD --> TESTS["🧪 Run Unit Tests + Coverage<br/>dotnet test UnitTesting.csproj<br/>--settings tests.runsettings<br/>Logger: TRX<br/>Cobertura: Cobertura XML"]

    TESTS --> REPORT_GEN["📊 Install ReportGenerator<br/>Convierte Cobertura → SonarQube XML"]

    REPORT_GEN --> VERIFY_REPORT{¿Existe<br/>SonarQube.xml?}
    VERIFY_REPORT --> |"No"| FAIL["❌ Error: Reporte no encontrado<br/>exit 1"]
    VERIFY_REPORT --> |"Sí"| UPLOAD["⬆️ Upload Artifact<br/>sonarqube-report<br/>(retención 7 días)"]

    UPLOAD --> SONAR_END["🔬 End SonarQube Scan<br/>Envía resultados + cobertura<br/>al servidor SonarQube"]

    SONAR_END --> END([Análisis completado<br/>Quality Gate disponible en SonarQube])

    style SONAR_BEGIN fill:#0e8a16,color:#fff
    style SONAR_END fill:#0e8a16,color:#fff
    style TESTS fill:#00bcd4,color:#fff
    style FAIL fill:#c32607,color:#fff
    style UPLOAD fill:#0075ca,color:#fff
```

---

## 4. pr-close

**Archivo:** `pr-close.yml`  
**Trigger:** Pull Request cerrado (`types: [closed]`) — cualquier rama  
**Runner:** `ubuntu-latest`  
**Timeout:** 5 minutos

### Qué hace

Al cerrarse un PR (con o sin merge), actualiza las etiquetas de estado y genera un reporte final con métricas del PR.

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([PR cerrado]) --> CHECKOUT["Checkout<br/>(sin submódulos)"]

    CHECKOUT --> SETUP_LABELS["🏷️ Setup Status Labels<br/>Crea labels 'merged' y 'closed'<br/>si no existen"]

    SETUP_LABELS --> DETERMINE{¿Fue<br/>mergeado?}

    DETERMINE --> |"merged = true"| LABEL_MERGED["✅ Agrega label 'merged'"]
    DETERMINE --> |"merged = false"| LABEL_CLOSED["❌ Agrega label 'closed'"]

    LABEL_MERGED & LABEL_CLOSED --> REMOVE_LABELS["🧹 Elimina labels temporales:<br/>- no-pr-activity<br/>- in-review<br/>- needs-info"]

    REMOVE_LABELS --> GEN_REPORT["📊 Generate Final Report<br/>Conteo de archivos por capa:<br/>WebAPI / AppLogic / BusinessLogic<br/>DataAccess / Tests"]

    GEN_REPORT --> COMMENT["💬 Publica comentario en PR:<br/>🏁 Final Report<br/>- Estado (mergeado/cerrado)<br/>- Archivos modificados<br/>- Líneas +/- <br/>- Branch origen → destino<br/>- Capas afectadas"]

    COMMENT --> END([Fin])

    style LABEL_MERGED fill:#6f42c1,color:#fff
    style LABEL_CLOSED fill:#cb2431,color:#fff
    style COMMENT fill:#0075ca,color:#fff
```

---

## 5. auto-release

**Archivo:** `auto-release.yml`  
**Trigger:** Push a `main`  
**Runner:** `ubuntu-latest`  
**Permisos:** `contents: write` (para crear tags y releases)

### Qué hace

Crea automáticamente un tag de Git y un GitHub Release cuando se mergea código a `main`. La versión se calcula leyendo las señales del PR mergeado usando versionado semántico.

**Reglas de cálculo de versión:**
- Si el PR contiene `vX.Y.Z` explícito → usa esa versión
- Si contiene `#major` → incrementa MAJOR, resetea MINOR y PATCH
- Si contiene `#minor` → incrementa MINOR, resetea PATCH
- Si contiene `#patch` o ninguna señal → incrementa PATCH
- Si el tag calculado ya existe → falla con error

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([Push a main]) --> CHECKOUT["Checkout completo<br/>(fetch-depth: 0<br/>para acceder a todos los tags)"]

    CHECKOUT --> GET_TAG["🔖 Get latest tag<br/>git tag -l 'v*' --sort=-v:refname<br/>Obtiene el último tag semántico"]

    GET_TAG --> EXTRACT_SIGNALS["🔍 Extract PR Signals<br/>Consulta GitHub API:<br/>repos/.../commits/SHA/pulls<br/>Obtiene título y cuerpo del PR mergeado"]

    EXTRACT_SIGNALS --> PARSE{¿Señal<br/>encontrada?}

    PARSE --> |"vX.Y.Z explícito"| EXPLICIT["Versión explícita<br/>Ej: v2.1.0"]
    PARSE --> |"#major"| MAJOR_BUMP["MAJOR+1, MINOR=0, PATCH=0"]
    PARSE --> |"#minor"| MINOR_BUMP["MINOR+1, PATCH=0"]
    PARSE --> |"#patch o ninguna"| PATCH_BUMP["PATCH+1"]
    PARSE --> |"Sin PR asociado"| DEFAULT["Default: PATCH+1"]

    EXPLICIT & MAJOR_BUMP & MINOR_BUMP & PATCH_BUMP & DEFAULT --> CALC["📐 Calculate Version<br/>Compone nuevo tag: vMAJOR.MINOR.PATCH"]

    CALC --> CHECK_EXISTS{¿El tag<br/>ya existe?}
    CHECK_EXISTS --> |"Sí"| FAIL["❌ Error: Tag ya existe<br/>exit 1"]
    CHECK_EXISTS --> |"No"| CREATE_RELEASE["🚀 Create Tag & Release<br/>softprops/action-gh-release@v1<br/>- Crea tag Git<br/>- Genera release notes automáticas<br/>- Publica en GitHub Releases"]

    CREATE_RELEASE --> END([Release publicado en GitHub])

    style FAIL fill:#c32607,color:#fff
    style CREATE_RELEASE fill:#6f42c1,color:#fff
    style EXPLICIT fill:#0075ca,color:#fff
    style MAJOR_BUMP fill:#c32607,color:#fff
    style MINOR_BUMP fill:#f9a825,color:#000
    style PATCH_BUMP fill:#0e8a16,color:#fff
```

---

## 6. cd-quality-gate-docker-publish

**Archivo:** `cd-quality-gate-docker-publish.yml`  
**Trigger:** Push a `main`  
**Runners:** `sonar-runner` (Job 1) + `ubuntu-latest` (Job 2)  
**Concurrencia:** Un job por workflow + ref (cancela ejecuciones previas)

### Qué hace

Pipeline de entrega continua en dos jobs secuenciales. Primero verifica que el análisis de SonarQube haya pasado el Quality Gate, y solo si pasa, construye y publica la imagen Docker en GHCR.

**Tags Docker generados al hacer push a `main`:**
- `ghcr.io/desarrolloort/api-fdp:main-<SHA>`
- `ghcr.io/desarrolloort/api-fdp:main-latest`
- `ghcr.io/desarrolloort/api-fdp:latest`

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([Push a main]) --> JOB1

    subgraph JOB1["Job 1: Quality Gate Check (sonar-runner)"]
        QG_POLL["🔄 Poll SonarQube Quality Gate<br/>Máx. 12 reintentos cada 15s<br/>Ignora SSL para servidores internos"]
        QG_POLL --> QG_CHECK{Estado del<br/>Quality Gate}
        QG_CHECK --> |"OK"| QG_PASS["✅ Quality Gate PASSED"]
        QG_CHECK --> |"WARN/ERROR/NONE"| QG_FAIL["❌ Quality Gate FAILED<br/>exit 1"]
        QG_CHECK --> |"Pendiente (< 3 intentos)"| QG_WAIT["⏳ Esperar 15s y reintentar"]
        QG_WAIT --> QG_POLL
        QG_PASS --> QG_SUMMARY["📋 Escribe resultado<br/>en GitHub Step Summary"]
        QG_FAIL --> QG_SUMMARY
    end

    JOB1 --> |"result = success"| JOB2
    JOB1 --> |"result = skipped"| JOB2
    JOB1 --> |"result = failure"| STOP([🛑 Pipeline detenido<br/>No se construye imagen])

    subgraph JOB2["Job 2: Build and Publish Docker Image (ubuntu-latest)"]
        CHECKOUT2["Checkout con submódulos<br/>(token SUBMODULES_TOKEN)"]
        CHECKOUT2 --> BUILDX["⚙️ Setup Docker Buildx"]
        BUILDX --> LOGIN["🔑 Login a GHCR<br/>(docker/login-action)"]
        LOGIN --> METADATA["📋 Generate Docker Metadata<br/>(docker/metadata-action)<br/>Tags: SHA prefix, branch-latest, latest"]
        METADATA --> BUILD_PUSH["🐳 Build and Push Docker Image<br/>(docker/build-push-action)<br/>Contexto: raíz del repo<br/>Dockerfile: FichaDePersona/WebApiFDP/Dockerfile<br/>Cache: GitHub Actions Cache"]
    end

    BUILD_PUSH --> GHCR(["📦 Imagen publicada en<br/>ghcr.io/desarrolloort/api-fdp"])

    style QG_PASS fill:#0e8a16,color:#fff
    style QG_FAIL fill:#c32607,color:#fff
    style STOP fill:#c32607,color:#fff
    style GHCR fill:#2496ed,color:#fff
    style BUILD_PUSH fill:#2496ed,color:#fff
```

---

## 7. wd-docker-image-builder

**Archivo:** `wd-docker-image-builder.yml`  
**Trigger:** `workflow_dispatch` (ejecución manual desde GitHub Actions UI)  
**Runner:** `ubuntu-latest`  
**Timeout:** 30 minutos

### Qué hace

Permite a un operador construir y publicar manualmente una imagen Docker desde **cualquier rama** del repositorio. Ofrece dos modos de operación:

| Input | Descripción | Requerido |
|---|---|---|
| `branch` | Nombre de la rama a construir | ✅ Sí |
| `stable_branch_mode` | Usa tags igual que el CI/CD automático | No (default: `false`) |
| `tag_suffix` | Sufijo personalizado para la imagen | No (default: `"manual"`) |

**Modo `stable_branch_mode = true`** — Tags generados:
- `ghcr.io/desarrolloort/api-fdp:<branch>-<SHA>`
- `ghcr.io/desarrolloort/api-fdp:<branch>-latest`
- `ghcr.io/desarrolloort/api-fdp:latest` *(solo si rama es `main`)*

**Modo `stable_branch_mode = false`** — Tags generados:
- `ghcr.io/desarrolloort/api-fdp:manual-<SHA>`
- `ghcr.io/desarrolloort/api-fdp:manual-<branch>-<suffix>-<timestamp>`

### Diagrama de Ejecución

```mermaid
flowchart TD
    START([workflow_dispatch<br/>desde GitHub UI]) --> INPUTS["📥 Inputs:<br/>- branch (requerido)<br/>- stable_branch_mode (bool)<br/>- tag_suffix (string)"]

    INPUTS --> VALIDATE["🔍 Validate branch exists<br/>git ls-remote --heads origin BRANCH"]
    VALIDATE --> EXISTS{¿Rama<br/>existe?}
    EXISTS --> |"No"| FAIL["❌ Error: Rama no existe<br/>exit 1"]
    EXISTS --> |"Sí"| CHECKOUT["Checkout rama especificada<br/>(con submódulos via SUBMODULES_TOKEN)"]

    CHECKOUT --> QEMU["⚙️ Setup QEMU<br/>(soporte multi-plataforma)"]
    QEMU --> BUILDX["⚙️ Setup Docker Buildx"]
    BUILDX --> LOGIN["🔑 Login a GHCR"]

    LOGIN --> MODE{stable_branch_mode}

    MODE --> |"true"| META_STABLE["📋 Generate Docker Metadata<br/>(stable mode)<br/>Tags: branch-SHA, branch-latest<br/>+ latest si es main"]
    MODE --> |"false"| META_MANUAL["📐 Determine image tags<br/>(manual mode)<br/>Sanitiza nombre de rama<br/>Tags: manual-SHA, manual-branch-suffix-timestamp"]

    META_STABLE --> BUILD_STABLE["🐳 Build and Push<br/>(stable mode)<br/>Con GHA Cache"]
    META_MANUAL --> BUILD_MANUAL["🐳 Build and Push<br/>(manual mode)<br/>Con labels OCI"]

    BUILD_STABLE & BUILD_MANUAL --> GHCR(["📦 Imagen publicada en<br/>ghcr.io/desarrolloort/api-fdp"])

    style FAIL fill:#c32607,color:#fff
    style GHCR fill:#2496ed,color:#fff
    style BUILD_STABLE fill:#2496ed,color:#fff
    style BUILD_MANUAL fill:#2496ed,color:#fff
    style META_STABLE fill:#0e8a16,color:#fff
    style META_MANUAL fill:#f9a825,color:#000
```

---

## Secretos Requeridos

| Secreto | Usado en | Descripción |
|---|---|---|
| `GITHUB_TOKEN` | Todos | Token automático de GitHub Actions |
| `SUBMODULES_TOKEN` | `ci-sonarqube-tests`, `cd-quality-gate-docker-publish`, `wd-docker-image-builder` | PAT con acceso al submódulo privado `Core` |
| `SONAR_TOKEN` | `ci-sonarqube-tests`, `cd-quality-gate-docker-publish` | Token de autenticación para SonarQube |
| `SONAR_HOST_URL` | `ci-sonarqube-tests`, `cd-quality-gate-docker-publish` | URL del servidor SonarQube interno |
| `SONAR_PROJECT_KEY` | `ci-sonarqube-tests`, `cd-quality-gate-docker-publish` | Clave del proyecto en SonarQube |

> **Nota:** `SUBMODULES_TOKEN` es crítico. Sin él, el checkout del submódulo `Core` falla y el build no puede completarse. Ver la sección de prerrequisitos en [copilot-instructions.md](../copilot-instructions.md).
