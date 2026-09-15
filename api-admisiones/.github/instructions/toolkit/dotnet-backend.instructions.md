---
name: dotnet-backend
description: "Backend .NET con bajo consumo de contexto para soluciones grandes."
applyTo: "**/*.cs, **/*.csproj, **/*.sln, **/*.json"
---

<!-- ai-toolkit:toolkit profile=dotnet path=.github/instructions/toolkit/dotnet-backend.instructions.md -->

# .NET Backend

- Empieza por el `.sln`, los `.csproj`, `Program.cs` y los archivos tocados por el cambio. No cargues soluciones completas si no hace falta.
- En soluciones grandes, evita recorrer `Core/`, `bin/`, `obj/`, migraciones, EF/Devart generado o modelos masivos salvo que el cambio lo requiera.
- Respeta la separacion habitual: `WebApi*` para entrada HTTP, `AppLogic` para casos de uso/orquestacion, `BusinessLogic` para reglas, `DataAccess` para persistencia y `UnitTesting` para pruebas.
- Cuando existan modulos compartidos bajo `Core/Modules`, usa sus contratos publicos y patrones vigentes antes de duplicar logica.
- Mantiene `net10.0`, nullable enabled e implicit usings si ya estan presentes.
- Configura servicios y pipeline mediante extension methods existentes; no infles `Program.cs`.
- Para APIs, preserva JWT, Swagger/OpenAPI, CORS, Kestrel security, Serilog, OpenTelemetry y Prometheus cuando el repo ya los usa.
- Valida entrada, autorizacion y manejo de errores en endpoints, handlers y servicios. No registres secretos ni datos sensibles en logs.
- Agrega o ajusta tests en `UnitTesting` para reglas de negocio, mapeos y casos de borde tocados por el cambio.
