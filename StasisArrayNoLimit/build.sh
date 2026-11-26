#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
MANAGED="${MANAGED:-/Applications/Stardeus.app/Contents/Resources/Data/Managed}"
CSC="$(find "$HOME/.dotnet/sdk" -path '*/Roslyn/bincore/csc.dll' | sort | tail -1)"

if [[ ! -x "$DOTNET" ]]; then
    echo "dotnet not found at $DOTNET" >&2
    exit 1
fi
if [[ ! -f "$CSC" ]]; then
    echo "csc.dll not found under $HOME/.dotnet/sdk" >&2
    exit 1
fi

mkdir -p "$ROOT/Libraries"
REFS=()
for dll in "$MANAGED"/*.dll; do
    REFS+=("/r:$dll")
done

"$DOTNET" "$CSC" \
    -nologo \
    -target:library \
    -langversion:9.0 \
    -nostdlib+ \
    -noconfig \
    -out:"$ROOT/Libraries/StasisArrayNoLimit.dll" \
    "${REFS[@]}" \
    "$ROOT"/Source/*.cs

echo "Built $ROOT/Libraries/StasisArrayNoLimit.dll"
