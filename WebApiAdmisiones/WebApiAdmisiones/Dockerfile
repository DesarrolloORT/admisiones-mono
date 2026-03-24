# syntax=docker/dockerfile:1.4

####### Etapa de build ########
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Directorio de trabajo genérico
WORKDIR /src

# 1) Copio únicamente lo necesario:
#    - toda la carpeta Empleos (donde está WebApiEmpleos.sln y el sub-proyecto WebApiEmpleos)
#    - toda la carpeta Core (tus librerías compartidas)
COPY Core/DbConnectionContext        Core/DbConnectionContext
COPY Core/LdapService        Core/LdapService
COPY Core/MailORT                   Core/MailORT
COPY Core/Modules/ModBandeja/ModAppLogic        Core/Modules/ModBandeja/ModAppLogic
COPY Core/Modules/ModBandeja/ModBusinessLogic   Core/Modules/ModBandeja/ModBusinessLogic
COPY Core/Modules/ModBandeja/ModDataAccess      Core/Modules/ModBandeja/ModDataAccess
COPY Core/Utilities                Core/Utilities

COPY WebApiAdmisiones/AppLogic      WebApiAdmisiones/AppLogic
COPY WebApiAdmisiones/DataAccess    WebApiAdmisiones/DataAccess
COPY WebApiAdmisiones/WebApiAdmisiones     WebApiAdmisiones/WebApiAdmisiones
COPY WebApiAdmisiones/WebApiAdmisiones.sln WebApiAdmisiones/WebApiAdmisiones.sln

COPY WebApiAdmisiones/BusinessLogic WebApiAdmisiones/BusinessLogic
COPY WebApiAdmisiones/UnitTesting WebApiAdmisiones/UnitTesting
COPY Core/Modules/ModGenericBase/ModAppLogic Core/Modules/ModGenericBase/ModAppLogic
COPY Core/Modules/ModGenericBase/ModBusinessLogic Core/Modules/ModGenericBase/ModBusinessLogic
COPY Core/Modules/ModGenericBase/ModDataAccess Core/Modules/ModGenericBase/ModDataAccess


# 2) Restore de la solución que vive en Empleos
WORKDIR /src/Empleos
RUN dotnet restore WebApiEmpleos.sln

# 3) Publicar directamente el proyecto de la API
WORKDIR /src/WebApiAdmisiones/WebApiAdmisiones

RUN dotnet publish \
      WebApiAdmisiones.csproj \
      -c "$BUILD_CONFIGURATION" \
      -o /app/publish \ 
      --no-restore

####### Etapa final (runtime) ########
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# 1) Crear un usuario no privilegiado
RUN groupadd -g50000 appdocker && useradd -m -u50000 -gappdocker -s/bin/bash appdocker

WORKDIR /app
RUN chown -R appdocker:appdocker /app

# 2) Exponer puertos
EXPOSE 8080
EXPOSE 8081

# 3) Copio solo el output del publish
COPY --from=build /app/publish ./

# 4) Cambiar a usuario no privilegiado
USER appdocker

# 5) Punto de entrada
ENTRYPOINT ["dotnet", "WebApiAdmisiones.dll"]
