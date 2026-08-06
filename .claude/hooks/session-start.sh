#!/bin/bash
# Prepares a Claude Code on the web container for building, testing and running EvilCase:
# .NET SDK, PostgreSQL, EvilCase.Host/.env, a dev certificate and a warm restore.
# Idempotent — every step is skipped when it is already done.
set -euo pipefail

# Local machines have their own toolchain; this only fixes up the remote container.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
    exit 0
fi

readonly REPO="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
readonly DOTNET_ROOT_DIR=/usr/share/dotnet
readonly SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0

log() { echo "[session-start] $*"; }

# --- .NET SDK -------------------------------------------------------------------------------
# The egress policy blocks builds.dotnet.microsoft.com, so dotnet-install.sh cannot reach the
# SDK. mcr.microsoft.com is allowed, so the SDK is copied out of the official image instead and
# ends up on the host filesystem — Docker is not involved in anything after this block.
install_dotnet() {
    if ! docker info >/dev/null 2>&1; then
        log "starting docker daemon"
        nohup dockerd >/var/log/dockerd.log 2>&1 &
        for _ in $(seq 1 30); do
            docker info >/dev/null 2>&1 && break
            sleep 1
        done
        docker info >/dev/null 2>&1 || { log "docker daemon did not start"; return 1; }
    fi

    log "pulling $SDK_IMAGE"
    docker pull --quiet "$SDK_IMAGE" >/dev/null

    local container
    container=$(docker create "$SDK_IMAGE")
    mkdir -p "$DOTNET_ROOT_DIR"
    docker cp "$container:$DOTNET_ROOT_DIR/." "$DOTNET_ROOT_DIR/"
    docker rm "$container" >/dev/null
    ln -sf "$DOTNET_ROOT_DIR/dotnet" /usr/local/bin/dotnet
}

if dotnet --version >/dev/null 2>&1; then
    log ".NET SDK $(dotnet --version) already installed"
else
    install_dotnet
    log ".NET SDK $(dotnet --version) installed"
fi

# run-script (the `r` tool) targets net8.0 and the SDK image carries only the 10.0 runtime.
export DOTNET_ROLL_FORWARD=LatestMajor
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export PATH="$PATH:/root/.dotnet/tools"

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
        echo 'export DOTNET_ROLL_FORWARD=LatestMajor'
        echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
        echo 'export DOTNET_NOLOGO=1'
        echo 'export PATH="$PATH:/root/.dotnet/tools"'
    } >> "$CLAUDE_ENV_FILE"
fi

# global.json sets scriptShell to pwsh, so every `dotnet r` script needs it. The SDK image carries
# one but the extracted layout does not, and the NuGet package is reachable where the installers
# are not.
if ! command -v pwsh >/dev/null 2>&1; then
    log "installing PowerShell"
    dotnet tool install --global PowerShell >/dev/null
fi

# --- GitHub CLI -----------------------------------------------------------------------------
# .claude/rules/github.md reads and writes GitHub through gh, which the container does not ship.
# It authenticates from $GH_TOKEN, so no login step. There is no fallback: a worktree-isolated
# subagent cannot run a command that expands $GH_TOKEN, so a session without gh cannot work.
install_gh() {
    local version=2.97.0
    local archive="gh_${version}_linux_amd64"
    local temp
    temp=$(mktemp -d) || return 1

    curl -fsSL --max-time 120 \
        "https://github.com/cli/cli/releases/download/v${version}/${archive}.tar.gz" \
        -o "$temp/gh.tar.gz" || { rm -rf "$temp"; return 1; }
    tar -xzf "$temp/gh.tar.gz" -C "$temp" || { rm -rf "$temp"; return 1; }
    install -m 755 "$temp/$archive/bin/gh" /usr/local/bin/gh || { rm -rf "$temp"; return 1; }
    rm -rf "$temp"
}

if command -v gh >/dev/null 2>&1; then
    log "gh $(gh --version | head -n 1 | cut -d' ' -f3) already installed"
else
    log "installing gh"
    install_gh
    log "gh $(gh --version | head -n 1 | cut -d' ' -f3) installed"
fi

# --- PostgreSQL -----------------------------------------------------------------------------
# The image ships PostgreSQL 16, so deploy/docker-compose.dev.yml (PostgreSQL 18) is not needed.
# Credentials and database name still match the connection string in .env.example.
if pg_isready -h 127.0.0.1 -q 2>/dev/null; then
    log "PostgreSQL already running"
else
    log "starting PostgreSQL"
    service postgresql start >/dev/null
    for _ in $(seq 1 30); do
        pg_isready -h 127.0.0.1 -q 2>/dev/null && break
        sleep 1
    done
fi

su postgres -c "psql -qc \"ALTER USER postgres PASSWORD 'postgres';\"" >/dev/null
su postgres -c "psql -tAc \"SELECT 1 FROM pg_database WHERE datname='evilcase'\"" | grep -q 1 \
    || su postgres -c "createdb evilcase"

# --- Secrets --------------------------------------------------------------------------------
# .env is gitignored, so a fresh container has none. These values are throwaway and local only;
# every real environment passes the same keys in as environment variables.
readonly ENV_FILE="$REPO/src/EvilCase.Host/.env"
if [ -f "$ENV_FILE" ]; then
    log ".env already present"
else
    log "writing .env with development credentials"
    cat > "$ENV_FILE" <<'ENV'
EvilBrains__EvilCase__ConnectionString=Host=localhost;Port=5432;Database=evilcase;Username=postgres;Password=postgres

EvilBrains__EvilCase__Auth__Jwt__Key=dev-only-local-signing-key-not-a-secret-0123456789

EvilBrains__EvilCase__Auth__Seed__Email=admin@evilcase.local
EvilBrains__EvilCase__Auth__Seed__Password=DevPassword123!

EvilBrains__EvilCase__Logging__Seq__ApiKey=
ENV
fi

# --- Commit identity ------------------------------------------------------------------------
# The container's global config authors commits as the user account `claude`. Pinning the
# repository's identity to the app leaves one commit author on every branch and on master alike.
# Comments and pull requests are a separate matter: .claude/rules/github.md is what marks them
# as the agent's.
log "authoring commits as claude[bot]"
git -C "$REPO" config user.name 'claude[bot]'
git -C "$REPO" config user.email '209825114+claude[bot]@users.noreply.github.com'

# --- Restore --------------------------------------------------------------------------------
cd "$REPO/src"
log "restoring tools and packages"
dotnet tool restore >/dev/null
dotnet restore >/dev/null
dotnet dev-certs https >/dev/null

log "ready — run 'dotnet r build', 'dotnet r test' or 'dotnet r run' from src/"
