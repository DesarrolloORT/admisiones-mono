---
name: i18n-naming
description: "Convencion de idioma: rutas y texto visible en español, codigo e identificadores en ingles."
applyTo: "**/*.ts, **/*.html, **/*.routes.ts, **/*.route.ts"
---

<!-- ai-toolkit:toolkit profile=base path=.github/instructions/toolkit/i18n-naming.instructions.md -->

# Idioma: Código vs. Texto Visible

## Regla general

| Ámbito                                                                                           | Idioma                              | Ejemplos                                                                               |
| ------------------------------------------------------------------------------------------------ | ----------------------------------- | -------------------------------------------------------------------------------------- |
| Rutas (path en el navegador)                                                                     | Español                             | `iniciar-sesion`, `registro`, `recuperar-acceso`, `crear-password`, `cambiar-password` |
| Texto en pantalla (labels, títulos, mensajes, placeholders)                                      | Español                             | `'Iniciar sesión'`, `'Crear cuenta'`                                                   |
| Identificadores de código (clases, funciones, variables, signals, selectors, nombres de archivo) | Inglés                              | `LoginFacade`, `auth.routes.ts`, `home-profile-menu`                                   |
| Comentarios técnicos y JSDoc                                                                     | Español preferido, inglés aceptable | —                                                                                      |

## Rutas

- Los segmentos de URL visibles al usuario van en español, kebab-case y sin tildes: `iniciar-sesion`, `registro`, `recuperar-acceso`, `crear-password`, `cambiar-password`, `inicio`.
- Los segmentos puramente técnicos (IDs, tokens, query params internos) pueden ser inglés.
- Si una ruta existente está en inglés (`login`, `register`, `recover-access`), migrarla a español cuando se toque el archivo.

## HTML / Templates

- Todo texto renderizado al usuario (botones, headings, labels, errores, hints, tooltips) va en español.
- Atributos técnicos (`class`, `id`, `formControlName`, `role`) van en inglés.

## No aplica a

- Nombres de paquetes, imports, librerías externas.
- Valores de enums o constantes que representan claves técnicas del backend.
