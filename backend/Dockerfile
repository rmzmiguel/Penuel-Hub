# Imagen del backend de Penuel.
#
# Render no tiene entorno nativo de .NET, así que el despliegue va por Docker. El
# contexto de construcción es esta carpeta (backend/), no la raíz del repositorio:
# el frontend no pinta nada aquí.

# ── Construcción ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primero SOLO los archivos de proyecto. Docker cachea capas por contenido, así que
# mientras no cambie una dependencia, el `restore` —que es lo lento— se reutiliza
# entre despliegues aunque haya cambiado todo el código.
COPY global.json ./
COPY Penuel.Domain/Penuel.Domain.csproj                 Penuel.Domain/
COPY Penuel.Application/Penuel.Application.csproj       Penuel.Application/
COPY Penuel.Infrastructure/Penuel.Infrastructure.csproj Penuel.Infrastructure/
COPY Penuel.WebApi/Penuel.WebApi.csproj                 Penuel.WebApi/

# Solo la WebApi y lo que arrastra. Restaurar la solución entera traería además las
# dependencias de las pruebas, que no tienen nada que hacer en una imagen de producción.
RUN dotnet restore Penuel.WebApi/Penuel.WebApi.csproj

COPY Penuel.Domain/         Penuel.Domain/
COPY Penuel.Application/    Penuel.Application/
COPY Penuel.Infrastructure/ Penuel.Infrastructure/
COPY Penuel.WebApi/         Penuel.WebApi/

RUN dotnet publish Penuel.WebApi/Penuel.WebApi.csproj \
    -c Release -o /app --no-restore

# ── Ejecución ───────────────────────────────────────────────────────────────
# La imagen `aspnet` trae solo el motor, sin el SDK: alrededor de un tercio del tamaño.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Usuario sin privilegios. La imagen lo trae creado; solo hay que pedirlo.
USER $APP_UID

# Informativo: el puerto real lo dice la variable PORT, que Program.cs traduce a la
# URL de escucha. Render la inyecta sola.
EXPOSE 8080

ENTRYPOINT ["dotnet", "Penuel.WebApi.dll"]
