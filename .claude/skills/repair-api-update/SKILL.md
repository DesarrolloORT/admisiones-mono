---
name: repair-api-update
description: Repair frontend consumers after npm run update-api changes generated OpenAPI endpoints, models, or form contracts. Use when update-api breaks compilation, reports stale imports, introduces contract drift, or adds API fields whose impact must be traced through adapters, mappers, services, facades, components, templates, tests, and canonical flow documentation.
---

# Repair API Update

Repair the last API update from the generated boundary outward. Make the smallest coherent change that restores the real feature flow.

## Follow the authority order

Use this precedence without preserving stale frontend behavior:

1. Current backend contracts in `src/app/shared/api/generated/contracts/`.
2. Current generated endpoints and models.
3. Existing frontend code, tests, and documentation.

The previous snapshot is evidence of what changed, not an authority. Always migrate the frontend to the current API. When a field, endpoint, request, or response disappears, remove its dependent adapter mapping, feature types, state, validation, component/template UI, tests, and documentation along the proven call path. Do not keep removed behavior with empty fallbacks, casts, assertions, handwritten DTOs, or compatibility layers.

If contracts and generated endpoints/models disagree, contracts define the intended result. Do not edit generated output; report the generator or backend publication drift as a blocker unless the code generator itself is in scope.

## Preserve evidence

- Run `git status --short` and preserve unrelated user changes.
- Require both `tmp/update-api/previous/` and `src/app/shared/api/generated/`.
- If the snapshot is missing, stop. Do not rerun `update-api`, because that would destroy the previous contract evidence.
- Never edit either generated tree.

## Identify the exact delta

- Compare names first:

  ```bash
  git diff --no-index --name-status -- tmp/update-api/previous src/app/shared/api/generated
  ```

- Treat exit code `1` as "differences found", not command failure.
- Diff only changed files in detail. Classify endpoint renames/removals, method/path changes, request fields, response shapes, model fields, and form-contract changes.
- If a generated response is `unknown`, block that flow and report the missing backend Swagger schema. Independent proven-safe repairs may continue, but do not declare the API repair complete. Never hide `unknown` with `any`, assertions, or handwritten generated types.

## Trace impacted flows

- If `.codegraph/` exists, use CodeGraph before text search. Query each changed generated symbol and trace:

  ```text
  generated -> feature endpoint adapter -> mapper/service/facade/store -> component/page -> template
  ```

- Start from adapter imports under `src/app/features/*/endpoints/`. Only widen the search to callers that CodeGraph identifies.
- Read `docs/index.md`. For login, registration, or inscriptions, read the matching `businessId` page in `docs/flujos/` before changing behavior.
- Keep the adapter public contract stable only for URL, operationId, generated-name, or backend-only shape changes.
- Propagate additions, removals, and semantic contract changes through every affected frontend consumer.
- Ask the user only when the current backend contract requires a product decision that it does not define. Never ask whether stale frontend behavior should override the contract.

## Repair from the boundary outward

- Update generated imports and explicit request/response mapping inside the feature adapter.
- Expose only feature-owned types from public adapter methods.
- Keep API dates as `string | null`; convert to `Date` explicitly in facade or UI when needed.
- Update mapper, service, facade, store, component, and template only along the proven call path.
- For removed API capabilities, delete the corresponding frontend behavior instead of manufacturing replacement data.
- Update the smallest tests that lock changed mapping or behavior.
- For a generated rename with identical HTTP and public feature contracts, change only the adapter and run its existing coverage; do not rewrite tests without a behavior change.
- Update the canonical flow page when behavior changes. Otherwise report `docs-none: <reason>` in the final result.
- Do not add speculative abstractions, duplicate DTOs, unrelated cleanup, or TODO-based endpoint removal.

## Verify

Run, in order:

1. The smallest relevant unit specs.
2. `npm run check-api-contracts`.
3. `node node_modules/@angular/cli/bin/ng.js build --configuration serving --no-progress`.

Do not rerun `npm run update-api` during repair. Finish with the contract delta, changed flow, checks run, and any backend or product blocker.
