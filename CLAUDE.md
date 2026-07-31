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
    ├── AppLogic/                      # IServices/, Services/, DTOs/, Helpers/, ApiClients/
    ├── BusinessLogic/                 # DevartEFCore/, IGenericRepository/, IServices/
    ├── DataAccess/                    # DevartDataAccess/, GenericRepository/, Services/
    └── UnitTesting/                   # xUnit + Moq (Controllers/, Services/, Security/...)
```

## Loadtest

`loadtest/get-endpoints.k6.js` mide el tiempo de respuesta de todos los `[HttpGet]` de `WebApiAdmisiones/Controllers/`. Al agregar, eliminar o cambiar la ruta/params de un `[HttpGet]`, actualiza en el mismo cambio: la lista `ENDPOINT_LABELS` (declara el Trend, obligatorio en k6 antes de usarlo) y la lista `endpoints` dentro de `measureGets` (params dinámicos van en `setup()`, no hardcodeados).
