# DesarrolloORT AI Baseline

Usa .github/copilot-instructions.md como baseline compartido del equipo.

## Prioridades

1. Resuelve con el menor contexto y la menor salida util posible.
2. Sigue patrones existentes del repo antes de introducir abstracciones.
3. Trata seguridad como restriccion de generacion.
4. Carga instrucciones, prompts o skills solo cuando aporten contexto real.

## Perfiles

- common: baseline comun y CodeGraph.
- front: reglas Angular/UI, si fueron instaladas.
- back: reglas .NET backend, si fueron instaladas.

## Codegraph

```
api-admisiones/
├── Core/                              # Submódulo privado — bibliotecas compartidas
│   ├── AzureService/                  # Reconocimiento de documentos (Azure AI)
│   ├── DbConnectionContext/
│   ├── LdapService/
│   ├── MailORT/
│   ├── Modules/ (ModBandeja, ModGenericBase)
│   └── Utilities/                     # OperationResult<T>, Constantes, Encriptador
└── WebApiAdmisiones/
    ├── WebApiAdmisiones/              # Entrypoint: Program.cs, Controllers/, Extensions/, Security/
    ├── AppLogic.*/                    # 12 módulos, uno por área (ver docs/README-MODULOS.md)
    ├── BusinessLogic/                 # DevartEFCore/, IGenericRepository/, IServices/
    ├── DataAccess/                    # DevartDataAccess/, GenericRepository/, Services/
    └── UnitTesting/                   # xUnit + Moq (Controllers/, Services/, Security/...)
```

## Documentación

Antes de tocar código de un módulo, lee su `README.md` (está dentro del propio proyecto).

- `docs/README-MODULOS.md` — índice, niveles de dependencia y reglas que no se rompen.
- `docs/GLOSARIO-DOMINIO.md` — español→inglés; qué se traduce y qué no.
- `docs/MATRIZ-TRAZABILIDAD.md` — por qué las cosas están como están; breaking changes con el front.
- `docs/GUIA-ESTILO-CODIGO.md` — cómo escribir código nuevo.

Reglas que cuestan caro si se ignoran:

1. `Core/` (submódulo), `BusinessLogic`, `DataAccess` y `AppLogic.DevartDtos` (generado) **no se tocan**.
2. Los DTOs y las claves de query de `AppLogic.Integrations.*` **quedan en español**: modelan formatos
   ajenos y traducirlos rompe la integración sin dar error de compilación.
3. Ningún tipo de Devart ni de `Core` sale al front: cada módulo mapea a su DTO propio.
4. Un `sed -i` recursivo sobre `*.cs` pisa código generado y literales de string. Excluye
   `AppLogic.DevartDtos/`, `BusinessLogic/`, `DataAccess/` y `Core/`, y revisa después que no hayan
   cambiado mensajes de error ni URLs.

## Loadtest

`loadtest/get-endpoints.k6.js` mide el tiempo de respuesta de todos los `[HttpGet]` de `WebApiAdmisiones/Controllers/`. Al agregar, eliminar o cambiar la ruta/params de un `[HttpGet]`, actualiza en el mismo cambio: la lista `ENDPOINT_LABELS` (declara el Trend, obligatorio en k6 antes de usarlo) y la lista `endpoints` dentro de `measureGets` (params dinámicos van en `setup()`, no hardcodeados).
