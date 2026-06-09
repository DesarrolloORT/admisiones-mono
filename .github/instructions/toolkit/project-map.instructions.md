---
name: project-map
description: 'Mapa estructural del repositorio para navegacion rapida del agente.'
applyTo: '**'
---

# Project Map

```
admisiones/
├── src/
│   ├── main.ts                          # Bootstrap Angular
│   ├── styles.scss                      # Estilos globales
│   ├── index.html
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.generated.ts     # Generado por CI/scripts
│   └── app/
│       ├── app.ts                       # Componente root
│       ├── app.routes.ts                # Router principal
│       ├── app.config.ts                # Providers Angular
│       ├── core/
│       │   ├── guards/
│       │   │   └── auth.ts              # Guard de autenticacion
│       │   ├── interceptors/
│       │   │   └── http.ts              # Interceptor HTTP global
│       │   ├── services/
│       │   │   ├── api-error-notifier.ts
│       │   │   ├── captcha-token.ts
│       │   │   └── telemetry.ts
│       │   └── storage/
│       │       └── keys.ts              # Claves de storage
│       ├── features/
│       │   ├── auth/
│       │   │   ├── auth.routes.ts
│       │   │   ├── pages/
│       │   │   │   ├── login/
│       │   │   │   ├── register/
│       │   │   │   ├── recover-access/
│       │   │   │   └── set-password/
│       │   │   ├── components/
│       │   │   │   ├── auth-form/
│       │   │   │   ├── location-select/
│       │   │   │   ├── register-identity-step/
│       │   │   │   ├── register-personal-step/
│       │   │   │   └── ...
│       │   │   ├── facades/
│       │   │   │   ├── login.facade.ts
│       │   │   │   ├── register-flow.facade.ts
│       │   │   │   ├── recover-access.facade.ts
│       │   │   │   └── set-password.facade.ts
│       │   │   ├── services/
│       │   │   │   ├── account.ts
│       │   │   │   ├── auth-session.ts
│       │   │   │   ├── document-prefill.ts
│       │   │   │   ├── document-recognition.ts
│       │   │   │   ├── password-activation.ts
│       │   │   │   └── registration.ts
│       │   │   ├── endpoints/
│       │   │   │   ├── account.endpoint.ts
│       │   │   │   └── auth.endpoint.ts
│       │   │   ├── models/
│       │   │   ├── mappers/
│       │   │   ├── forms/
│       │   │   └── store/
│       │   ├── catalogs/
│       │   │   ├── endpoints/
│       │   │   │   └── catalogs.endpoint.ts
│       │   │   ├── models/
│       │   │   └── services/
│       │   ├── home/
│       │   │   ├── home.routes.ts
│       │   │   ├── pages/
│       │   │   │   ├── dashboard/
│       │   │   │   ├── home/
│       │   │   │   ├── change-password/
│       │   │   │   └── personal-data/
│       │   │   ├── components/
│       │   │   │   ├── dashboard-career-status-chip/
│       │   │   │   ├── dashboard-career-summary/
│       │   │   │   ├── dashboard-quick-actions/
│       │   │   │   └── home-header/
│       │   │   ├── layouts/
│       │   │   │   └── home-layout/
│       │   │   ├── endpoints/
│       │   │   │   └── home.endpoint.ts
│       │   │   └── models/
│       │   └── inscripciones/
│       │       ├── inscripciones.routes.ts
│       │       ├── pages/
│       │       │   └── inscripcion/
│       │       ├── components/
│       │       │   ├── inscripcion-academic-step/
│       │       │   ├── inscripcion-confirmation-step/
│       │       │   ├── inscripcion-personal-step/
│       │       │   ├── inscripcion-success-step/
│       │       │   ├── inscripcion-shell/
│       │       │   ├── inscripcion-radio-card/
│       │       │   └── ...
│       │       ├── facades/
│       │       │   └── inscripcion-flow.facade.ts
│       │       └── models/
│       └── shared/
│           ├── animations/
│           │   └── fade-in-out.ts
│           ├── api/
│           │   ├── core/
│           │   │   ├── api-endpoint.ts     # Clase base endpoints
│           │   │   ├── api-http-client.ts   # HttpClient wrapper
│           │   │   └── api-path-builder.ts  # Armado de URLs
│           │   └── generated/
│           │       ├── endpoints/           # Endpoints autogenerados (OpenAPI)
│           │       └── models/              # DTOs autogenerados (OpenAPI)
│           ├── forms/
│           │   ├── form-error-summary.ts
│           │   ├── matching-fields.validator.ts
│           │   └── password-validation.ts
│           └── ui/
│               ├── loader/
│               │   └── loader.ts
│               └── snackbar/
│                   └── snackbar-handler.ts
├── e2e/
│   ├── registration.spec.ts
│   ├── app-flows.spec.ts
│   ├── a11y.spec.ts
│   ├── assisted.spec.ts
│   ├── nightly.spec.ts
│   ├── observability.spec.ts
│   └── support/
│       ├── pages/                          # Page Objects
│       └── test-data/
├── scripts/
│   ├── ai-hooks/                           # Hooks para agentes AI
│   ├── codegen/                            # Generacion OpenAPI (endpoints + models)
│   └── testing/                            # Deteccion y generacion de tests faltantes
├── docs/
│   ├── ARCHITECTURE.md
│   ├── SETUP.md
│   ├── WORKFLOW.md
│   ├── REGISTER-FLOW.md
│   ├── ERROR-HANDLING.md
│   ├── ACCESSIBILITY.md
│   ├── BEST-PRACTICES.md
│   └── E2E-GUARDRAILS.md
├── .github/
│   ├── copilot-instructions.md             # Baseline AI del equipo
│   ├── instructions/toolkit/               # Instrucciones por dominio
│   ├── prompts/                            # Prompts reutilizables
│   └── skills/                             # Skills para agentes
├── angular.json
├── eslint.config.js
├── playwright.config.ts
├── vitest-global-mocks.ts
├── setup-vitest.ts
├── openapitools.json                       # Config generador OpenAPI
├── AGENTS.md
├── CLAUDE.md
└── CONTRIBUTING.md
```

