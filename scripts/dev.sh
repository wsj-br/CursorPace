#!/usr/bin/env bash
# Run Cursor Pace for local development.
#
# Usage:
#   ./scripts/dev.sh
#   ./scripts/dev.sh --background
#   ./scripts/dev.sh --show
#   ./scripts/dev.sh --configuration Release
#   ./scripts/dev.sh --test
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

BACKGROUND=0
SHOW=0
CONFIGURATION=Debug
TEST=0

usage() {
  cat <<'EOF'
Usage: ./scripts/dev.sh [options]

  --background, -b          Launch in tray-only mode (--background).
  --show, -s                Force the main window open (--show). Wins over --background.
  --configuration, -c NAME  Build configuration: Debug (default) or Release.
  --test, -t                Run unit tests instead of launching the app.
  -h, --help                Show this help.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --background|-b)
      BACKGROUND=1
      shift
      ;;
    --show|-s)
      SHOW=1
      shift
      ;;
    --configuration|-c)
      if [[ $# -lt 2 ]]; then
        echo "Error: --configuration requires Debug or Release." >&2
        exit 1
      fi
      CONFIGURATION="$2"
      shift 2
      ;;
    --test|-t)
      TEST=1
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

if [[ "$TEST" -eq 1 ]]; then
  echo "Running tests ($CONFIGURATION)..."
  exec dotnet test ./Tests/CursorPace.Tests.csproj -c "$CONFIGURATION"
fi

echo "Starting Cursor Pace ($CONFIGURATION)..."
run_args=(run --project ./CursorPace.csproj -c "$CONFIGURATION")
if [[ "$SHOW" -eq 1 ]]; then
  run_args+=(-- --show)
elif [[ "$BACKGROUND" -eq 1 ]]; then
  run_args+=(-- --background)
fi

exec dotnet "${run_args[@]}"
