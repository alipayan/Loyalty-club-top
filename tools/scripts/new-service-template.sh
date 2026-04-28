#!/usr/bin/env bash
set -Eeuo pipefail

trap 'echo; echo "ERROR on line $LINENO"; read -p "Press Enter to exit..."' ERR

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <ServiceName> [SolutionPath]"
  read -p "Press Enter to exit..."
  exit 1
fi

SERVICE_NAME="$1"
SOLUTION_PATH_INPUT="${2:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

TEMPLATE_DIR="$ROOT_DIR/templates/service-template"
TARGET_DIR="$ROOT_DIR/../src/Services/$SERVICE_NAME"

mkdir -p "$(dirname "$TARGET_DIR")"

if [[ -d "$TARGET_DIR" ]]; then
  echo "Target already exists: $TARGET_DIR"
  read -p "Press Enter to exit..."
  exit 1
fi

cp -R "$TEMPLATE_DIR" "$TARGET_DIR"

# rename template tokens
find "$TARGET_DIR" -depth -type d -name '*ServiceTemplate*' | while read -r path; do
  mv "$path" "${path//ServiceTemplate/$SERVICE_NAME}"
done

find "$TARGET_DIR" -type f -name '*ServiceTemplate*' | while read -r path; do
  mv "$path" "${path//ServiceTemplate/$SERVICE_NAME}"
done

find "$TARGET_DIR" -type f \
  \( -name '*.cs' -o -name '*.csproj' -o -name '*.md' -o -name '*.json' -o -name '*.yml' -o -name '*.yaml' \) \
  -exec sed -i "s/ServiceTemplate/$SERVICE_NAME/g" {} +

# solution path (.slnx)
TARGET_SOLUTION_PATH="$TARGET_DIR/CustomerClub.$SERVICE_NAME.slnx"

if [[ -n "$SOLUTION_PATH_INPUT" ]]; then
  TARGET_SOLUTION_PATH="$SOLUTION_PATH_INPUT"
fi

mkdir -p "$(dirname "$TARGET_SOLUTION_PATH")"

# create slnx
dotnet new sln \
  -n "$(basename "$TARGET_SOLUTION_PATH" .slnx)" \
  -o "$(dirname "$TARGET_SOLUTION_PATH")" \
  --force

# add projects
mapfile -t CSPROJ_FILES < <(find "$TARGET_DIR" -type f -name '*.csproj' | sort)

for csproj in "${CSPROJ_FILES[@]}"; do
  rel="${csproj#$TARGET_DIR/}"

  if [[ "$rel" == src/* ]]; then
    folder="src"
  elif [[ "$rel" == tests/* ]]; then
    folder="tests"
  else
    folder="misc"
  fi

  echo "Adding project: $rel -> $folder"
  dotnet sln "$TARGET_SOLUTION_PATH" add "$csproj" --solution-folder "$folder"
done

# Add solution items (correct slnx way)
FILES=$(find "$TARGET_DIR/SolutionItems" -type f 2>/dev/null || true)

if [[ -n "$FILES" ]]; then
  TMP_FILE="$(mktemp)"

  awk -v base="$TARGET_DIR" -v files="$FILES" '
  BEGIN { split(files, arr, "\n") }

  /<\/Solution>/ && !added {

    print "  <Folder Name=\"/Solution Items/\">"

    for (i in arr) {
      rel = arr[i]
      sub(base"/", "", rel)
      print "    <File Path=\"" rel "\" />"
    }

    print "  </Folder>"
    added=1
  }

  { print }
  ' "$TARGET_SOLUTION_PATH" > "$TMP_FILE"

  mv "$TMP_FILE" "$TARGET_SOLUTION_PATH"
fi

echo "----------------------------------------"
echo "Service created at: $TARGET_DIR"
echo "Solution: $TARGET_SOLUTION_PATH"
echo "----------------------------------------"

read -p "Press Enter to exit..."