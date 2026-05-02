#!/usr/bin/env bash
set -Eeuo pipefail

# -----------------------------------------------------------------------------
# Publishes all NuGet packages from artifacts/nuget to a configured NuGet source.
#
# Versioning policy:
#   - Versions are already baked into .nupkg files by pack-building-blocks.sh.
#   - This script publishes all package files found in PACKAGE_DIR.
#   - Existing package versions are skipped via --skip-duplicate.
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
  echo "ERROR: publish-building-blocks.sh failed on line ${line_no} with exit code ${exit_code}." >&2
  pause_if_needed
  exit "$exit_code"
}

trap 'on_error $LINENO' ERR

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
ROOT_DIR="${ROOT_DIR:-$(cd "$SCRIPT_DIR/../.." && pwd -P)}"
PACKAGE_DIR="${PACKAGE_DIR:-$ROOT_DIR/artifacts/nuget}"
NUGET_SOURCE="${NUGET_SOURCE:-nexus-internal}"
API_KEY="${NEXUS_API_KEY:-${API_KEY:-}}"
INCLUDE_SYMBOLS="${INCLUDE_SYMBOLS:-false}"
DRY_RUN="${DRY_RUN:-false}"
SKIP_DUPLICATE="${SKIP_DUPLICATE:-true}"
PUSH_TIMEOUT_SECONDS="${PUSH_TIMEOUT_SECONDS:-300}"

print_usage() {
  cat <<USAGE
Usage:
  ./publish-building-blocks.sh [options]

Description:
  Publishes all .nupkg files from artifacts/nuget to the configured NuGet source.
  Duplicate package versions are skipped by default.

Options:
  --package-dir <path>      Directory that contains .nupkg files.
                            Default: ROOT_DIR/artifacts/nuget

  --source <name-or-url>    NuGet source name or URL.
                            Default: nexus-internal

  --include-symbols         Also publish .snupkg symbol packages.

  --dry-run                 Print what would be published without pushing.

  --no-skip-duplicate       Disable --skip-duplicate.
                            Not recommended for CI/CD.

  --timeout <seconds>       Push timeout in seconds.
                            Default: 300

  --no-pause                Never pause at the end.

  --pause                   Always pause at the end.

  -h, --help                Show this help.

Environment variables:
  ROOT_DIR              Repository root. Default: two levels above this script.
  PACKAGE_DIR           Directory that contains .nupkg files.
  NUGET_SOURCE          NuGet source name or URL. Default: nexus-internal.
  NEXUS_API_KEY         Preferred API key variable.
  API_KEY               Fallback API key variable.
  INCLUDE_SYMBOLS       true/false. Default: false.
  DRY_RUN               true/false. Default: false.
  SKIP_DUPLICATE        true/false. Default: true.
  PUSH_TIMEOUT_SECONDS  Default: 300.
  PAUSE_ON_EXIT         auto/always/never. Default: auto.

Examples:
  NEXUS_API_KEY=xxx ./scripts/nuget/publish-building-blocks.sh
  NEXUS_API_KEY=xxx NUGET_SOURCE=nexus-building-blocks ./scripts/nuget/publish-building-blocks.sh
  ./scripts/nuget/publish-building-blocks.sh --dry-run
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package-dir)
      [[ $# -ge 2 ]] || { echo "ERROR: --package-dir requires a value." >&2; exit 1; }
      PACKAGE_DIR="$2"
      shift 2
      ;;
    --source)
      [[ $# -ge 2 ]] || { echo "ERROR: --source requires a value." >&2; exit 1; }
      NUGET_SOURCE="$2"
      shift 2
      ;;
    --include-symbols)
      INCLUDE_SYMBOLS="true"
      shift
      ;;
    --dry-run)
      DRY_RUN="true"
      shift
      ;;
    --no-skip-duplicate)
      SKIP_DUPLICATE="false"
      shift
      ;;
    --timeout)
      [[ $# -ge 2 ]] || { echo "ERROR: --timeout requires a value." >&2; exit 1; }
      PUSH_TIMEOUT_SECONDS="$2"
      shift 2
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

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required but was not found in PATH." >&2
  exit 1
fi

if [[ ! "$PUSH_TIMEOUT_SECONDS" =~ ^[0-9]+$ ]]; then
  echo "ERROR: --timeout must be a positive number of seconds." >&2
  exit 1
fi

if [[ ! -d "$PACKAGE_DIR" ]]; then
  echo "ERROR: Package directory does not exist: $PACKAGE_DIR" >&2
  exit 1
fi

if [[ "$DRY_RUN" != "true" && -z "$API_KEY" ]]; then
  echo "ERROR: API key is required. Set NEXUS_API_KEY or API_KEY." >&2
  exit 1
fi

shopt -s nullglob
packages=("$PACKAGE_DIR"/*.nupkg)

if [[ "$INCLUDE_SYMBOLS" == "true" ]]; then
  packages+=("$PACKAGE_DIR"/*.snupkg)
fi
shopt -u nullglob

if [[ ${#packages[@]} -eq 0 ]]; then
  echo "ERROR: No package files found in $PACKAGE_DIR" >&2
  if [[ "$INCLUDE_SYMBOLS" == "true" ]]; then
    echo "Expected: *.nupkg or *.snupkg" >&2
  else
    echo "Expected: *.nupkg" >&2
  fi
  exit 1
fi

log "Repository root: $ROOT_DIR"
log "Package dir: $PACKAGE_DIR"
log "NuGet source: $NUGET_SOURCE"
log "Include symbols: $INCLUDE_SYMBOLS"
log "Dry run: $DRY_RUN"
log "Skip duplicate: $SKIP_DUPLICATE"
log "Timeout: ${PUSH_TIMEOUT_SECONDS}s"

pushd "$ROOT_DIR" >/dev/null

published_count=0

for pkg in "${packages[@]}"; do
  if [[ ! -f "$pkg" ]]; then
    continue
  fi

  if [[ "$DRY_RUN" == "true" ]]; then
    echo "DRY RUN: would publish: $pkg"
    continue
  fi

  log "Publishing: $pkg"

  push_args=(
    nuget push "$pkg"
    --source "$NUGET_SOURCE"
    --api-key "$API_KEY"
    --timeout "$PUSH_TIMEOUT_SECONDS"
  )

  if [[ "$SKIP_DUPLICATE" == "true" ]]; then
    push_args+=(--skip-duplicate)
  fi

  dotnet "${push_args[@]}"
  published_count=$((published_count + 1))
done

popd >/dev/null

if [[ "$DRY_RUN" == "true" ]]; then
  log "Dry run completed. Packages found: ${#packages[@]}"
else
  log "Publish completed. Publish attempts: $published_count"
fi

pause_if_needed
