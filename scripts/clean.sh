#!/usr/bin/env bash
# Remove generated build, test, and leftover WinUI artifacts from the repository.
#
# Usage:
#   ./scripts/clean.sh
#   ./scripts/clean.sh --dry-run
#   ./scripts/clean.sh --no-purge-nuget
#   ./scripts/clean.sh --no-purge-temp
set -u

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

CONFIGURATION=Debug
PURGE_NUGET=1
PURGE_USER_TEMP=1
DRY_RUN=0

usage() {
  cat <<'EOF'
Usage: ./scripts/clean.sh [options]

  --configuration, -c NAME  Build configuration for dotnet clean (Debug default).
  --dry-run                 Skip deletions (and skip dotnet clean / nuget clear).
  --no-purge-nuget          Keep local NuGet caches.
  --no-purge-temp           Keep CursorUsageProgress-related TEMP files.
  -h, --help                Show this help.

By default clears local NuGet caches and matching files under the user TEMP folder.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c)
      if [[ $# -lt 2 ]]; then
        echo "Error: --configuration requires Debug or Release." >&2
        exit 1
      fi
      CONFIGURATION="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    --no-purge-nuget)
      PURGE_NUGET=0
      shift
      ;;
    --no-purge-temp)
      PURGE_USER_TEMP=0
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Error: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

case "$CONFIGURATION" in
  Debug|Release) ;;
  *)
    echo "Error: configuration must be Debug or Release (got: $CONFIGURATION)." >&2
    exit 1
    ;;
esac

remove_generated_path() {
  local path="$1"
  if [[ ! -e "$path" ]]; then
    return 0
  fi
  if [[ "$DRY_RUN" -eq 0 ]]; then
    rm -rf "$path" 2>/dev/null || true
  fi
}

echo "Cleaning Cursor Usage Progress ($CONFIGURATION)..."
if [[ "$DRY_RUN" -eq 0 ]]; then
  if ! dotnet clean ./CursorUsageProgress.csproj -c "$CONFIGURATION" --nologo; then
    echo "Warning: dotnet clean exited with a non-zero status. Continuing with forced artifact cleanup." >&2
  fi
fi

generated_directories=(
  bin
  obj
  Tests/bin
  Tests/obj
  .vs
  TestResults
  artifacts
  _build-check
)

for relative_path in "${generated_directories[@]}"; do
  remove_generated_path "$REPO_ROOT/$relative_path"
done

# .xbf / .pri are leftover WinUI artifacts that can linger beside custom -o folders.
# Find generated files outside excluded directories (any path segment named like PS excludes).
while IFS= read -r -d '' file; do
  if [[ "$DRY_RUN" -eq 0 ]]; then
    rm -f "$file" 2>/dev/null || true
  fi
done < <(
  find "$REPO_ROOT" \
    \( -name .git -o \
       -name bin -o \
       -name obj -o \
       -name .vs -o \
       -name TestResults -o \
       -name artifacts -o \
       -name _build-check -o \
       -name installer \) -prune -o \
    -type f \( \
      -iname '*.xbf' -o \
      -iname '*.pri' -o \
      -iname '*.tmp' -o \
      -iname '*.tlog' -o \
      -iname '*.trx' -o \
      -iname '*.coverage' -o \
      -iname '*.coveragexml' -o \
      -iname '*.pdb' \
    \) -print0 2>/dev/null
)

if [[ "$PURGE_NUGET" -eq 1 ]]; then
  echo 'Clearing local NuGet caches...'
  if [[ "$DRY_RUN" -eq 0 ]]; then
    if ! dotnet nuget locals all --clear; then
      echo "Warning: dotnet nuget locals exited with a non-zero status." >&2
    fi
  fi
fi

if [[ "$PURGE_USER_TEMP" -eq 1 ]]; then
  temp_root="${TMPDIR:-${TEMP:-${TMP:-/tmp}}}"
  # Normalize trailing slash from TMPDIR on some systems.
  temp_root="${temp_root%/}"
  if [[ -d "$temp_root" ]]; then
    # *XamlCompiler* is a leftover WinUI temp-name pattern
    shopt -s nullglob
    for entry in "$temp_root"/*CursorUsageProgress* "$temp_root"/*XamlCompiler*; do
      remove_generated_path "$entry"
    done
    shopt -u nullglob
  fi
fi

echo 'Workspace cleanup complete.'
