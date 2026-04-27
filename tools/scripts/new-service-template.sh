#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <ServiceName>"
  exit 1
fi

SERVICE_NAME="$1"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEMPLATE_DIR="$ROOT_DIR/templates/service-template"
TARGET_DIR="$ROOT_DIR/../src/Services/$SERVICE_NAME"

mkdir -p "$(dirname "$TARGET_DIR")"

if [[ -d "$TARGET_DIR" ]]; then
  echo "Target already exists: $TARGET_DIR"
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

echo "Service scaffold created at: $TARGET_DIR"
