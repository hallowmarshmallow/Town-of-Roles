#!/usr/bin/env bash
#
# One-command build for the merged monorepo.
#
# Layout:
#   Atomic/        -> Atomic.dll             (RPC/lobby framework, loads first)
#   MarshAPI/       -> MarshAPI.dll                      (modding SDK: roles, abilities, kills)
#   ./              -> TownOfRoles.dll                   (the role mod itself)
#   UpdaterPatcher/ -> TownOfRoles.Updater.Patcher.dll   (preloader, applies staged updates)
#
# Output: the plugin DLLs staged in dist/plugins/, ready to drop into
# <game>/BepInEx/plugins/ (the patcher goes into BepInEx/patchers/).
#
# Usage:  sh build.sh [Release|Debug]

set -eu

CONFIG="${1:-Release}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

# Locate dotnet (plain PATH first, then the local toolchain install).
DOTNET="${DOTNET:-}"
if [ -z "$DOTNET" ]; then
  if command -v dotnet >/dev/null 2>&1; then
    DOTNET="dotnet"
  elif [ -x /tmp/dotnet/dotnet ]; then
    DOTNET=/tmp/dotnet/dotnet
    export DOTNET_ROOT=/tmp/dotnet
  elif [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET="$HOME/.dotnet/dotnet"
    export DOTNET_ROOT="$HOME/.dotnet"
  else
    echo "error: no dotnet SDK found (set DOTNET=/path/to/dotnet)" >&2
    exit 1
  fi
fi
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

echo "==> [1/4] ../Atomic/Atomic.csproj"
if [ -d "$ROOT/../Atomic" ]; then
  "$DOTNET" build "$ROOT/../Atomic/Atomic.csproj" -c "$CONFIG"
fi

echo "==> [2/4] TownOfRoles.csproj"
"$DOTNET" build TownOfRoles.csproj -c "$CONFIG"

echo "==> [3/4] UpdaterPatcher/TownOfRoles.Updater.Patcher.csproj"
"$DOTNET" build UpdaterPatcher/TownOfRoles.Updater.Patcher.csproj -c "$CONFIG"

echo "==> [4/4] Staging dist/plugins/ + dist/patchers/"
mkdir -p dist/plugins dist/patchers
if [ -f "$ROOT/../Atomic/bin/$CONFIG/Atomic.dll" ]; then
  cp "$ROOT/../Atomic/bin/$CONFIG/Atomic.dll" dist/plugins/
fi
if [ -f "$ROOT/../MarshAPI/bin/$CONFIG/MarshAPI.dll" ]; then
  cp "$ROOT/../MarshAPI/bin/$CONFIG/MarshAPI.dll" dist/plugins/
fi
cp "bin/$CONFIG/TownOfRoles.dll"                              dist/plugins/
cp "UpdaterPatcher/bin/$CONFIG/TownOfRoles.Updater.Patcher.dll" dist/patchers/

echo
echo "Done. Copy dist/plugins/*.dll into <game>/BepInEx/plugins/"
echo "      and dist/patchers/*.dll into <game>/BepInEx/patchers/:"
ls -la dist/plugins/ dist/patchers/
