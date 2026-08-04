---
name: secure-code
description: "Baseline secure-by-default para codigo nuevo o modificado."
applyTo: "**/*.ts, **/*.tsx, **/*.js, **/*.jsx, **/*.mjs, **/*.cjs, **/*.cts, **/*.mts, **/*.py, **/*.rb, **/*.go, **/*.rs, **/*.java, **/*.cs, **/*.php"
---

<!-- ai-toolkit:toolkit profile=base path=.github/instructions/toolkit/secure-code.instructions.md -->

# Secure Code

- Trata la seguridad como una restriccion de generacion, no solo de review posterior.
- Valida entradas, codifica salidas y evita interpolaciones inseguras en HTML, SQL, shell, templates o URLs.
- No hardcodees secretos, tokens, credenciales ni datos sensibles.
- Prefiere minimo privilegio, defaults cerrados y checks explicitos de autorizacion en cambios sensibles.
- Sigue secure-by-default con OWASP ASVS L2 como piso practico.
- Minimizar tokens, pasos o longitud de respuesta nunca justifica relajar validacion, controles o mitigaciones.
- Si una implementacion parece insegura, no la escribas; explica el riesgo y propone la variante segura.
