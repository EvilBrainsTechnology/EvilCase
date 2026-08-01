# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /source

# Restore from the project files alone, so the layer survives every change that touches neither a
# csproj nor a package version. A new project needs a line here or the restore below fails.
COPY src/global.json src/Directory.Build.props src/Directory.Packages.props ./src/
COPY src/EvilCase.Host/EvilCase.Host.csproj ./src/EvilCase.Host/
COPY src/Api/EvilCase.Api/EvilCase.Api.csproj ./src/Api/EvilCase.Api/
COPY src/Api/EvilCase.Api.Client/EvilCase.Api.Client.csproj ./src/Api/EvilCase.Api.Client/
COPY src/Api/EvilCase.Api.Contract/EvilCase.Api.Contract.csproj ./src/Api/EvilCase.Api.Contract/
COPY src/App/EvilCase.App/EvilCase.App.csproj ./src/App/EvilCase.App/
COPY src/Common/EvilCase.Auth/EvilCase.Auth.csproj ./src/Common/EvilCase.Auth/
COPY src/Data/EvilCase.Data/EvilCase.Data.csproj ./src/Data/EvilCase.Data/
COPY src/Data/EvilCase.Data.Migrations/EvilCase.Data.Migrations.csproj ./src/Data/EvilCase.Data.Migrations/
COPY src/Utils/EvilBrains.Analyzers/EvilBrains.Analyzers.csproj ./src/Utils/EvilBrains.Analyzers/
COPY src/Utils/EvilBrains.ApiClient/EvilBrains.ApiClient.csproj ./src/Utils/EvilBrains.ApiClient/
COPY src/Utils/EvilBrains.ApiClient.Generator/EvilBrains.ApiClient.Generator.csproj ./src/Utils/EvilBrains.ApiClient.Generator/
COPY src/Utils/EvilBrains.Collections/EvilBrains.Collections.csproj ./src/Utils/EvilBrains.Collections/
COPY src/Utils/EvilBrains.Cryptography/EvilBrains.Cryptography.csproj ./src/Utils/EvilBrains.Cryptography/
COPY src/Utils/EvilBrains.EntityFramework/EvilBrains.EntityFramework.csproj ./src/Utils/EvilBrains.EntityFramework/
COPY src/Utils/EvilBrains.Logging/EvilBrains.Logging.csproj ./src/Utils/EvilBrains.Logging/
COPY src/Utils/EvilBrains.Logging.AspNetCore/EvilBrains.Logging.AspNetCore.csproj ./src/Utils/EvilBrains.Logging.AspNetCore/
COPY src/Utils/EvilBrains.Logging.Contract/EvilBrains.Logging.Contract.csproj ./src/Utils/EvilBrains.Logging.Contract/
COPY src/Utils/EvilBrains.Logging.WebAssembly/EvilBrains.Logging.WebAssembly.csproj ./src/Utils/EvilBrains.Logging.WebAssembly/
COPY src/Utils/EvilBrains.Secrets.Infisical/EvilBrains.Secrets.Infisical.csproj ./src/Utils/EvilBrains.Secrets.Infisical/

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
