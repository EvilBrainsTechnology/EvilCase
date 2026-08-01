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

# The SDK is resolved from the working directory, not from the project, so this has to be the directory
# holding global.json — otherwise the pin is silently ignored and the image builds on whatever SDK the
# base image happens to carry.
WORKDIR /source/src

COPY --from=projects /projects ./
RUN dotnet restore EvilCase.Host/EvilCase.Host.csproj

COPY src/ ./

ARG VERSION=0.0.0
ARG SOURCE_REVISION=
RUN dotnet publish EvilCase.Host/EvilCase.Host.csproj \
        --configuration Release \
        --no-restore \
        --output /app \
        -p:Version=${VERSION} \
        -p:SourceRevisionId=${SOURCE_REVISION}

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final

# curl: the runtime image ships no HTTP client and HEALTHCHECK runs inside the container. ICU: the
# Alpine image turns globalization off and carries no ICU, which silently makes every culture aware
# comparison ordinal — wrong for Czech data.
RUN apk add --no-cache curl icu-data-full icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY --from=build /app .

USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "EvilBrains.EvilCase.Host.dll"]
