# Releases

## Estado

TODO: documentar criterio de versionado, responsables y enlace al ultimo release
confirmado.

## Antes de liberar a produccion

Hay exposicion que es aceptable en desa y no lo es en produccion. Hoy la API
publica sin autenticacion `/swagger/v1/swagger.json` y
`/contracts/*.contract.json` (verificado en desa: los endpoints de datos si
responden `401`, asi que no hay acceso indebido a informacion de personas).

El problema no es la forma del JSON —el frontend la necesita y se ve igual en el
tráfico del navegador— sino que los contratos documentan **implementacion
interna**: vistas y tablas Oracle (`VD_PRUEBAS_DISPONIBLES`, `T_INSCRIPTO_PRUEBA`,
`CREADOR.T_PERSONA.TELEFONO1`), nombres de columnas y el comportamiento de los
queries, endpoints y sistemas internos (`ORTSecure/Becas/AltaPostulacionBeca`,
SGI, api-fdp) y un bloque `limitacionesConocidas` que enumera casos borde donde el
sistema no se comporta como deberia. Nada de eso lo consume el frontend, y para
alguien que busca por donde entrar es reconocimiento gratis: abarata intentos de
inyeccion o enumeracion y dibuja la topologia interna.

Antes del pasaje a produccion, con backend:

1. **Cerrar la documentacion de la API en prod**: deshabilitar `/swagger` y
   `/contracts`, o dejarlos detras de autenticacion o allowlist de IP. El
   snapshot que usa el codegen vive versionado en `.api-spec/`, asi que cerrarlos
   no afecta al build.
2. **Partir el contrato en dos audiencias**: publico lo que el front necesita
   (campos, nulabilidad, semantica de valores, codigos de error, reglas de
   dominio observables); interno lo demas (vistas, tablas, columnas, queries,
   endpoints internos y `limitacionesConocidas`).
3. **Confirmar si desa queda accesible desde internet** o solo desde la red de
   ORT. Si es interna, el riesgo mientras tanto es bajo.

Esto no depende de que alguien se acuerde: `npm run build -- prod` y
`npm run build -- preprod` corren `npm run check:prod-exposure`, que **falla**
mientras el snapshot de `.api-spec/contracts/` siga trayendo identificadores
internos o mientras no se confirme el punto 1. Los dos escapes son explicitos y
quedan en el log del build:

| Variable                                | Significado                                                  |
| --------------------------------------- | ------------------------------------------------------------ |
| `PROD_API_DOCS_CLOSED=1`                | ya se cerraron `/swagger` y `/contracts` en ese ambiente     |
| `ALLOW_PUBLIC_API_CONTRACT_INTERNALS=1` | se acepta el riesgo de publicar los identificadores internos |

En desa y local el check no aplica y no dice nada.

## Referencias actuales

- [Workflow](./WORKFLOW.md)
- [CHANGELOG](https://github.com/DesarrolloORT/admisiones/blob/v1.0.0/main/CHANGELOG.md)
