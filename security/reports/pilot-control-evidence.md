# Pilot control evidence review

Reviewed on 2026-09-15 against commit `fbe7a8ee83a3542f8b021e25edd90eb0963fb491`. A located implementation is not treated as PASS without a test or other complete, commit-bound proof.

| Control | Located evidence | Assessment |
|---|---|---|
| `v5.0.0-2.2.2` | Global `JsonSchemaValidationFilter` registration and its unit tests exist. | PENDING: this does not prove that all relevant trusted-service inputs are validated. |
| `v5.0.0-3.3.4` | `CookieAuthenticationHelper` sets `HttpOnly = true` for access, refresh and temporary cookies. | PENDING: no focused test asserting the emitted `Set-Cookie` attributes was found. |
| `v5.0.0-7.2.1` | JWT bearer validation and `CurrentUserService` tests exist; protected controllers obtain the person ID from claims. | PENDING: no end-to-end inventory proves that every session-token verification path is backend-trusted. |
| `v5.0.0-8.2.2` | `ScholarshipFundServiceTests.SubirArchivoIngreso_CuandoNoPerteneceALaPersona_ReturnsForbidden` passed 1/1 and verifies 403 plus no save for a resource owned by another person. | PENDING with informational evidence: this is a real object-level authorization test, but covers one resource path only. |
| `v5.0.0-13.3.1` | Environment-variable configuration and redaction mechanisms exist. | PENDING: repository configuration cannot prove production secret storage or rotation; human/infrastructure evidence is required. |
| `v5.0.0-13.4.2` | Middleware exposes Swagger only in LocalHost, Development and Testing; production exception-detail test exists. | PENDING: these are partial debug-surface checks, not proof for every production component. |

Executed:

```text
dotnet test api-admisiones/WebApiAdmisiones/UnitTesting/UnitTesting.csproj --no-build --no-restore --filter FullyQualifiedName~ScholarshipFundServiceTests.SubirArchivoIngreso_CuandoNoPerteneceALaPersona_ReturnsForbidden --verbosity quiet
Passed: 1, Failed: 0, Skipped: 0.
```

No artificial IDOR/BOLA test was added and no application behavior was changed.
