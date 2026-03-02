# Template .NET 9 – WebApi (con submódulos)

## Descripción
Plantilla lista para crear APIs con .NET 9 y Visual Studio 2022, con estructura base, submódulos y scripts de renombrado para arrancar proyectos nuevos.

# Arquitectura Web API en .NET 9 con Entity Framework (EF)
🚀  Este repositorio contiene un template base de una arquitectura .NET 9, API RESTful diseñada para proporcionar servicios de backend escalables y de alto rendimiento. Integra Entity Framework Core para gestionar de forma eficiente las operaciones con la base de datos, implementa una arquitectura limpia y modular, y está preparada para entornos cloud y contenedores.

## Características Tecnológicas

- 🚀 **.NET 9**: Plataforma moderna con mejoras de rendimiento y nuevas funcionalidades.
- 🛠️ **Entity Framework Core**: ORM robusto para modelado de datos y migraciones automáticas.
- 🌐 **API RESTful**: Endpoints bien definidos y documentados con Swagger.
- 🧩 **Arquitectura Limpia**: Separación de capas (Core, AppLogic, BusinessLogic, DataAccess, WebApi) para código mantenible.
- 🔒 **Gestión de Secretos**: Uso de User Secrets y variables de entorno para proteger credenciales.
- 🐳 **Docker & Contenedores**: Despliegue rápido y consistente en cualquier entorno.
- 🧪 **Pruebas Unitarias y de Integración**: xUnit, Moq y Coverlet para asegurar calidad y cobertura de código.

## Arquitectura de la Solución

La solución está organizada en capas y proyectos independientes:

```graphql
├─ Core
│   ├─ DbConnectionContext    (Contexto de conexión a base de datos)
│   ├─ MailORT               (Servicio de envío de correo)
│   ├─ Modules               (Módulos genéricos y específicos, p.ej., ModBandeja, ModGenericBase)
│   └─ Utilities             (Clases auxiliares y validadores)
│
├─ AppLogic
│   └─ Servicios de aplicación (Casos de uso) y Helpers (DTOs y converters)
│
├─ BusinessLogic
│   └─ Lógica de dominio e interfaces de repositorios
│
├─ DataAccess
│   └─ EF Core DbContext y repositorios de datos
│
├─ WebApi
│   └─ API REST (Controllers, Program.cs, configuración)
│
└─ UnitTesting
    └─ Pruebas de integración y unitarias (xUnit, Moq)
```

La solución principal se encuentra en el archivo `WebApi.sln`, que referencia todos los proyectos anteriores.

# PASOS A SEGUIR PARA TU NUEVO PROYECTO:
##  1) 🧪 Crear un repo nuevo desde el template
Desde la Web (recomendado)
 - Entrá al repo template en GitHub.
 - Click Use this template → Create a new repository.
 - Elegí nombre/visibilidad y crealo.

##  2) ⬇️ Clonar el nuevo repo (con submódulos)
 - git clone --recurse-submodules git@github.com:ORG/MI-NUEVO-REPO.git
    
 - Si al clonarse no cargo los submódulos:
   cd MI-NUEVO-REPO
   git submodule update --init --recursive
   *Si los submódulos son privados, el usuario debe tener permisos o usar un PAT.

##  3) ✏️ Renombrar el proyecto (scripts)
 - Una vez que tienes descargado tu proyecto en tu pc 
   *Cerrá Visual Studio/VS Code antes de ejecutar (evitás archivos bloqueados).

## Windows (doble clic)
- Abrí scripts/rename.cmd (doble clic).
- El script rename.ps1 te pedirá:
- Nombre del PROYECTO → renombra la carpeta raíz (NewApi) y la solución (.sln).
- Nombre del proyecto WEB API → renombra la carpeta interna (WebApiTemplate) y el .csproj. (por defecto: WebApi{Proyecto})
- Namespace NUEVO → reemplaza WebApiFDP en todo el código. (por defecto: {Proyecto})

¿Qué cambia exactamente?
 - NewApi/ → MiProyecto/
 - WebApiTemplate.sln → MiProyecto.sln
 - WebApiTemplate/ → WebApiMiProyecto/
 - WebApiTemplate.csproj → WebApiMiProyecto.csproj
 - namespace WebApiFDP → namespace Mi.Empresa.MiProyecto (o el que elijas)
 - Actualiza referencias en .sln, .csproj, .gitmodules, Dockerfile, YAMLs, etc.

🛠️ Restaurar y compilar
   - dotnet restore "MiProyecto/MiProyecto.sln"
   - dotnet build   "MiProyecto/MiProyecto.sln" -c Debug
   - Si preferís, cd MiProyecto y corré dotnet restore && dotnet build.

##  4) ✏️ Paso final
- Si ya estas en tu propio repositorio y todo funciona correctamente. 
- Ahora ya puedes eliminar los archivos rename.cmd y rename.ps1
- También deberías editar tu readme.md y describir tu nuevo proyecto.
