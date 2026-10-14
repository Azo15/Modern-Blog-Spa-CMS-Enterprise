# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src

# Restore project dependencies before copying frequently changing source files.
COPY EntityLayer/EntityLayer.csproj EntityLayer/
COPY DataAccesLayer/DataAccesLayer.csproj DataAccesLayer/
COPY BusinessLayer/BusinessLayer.csproj BusinessLayer/
COPY WebUI/WebUI.csproj WebUI/
RUN dotnet restore WebUI/WebUI.csproj --runtime linux-x64

COPY EntityLayer/ EntityLayer/
COPY DataAccesLayer/ DataAccesLayer/
COPY BusinessLayer/ BusinessLayer/
COPY WebUI/ WebUI/
RUN dotnet publish WebUI/WebUI.csproj --configuration Release --runtime linux-x64 \
    --self-contained false --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app

# NativeAssets.Linux.NoDependencies provides libSkiaSharp; Linux font discovery
# uses libfontconfig and curl is used only by the container healthcheck.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libfontconfig1 fonts-dejavu-core curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish/ ./
RUN test -f /app/libSkiaSharp.so \
    && mkdir -p /app/logs /app/wwwroot/uploads /app/data-protection-keys \
    && chown -R app:app /app/logs /app/wwwroot/uploads /app/data-protection-keys
RUN chmod 700 /app/data-protection-keys

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/app/data-protection-keys \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "WebUI.dll"]
