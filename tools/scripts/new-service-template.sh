#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <ServiceName> [SolutionPath]"
  echo "  ServiceName: name used in folders/files/namespaces (e.g. Member)"
  echo "  SolutionPath: optional .sln path to add service projects into"
  exit 1
fi

SERVICE_NAME="$1"
SOLUTION_PATH_INPUT="${2:-}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEMPLATE_DIR="$ROOT_DIR/templates/service-template"
TARGET_DIR="$ROOT_DIR/../src/Services/$SERVICE_NAME"

mkdir -p "$(dirname "$TARGET_DIR")"

if [[ -d "$TARGET_DIR" ]]; then
  echo "Target already exists: $TARGET_DIR"
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required but was not found in PATH."
  exit 1
fi

cp -R "$TEMPLATE_DIR" "$TARGET_DIR"

# Rename directories first (deepest first)
find "$TARGET_DIR" -depth -type d -name '*ServiceTemplate*' | while read -r path; do
  new_path="${path//ServiceTemplate/$SERVICE_NAME}"
  mv "$path" "$new_path"
done

# Rename files next
find "$TARGET_DIR" -type f -name '*ServiceTemplate*' | while read -r path; do
  new_path="${path//ServiceTemplate/$SERVICE_NAME}"
  mv "$path" "$new_path"
done

# Replace content tokens
find "$TARGET_DIR" -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.md' -o -name '*.json' -o -name 'Dockerfile' -o -name '*.yml' \) \
  -exec sed -i "s/ServiceTemplate/$SERVICE_NAME/g" {} +

SERVICE_SOLUTION_PATH="$TARGET_DIR/CustomerClub.$SERVICE_NAME.sln"
TARGET_SOLUTION_PATH="$SERVICE_SOLUTION_PATH"

if [[ -n "$SOLUTION_PATH_INPUT" ]]; then
  if [[ "$SOLUTION_PATH_INPUT" = /* ]]; then
    TARGET_SOLUTION_PATH="$SOLUTION_PATH_INPUT"
  else
    TARGET_SOLUTION_PATH="$PWD/$SOLUTION_PATH_INPUT"
  fi
fi

if [[ ! "$TARGET_SOLUTION_PATH" =~ \.sln$ ]]; then
  TARGET_SOLUTION_PATH="${TARGET_SOLUTION_PATH}.sln"
fi

mkdir -p "$(dirname "$TARGET_SOLUTION_PATH")"

if [[ ! -f "$TARGET_SOLUTION_PATH" ]]; then
  dotnet new sln -n "$(basename "$TARGET_SOLUTION_PATH" .sln)" -o "$(dirname "$TARGET_SOLUTION_PATH")" >/dev/null
fi

while IFS= read -r -d '' csproj; do
  dotnet sln "$TARGET_SOLUTION_PATH" add "$csproj" >/dev/null
done < <(find "$TARGET_DIR" -type f -name '*.csproj' -print0 | sort -z)

echo "Service scaffold created at: $TARGET_DIR"
echo "Solution updated at: $TARGET_SOLUTION_PATH"
