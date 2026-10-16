---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-016 — Mensajes al usuario en `.resx` con `IStringLocalizer`

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-18 al 2026-09-22 (piloto en Becas y siete módulos).

## Estado

`accepted` (2026-09-22). La forma de las claves la reemplaza [ADR-017](ADR-017-result-t-formato-unico-error.md).

## Contexto

Los mensajes que el backend manda al front eran literales en español dispersos en ~430 `IsFailed`. Desde la ronda 1 de `AUDITORIA-ERRORES.md`, el front muestra el `Message` del backend tal cual en los 4xx: cada literal es texto de producto visible.

## Problema

¿Dónde vive el texto que ve el usuario para que Análisis Funcional lo revise en un solo lugar, sin perder las garantías del compilador sobre el contrato?

## Opciones consideradas

- **Constantes en C#.** Ya existían a medias (`RegistrationErrorCodes`); no separan el texto del código.
- **`.resx` con texto, código y HTTP.** Descartada: en un `.resx` nadie chequea el contrato.
- **`.resx` solo con el texto.** Elegida.

## Decisión

- Carpeta `Resources/` por módulo con un marcador `<Modulo>Resources.cs` y su `.resx`. El servicio recibe `IStringLocalizer<T>`.
- El `.resx` guarda solo el texto. Código y status siguen en C#.
- `AddLocalization()` **sin** `ResourcesPath`: con `"Resources"` el prefijo queda duplicado y nada resuelve, en silencio. Sin `UseRequestLocalization`: una sola cultura.
- Clases `static` que producen errores reciben el localizer por parámetro; no se convierten en servicios.
- Un test por módulo recorre todas las claves y falla si alguna no resuelve, con el localizer real. Se verificó borrando una clave.
- Los mensajes de éxito también pasan al `.resx`.

## Consecuencias

- `IStringLocalizer` devuelve la clave cuando el recurso falta, sin error: sin el test de cobertura, el usuario vería un código en pantalla.
- Quedaron a la vista los textos duplicados y los códigos con dos textos, que se resolvieron después con [ADR-017](ADR-017-result-t-formato-unico-error.md).
- Los textos de `Core/` y los de validación de documento de `Core/Utilities` quedaron fuera.
- Ocho `.resx` y 313 claves al cerrar el refactor.

## Referencias

- `api-admisiones/docs/AUDITORIA-ERRORES.md`
- [ADR-006](ADR-006-modularizacion-applogic-ingles.md) — módulos de `AppLogic`
