# syntax=docker/dockerfile:1
# =============================================================================
# Imagen multi-stage de la API .NET 8. Los secretos (Jwt__Key, ConnectionStrings__
# Default, Seed__AdminPassword, ...) se pasan en tiempo de ejecución por variables
# de entorno, NUNCA se hornean en la imagen. Ver DESPLIEGUE.md.
# =============================================================================

# ---- Stage 1: build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para cachear el restore (no se invalida si cambia
# el código pero no las dependencias).
COPY GestionCelulares.Api/GestionCelulares.Api.csproj GestionCelulares.Api/
COPY GestionCelulares.Application/GestionCelulares.Application.csproj GestionCelulares.Application/
COPY GestionCelulares.Infrastructure/GestionCelulares.Infrastructure.csproj GestionCelulares.Infrastructure/
COPY GestionCelulares.Domain/GestionCelulares.Domain.csproj GestionCelulares.Domain/
RUN dotnet restore GestionCelulares.Api/GestionCelulares.Api.csproj

# Copiar el resto del código y publicar
COPY GestionCelulares.Api/ GestionCelulares.Api/
COPY GestionCelulares.Application/ GestionCelulares.Application/
COPY GestionCelulares.Infrastructure/ GestionCelulares.Infrastructure/
COPY GestionCelulares.Domain/ GestionCelulares.Domain/
RUN dotnet publish GestionCelulares.Api/GestionCelulares.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Stage 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Carpeta de logs de Serilog escribible por el usuario no-root de la imagen.
RUN mkdir -p logs && chown -R app:app logs
USER app

# La imagen base de .NET 8 escucha en 8080 por defecto (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionCelulares.Api.dll"]
