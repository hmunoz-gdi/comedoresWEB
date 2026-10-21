#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
if ! command -v dotnet >/dev/null; then export PATH="/workspace/.dotnet:$PATH"; fi
mkdir -p .local
if ! docker container inspect comedores-postgres >/dev/null 2>&1; then
 docker run -d --name comedores-postgres -e POSTGRES_DB=comedores -e POSTGRES_USER=comedores -e POSTGRES_HOST_AUTH_METHOD=trust -p 127.0.0.1:5432:5432 postgres:17 >/dev/null
else
 docker start comedores-postgres >/dev/null
fi
ready=false
for i in {1..30}; do
 if docker exec comedores-postgres pg_isready -U comedores -d comedores >/dev/null; then ready=true; break; fi
 sleep 1
done
if [[ "$ready" != true ]]; then echo 'PostgreSQL no está listo.' >&2; exit 1; fi
if [[ ! -f .local/demo-password ]]; then
 umask 077
 python3 -c 'import secrets;print("Aa1!"+secrets.token_urlsafe(24))' > .local/demo-password
fi
export Demo__Password="$(cat .local/demo-password)"
export Demo__TerminalIp="${Demo__TerminalIp:-172.16.66.230}"
export ConnectionStrings__Comedores="${ConnectionStrings__Comedores:-Host=127.0.0.1;Database=comedores;Username=comedores}"
export ASPNETCORE_ENVIRONMENT=Development
exec dotnet run --project src/Comedores.Web --no-launch-profile --urls http://127.0.0.1:5080
