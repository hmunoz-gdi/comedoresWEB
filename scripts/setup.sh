#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export PATH="/workspace/.dotnet:$PATH"
if ! command -v dotnet >/dev/null || ! dotnet --list-sdks | awk '{print $1}' | rg -q '^10\.'; then
 installer="$(mktemp /tmp/comedores-dotnet-install.XXXXXX)"
 trap 'rm -f "$installer"' EXIT
 curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$installer"
 bash "$installer" --version 10.0.402 --install-dir /workspace/.dotnet
fi
dotnet restore src/Comedores.Web --locked-mode
dotnet restore tools/Comedores.Catalogo --locked-mode
dotnet restore tests/Comedores.Checks --locked-mode
dotnet build src/Comedores.Web --no-restore
