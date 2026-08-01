# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

# Everything the restore reads and nothing else. This stage reruns on every source change, but as
# long as no project file changed its output is byte for byte the same, and the cache key of the
# COPY below is computed from that output — which is what keeps the restore cached.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS projects
COPY src/ /projects/
RUN find /projects -type f \
        ! -name '*.csproj' \
        ! -name 'Directory.*.props' \
        ! -name 'global.json' \
        -delete \
    && find /projects -depth -type d -empty -delete

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /source

COPY --from=projects /projects ./src/
RUN dotnet restore src/EvilCase.Host/EvilCase.Host.csproj

COPY src/ ./src/

ARG VERSION=0.0.0
ARG SOURCE_REVISION=
RUN dotnet publish src/EvilCase.Host/EvilCase.Host.csproj \
        --configuration Release \
        --no-restore \
        --output /app \
        -p:Version=${VERSION} \
        -p:SourceRevisionId=${SOURCE_REVISION}

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final

# The runtime image ships no HTTP client, and both HEALTHCHECK and compose's depends_on run inside
# the container.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "EvilBrains.EvilCase.Host.dll"]
