#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUTPUT_DIR="${OUTPUT_DIR:-$ROOT_DIR/artifacts/nuget}"
CONFIGURATION="${CONFIGURATION:-Release}"
VERSION="${VERSION:-}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required but was not found in PATH." >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"

PACK_ARGS=(
  pack "$ROOT_DIR/src/BuildingBlocks"
  --configuration "$CONFIGURATION"
  -o "$OUTPUT_DIR"
)

if [[ -n "$VERSION" ]]; then
  PACK_ARGS+=("-p:Version=$VERSION")
fi

echo "Packing building blocks..."
dotnet "${PACK_ARGS[@]}"

echo "Packages created in: $OUTPUT_DIR"
