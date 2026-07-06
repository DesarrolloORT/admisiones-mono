---
slug: /flujos/inscripciones/pagos
title: Flujo de pagos de inscripción
description: Decisiones por método de pago y estados esperados del paso de pago.
---

# Flow de pagos de inscripción

> Tipo: reference

Fuente de verdad frontend: `src/app/features/inscriptions/**`.

El pago parte de una inscripción con pago pendiente y llama a
`POST /Inscripciones/Pagar`. El front envía tipos propios de la feature al
adapter; solo `endpoints/` conoce el contrato generado.

## Contrato del paso

Payload feature:

```json
{
  "idInscripcion": 1072704,
  "metodoPago": "cuenta-bancaria",
  "idBancoSistarbanc": "brou"
}
```

Mapping adapter:

| UI                | API               | `idBancoSistarbanc` |
| ----------------- | ----------------- | ------------------- |
| `cuenta-personal` | `CUENTA_PERSONAL` | `null`              |
| `abitab`          | `ABITAB`          | `null`              |
| `paganza`         | `PAGANZA`         | `null`              |
| `banred`          | `BANRED`          | `null`              |
| `geopay`          | `GEOPAY`          | `null`              |
| `cuenta-bancaria` | `SISTARBANC`      | código del banco    |

`tarjeta-credito` no queda como método activo hasta que el backend confirme un
`tipoPago` propio o su mapeo dentro de Sistarbanc.

## Flujo por método

```mermaid
flowchart TD
  A[Inscripción con pago pendiente] --> B[POST /Inscripciones/Pagar]
  B --> C{Método}
  C -->|CUENTA_PERSONAL| D{Backend confirma pago}
  D -->|OK| E[Inscripción confirmada]
  D -->|Error| F[Permanece en pago con error]
  C -->|ABITAB / PAGANZA| G[Reserva / pago pendiente externo]
  C -->|BANRED / GEOPAY / SISTARBANC| H{Backend devuelve urlPago}
  H -->|Sí| I[Front redirige a pasarela]
  H -->|No| J[Pago pendiente externo]
  I --> J
```

## Estados frontend

- `processing`: solo mientras responde `/Inscripciones/Pagar`.
- `inscription-confirmada`: pago confirmado por backend.
- `reserva`: Abitab o Paganza quedan con instrucciones de pago externo.
- `pago-pendiente-externo`: Banred, Geopay o Sistarbanc ya salieron a pasarela o
  quedaron esperando definición de acreditación.
- `editing`: errores de validación o error de backend; el usuario puede corregir
  y reintentar.

## Brecha pendiente

Para Banred, Geopay y Sistarbanc el front redirige fuera del proyecto. Hoy este
proyecto no recibe callback ni consulta de estado para saber si el usuario pagó.
Por eso el estado default al volver o no poder confirmar es
`pago-pendiente-externo`, nunca un loader infinito.

Cuando backend defina callback, polling o endpoint de consulta, ese mecanismo debe
actualizar este estado a `inscription-confirmada` o mostrar error final.
