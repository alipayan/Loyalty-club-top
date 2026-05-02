#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PACKAGE_DIR="${PACKAGE_DIR:-$ROOT_DIR/artifacts/nuget}"
NUGET_SOURCE="${NUGET_SOURCE:-nexus-internal}"
API_KEY="${NEXUS_API_KEY:-${API_KEY:-}}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required but was not found in PATH." >&2
  exit 1
fi

if [[ -z "$API_KEY" ]]; then
  echo "ERROR: API key is required. Set NEXUS_API_KEY or API_KEY." >&2
  exit 1
fi

shopt -s nullglob
packages=("$PACKAGE_DIR"/*.nupkg)

if [[ ${#packages[@]} -eq 0 ]]; then
  echo "ERROR: No .nupkg files found in $PACKAGE_DIR" >&2
  exit 1
fi

for pkg in "${packages[@]}"; do
  if [[ "$pkg" == *.snupkg ]]; then
    continue
  fi

  echo "Publishing: $pkg"
  dotnet nuget push "$pkg" \
    --source "$NUGET_SOURCE" \
    --api-key "$API_KEY" \
    --skip-duplicate
done

echo "Publish completed."
