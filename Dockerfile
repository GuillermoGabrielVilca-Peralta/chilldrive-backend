# ── Etapa 1: compilar ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias primero (aprovecha cache de capas Docker)
COPY ChillDrive_Backend/ChillDrive_Backend.csproj ChillDrive_Backend/
RUN dotnet restore ChillDrive_Backend/ChillDrive_Backend.csproj

# Copiar el resto del código y publicar en Release
COPY . .
RUN dotnet publish ChillDrive_Backend/ChillDrive_Backend.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Etapa 2: imagen de runtime (sin SDK, más ligera) ─────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Puerto interno de la API (HTTP). TLS lo termina el reverse proxy, no la app.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ChillDrive_Backend.dll"]
