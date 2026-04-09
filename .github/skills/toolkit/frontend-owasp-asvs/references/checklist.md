<!-- ai-toolkit:toolkit profile=security path=.github/skills/toolkit/frontend-owasp-asvs/references/checklist.md -->

# Checklist base

- autenticacion y expiracion de sesion
- autorizacion a nivel de ruta, vista y accion sensible
- almacenamiento de tokens o secretos en cliente
- validacion y sanitizacion de entrada antes de render o enviar
- errores que filtran informacion sensible
- protecciones frente a CSRF, open redirects o navegacion insegura
- consumo de APIs con headers, scopes o permisos inconsistentes
- telemetria o logs con datos sensibles
