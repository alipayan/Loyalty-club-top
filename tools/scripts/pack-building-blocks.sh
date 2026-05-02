#!/usr/bin/env bash
set -Eeuo pipefail

# -----------------------------------------------------------------------------
# Packs all packable BuildingBlocks projects as NuGet packages.
#
# Versioning policy:
#   - Each package owns its own <PackageId>, <Version>, and <Description> in its .csproj.
#   - Common package metadata lives in Directory.Build.props at repo root.
#   - This script does NOT set package versions. It only reads project configuration.
#
# Default behavior:
#   - Finds all .csproj files under src/BuildingBlocks.
#   - Skips projects with <IsPackable>false</IsPackable>.
#   - Requires <PackageId> and <Version> in each packable .csproj.
#   - Cleans artifacts/nuget before packing to avoid publishing stale packages.
#   - Pauses at the end only when run interactively, so CI/CD will not hang.
# -----------------------------------------------------------------------------

PAUSE_ON_EXIT="${PAUSE_ON_EXIT:-auto}" # auto | always | never

pause_if_needed() {
  if [[ "$PAUSE_ON_EXIT" == "never" ]]; then
    return 0
  fi

  if [[ "$PAUSE_ON_EXIT" == "always" || -t 0 ]]; then
    echo
    read -rp "Press Enter to exit..." || true
  fi
}

on_error() {
  local exit_code=$?
  local line_no=${1:-unknown}
  echo >&2
  echo "ERROR: pack-building-blocks.sh failed on line ${line_no} with exit code ${exit_code}." >&2
  pause_if_needed
  exit "$exit_code"
}

trap 'on_error $LINENO' ERR

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
ROOT_DIR="${ROOT_DIR:-$(cd "$SCRIPT_DIR/../.." && pwd -P)}"
BUILDING_BLOCKS_DIR="${BUILDING_BLOCKS_DIR:-$ROOT_DIR/src/BuildingBlocks}"
OUTPUT_DIR="${OUTPUT_DIR:-$ROOT_DIR/artifacts/nuget}"
CONFIGURATION="${CONFIGURATION:-Release}"
CLEAN_OUTPUT="${CLEAN_OUTPUT:-true}"
INCLUDE_SYMBOLS="${INCLUDE_SYMBOLS:-false}"
NO_RESTORE="${NO_RESTORE:-false}"

print_usage() {
  cat <<USAGE
Usage:
  ./pack-building-blocks.sh [options]

Description:
  Packs all packable .csproj files under src/BuildingBlocks.
  PackageId and Version are read from each project file.

Options:
  --building-blocks-dir <path>  Directory that contains BuildingBlocks projects.
                                Default: ROOT_DIR/src/BuildingBlocks

  --output-dir <path>           NuGet package output directory.
                                Default: ROOT_DIR/artifacts/nuget

  --configuration <value>       Build configuration.
                                Default: Release

  --include-symbols             Also generate .snupkg symbol packages.

  --no-restore                  Pass --no-restore to dotnet pack.

  --no-clean-output             Do not clean existing .nupkg/.snupkg files before packing.
                                Default behavior is to clean output to avoid stale publish.

  --no-pause                    Never pause at the end.

  --pause                       Always pause at the end.

  -h, --help                    Show this help.

Environment variables:
  ROOT_DIR                 Repository root. Default: two levels above this script.
  BUILDING_BLOCKS_DIR      BuildingBlocks directory.
  OUTPUT_DIR               Package output directory.
  CONFIGURATION            Release or Debug. Default: Release.
  CLEAN_OUTPUT             true/false. Default: true.
  INCLUDE_SYMBOLS          true/false. Default: false.
  NO_RESTORE               true/false. Default: false.
  PAUSE_ON_EXIT            auto/always/never. Default: auto.

Examples:
  ./scripts/nuget/pack-building-blocks.sh
  ./scripts/nuget/pack-building-blocks.sh --include-symbols
  CONFIGURATION=Debug ./scripts/nuget/pack-building-blocks.sh
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --building-blocks-dir)
      [[ $# -ge 2 ]] || { echo "ERROR: --building-blocks-dir requires a value." >&2; exit 1; }
      BUILDING_BLOCKS_DIR="$2"
      shift 2
      ;;
    --output-dir)
      [[ $# -ge 2 ]] || { echo "ERROR: --output-dir requires a value." >&2; exit 1; }
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --configuration)
      [[ $# -ge 2 ]] || { echo "ERROR: --configuration requires a value." >&2; exit 1; }
      CONFIGURATION="$2"
      shift 2
      ;;
    --include-symbols)
      INCLUDE_SYMBOLS="true"
      shift
      ;;
    --no-restore)
      NO_RESTORE="true"
      shift
      ;;
    --no-clean-output)
      CLEAN_OUTPUT="false"
      shift
      ;;
    --no-pause)
      PAUSE_ON_EXIT="never"
      shift
      ;;
    --pause)
      PAUSE_ON_EXIT="always"
      shift
      ;;
    -h|--help)
      print_usage
      exit 0
      ;;
    *)
      echo "ERROR: Unknown option: $1" >&2
      echo >&2
      print_usage >&2
      exit 1
      ;;
  esac
done

log() {
  echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

trim() {
  local value="$1"
  value="${value#"${value%%[![:space:]]*}"}"
  value="${value%"${value##*[![:space:]]}"}"
  printf '%s' "$value"
}

extract_xml_value() {
  local file="$1"
  local tag="$2"

  # This intentionally reads the project file itself, because package-specific
  # values such as PackageId and Version should be explicit per package.
  sed -nE "s|.*<${tag}>([^<]+)</${tag}>.*|\1|p" "$file" | tail -n 1 | while IFS= read -r line; do trim "$line"; done
}

has_is_packable_false() {
  local file="$1"
  grep -Eiq '<IsPackable>[[:space:]]*false[[:space:]]*</IsPackable>' "$file"
}

validate_semver_like() {
  local version="$1"
  [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z][0-9A-Za-z.-]*)?$ ]]
}

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required but was not found in PATH." >&2
  exit 1
fi

if [[ ! -d "$ROOT_DIR" ]]; then
  echo "ERROR: ROOT_DIR does not exist: $ROOT_DIR" >&2
  exit 1
fi

if [[ ! -d "$BUILDING_BLOCKS_DIR" ]]; then
  echo "ERROR: BuildingBlocks directory does not exist: $BUILDING_BLOCKS_DIR" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"

if [[ "$CLEAN_OUTPUT" == "true" ]]; then
  log "Cleaning NuGet output directory: $OUTPUT_DIR"
  find "$OUTPUT_DIR" -maxdepth 1 \( -name '*.nupkg' -o -name '*.snupkg' \) -type f -delete
fi

projects=()
while IFS= read -r -d '' project; do
  projects+=("$project")
done < <(find "$BUILDING_BLOCKS_DIR" -type f -name '*.csproj' -print0)

if [[ ${#projects[@]} -eq 0 ]]; then
  echo "ERROR: No .csproj files found under: $BUILDING_BLOCKS_DIR" >&2
  exit 1
fi

log "Repository root: $ROOT_DIR"
log "BuildingBlocks dir: $BUILDING_BLOCKS_DIR"
log "Output dir: $OUTPUT_DIR"
log "Configuration: $CONFIGURATION"
log "Include symbols: $INCLUDE_SYMBOLS"
log "No restore: $NO_RESTORE"

packed_count=0
skipped_count=0

pushd "$ROOT_DIR" >/dev/null

for project in "${projects[@]}"; do
  if has_is_packable_false "$project"; then
    log "Skipping non-packable project: $project"
    skipped_count=$((skipped_count + 1))
    continue
  fi

  package_id="$(extract_xml_value "$project" "PackageId")"
  version="$(extract_xml_value "$project" "Version")"

  if [[ -z "$package_id" ]]; then
    echo "ERROR: Packable project is missing <PackageId>: $project" >&2
    exit 1
  fi

  if [[ -z "$version" ]]; then
    echo "ERROR: Packable project is missing <Version>: $project" >&2
    exit 1
  fi

  if ! validate_semver_like "$version"; then
    echo "ERROR: Invalid package version '$version' in project: $project" >&2
    echo "Expected examples: 1.0.0, 1.0.1, 1.1.0-preview.1" >&2
    exit 1
  fi

  log "Packing ${package_id} ${version}"

  pack_args=(
    pack "$project"
    --configuration "$CONFIGURATION"
    --output "$OUTPUT_DIR"
    -p:ContinuousIntegrationBuild=true
  )

  if [[ "$NO_RESTORE" == "true" ]]; then
    pack_args+=(--no-restore)
  fi

  if [[ "$INCLUDE_SYMBOLS" == "true" ]]; then
    pack_args+=(--include-symbols -p:SymbolPackageFormat=snupkg)
  fi

  dotnet "${pack_args[@]}"
  packed_count=$((packed_count + 1))
done

popd >/dev/null

shopt -s nullglob
created_packages=("$OUTPUT_DIR"/*.nupkg)
created_symbols=("$OUTPUT_DIR"/*.snupkg)
shopt -u nullglob

if [[ ${#created_packages[@]} -eq 0 ]]; then
  echo "ERROR: Pack completed but no .nupkg files were created in: $OUTPUT_DIR" >&2
  exit 1
fi

log "Pack completed. Packed: $packed_count, Skipped: $skipped_count"
log "Packages created:"
for pkg in "${created_packages[@]}"; do
  echo "  - $pkg"
done

if [[ ${#created_symbols[@]} -gt 0 ]]; then
  log "Symbol packages created:"
  for pkg in "${created_symbols[@]}"; do
    echo "  - $pkg"
  done
fi

pause_if_needed
