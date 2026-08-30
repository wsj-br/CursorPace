#!/usr/bin/env bash
# Publish a self-contained Release build and package for the host platform:
#   Linux  -> AppImage
#   macOS  -> .app bundle (zipped)
#
# Windows packaging uses scripts/build.ps1 (Inno Setup).
#
# Usage:
#   ./scripts/build.sh
#   ./scripts/build.sh --skip-tests
#   ./scripts/build.sh --skip-installer
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

SKIP_TESTS=0
SKIP_INSTALLER=0
RID=""

usage() {
  cat <<'EOF'
Usage: ./scripts/build.sh [options]

  --skip-tests       Skip unit tests.
  --skip-installer   Publish only; skip AppImage or app bundle packaging.
  --rid RID          Override runtime: linux-x64, linux-arm64, osx-x64, or osx-arm64.
  -h, --help         Show this help.

Detects the host OS and architecture:
  Linux  -> linux-x64 or linux-arm64 + AppImage
  macOS  -> osx-arm64 or osx-x64 + zipped .app bundle

On Windows use scripts/build.ps1 instead.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-tests)
      SKIP_TESTS=1
      shift
      ;;
    --skip-installer)
      SKIP_INSTALLER=1
      shift
      ;;
    --rid)
      RID="${2:-}"
      shift 2
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

detect_default_rid() {
  case "$(uname -s)" in
    Linux)
      case "$(uname -m)" in
        x86_64) printf '%s\n' linux-x64 ;;
        aarch64|arm64) printf '%s\n' linux-arm64 ;;
        *)
          echo "Error: unsupported Linux architecture: $(uname -m)" >&2
          exit 1
          ;;
      esac
      ;;
    Darwin)
      case "$(uname -m)" in
        arm64) printf '%s\n' osx-arm64 ;;
        x86_64) printf '%s\n' osx-x64 ;;
        *)
          echo "Error: unsupported macOS architecture: $(uname -m)" >&2
          exit 1
          ;;
      esac
      ;;
    *)
      echo "Error: build.sh supports Linux and macOS only. On Windows use scripts/build.ps1." >&2
      exit 1
      ;;
  esac
}

if [[ -z "$RID" ]]; then
  RID="$(detect_default_rid)"
fi

case "$RID" in
  linux-x64|linux-arm64|osx-x64|osx-arm64) ;;
  *)
    echo "Error: unsupported --rid '$RID' (use linux-x64, linux-arm64, osx-x64, or osx-arm64)." >&2
    exit 1
    ;;
esac

if [[ "$RID" == linux-* && "$(uname -s)" != "Linux" ]]; then
  echo "Error: AppImage packaging must run on Linux (got --rid $RID on $(uname -s))." >&2
  exit 1
fi

if [[ "$RID" == "linux-x64" && "$(uname -m)" != "x86_64" ]]; then
  echo "Error: linux-x64 AppImage packaging must run on an x86_64 host (bundles host native libraries)." >&2
  exit 1
fi

if [[ "$RID" == "linux-arm64" && "$(uname -m)" != "aarch64" && "$(uname -m)" != "arm64" ]]; then
  echo "Error: linux-arm64 AppImage packaging must run on an aarch64 host (bundles host native libraries)." >&2
  exit 1
fi

if [[ "$RID" == osx-* && "$(uname -s)" != "Darwin" ]]; then
  echo "Error: app bundle packaging must run on macOS (got --rid $RID on $(uname -s))." >&2
  exit 1
fi

APP_CSPROJ="$REPO_ROOT/CursorPace.csproj"

get_csproj_property() {
  local path="$1"
  local name="$2"
  local value
  value="$(sed -n "s/.*<${name}>\\([^<]*\\)<\\/${name}>.*/\\1/p" "$path" | head -n 1 | tr -d '[:space:]')"
  if [[ -z "$value" ]]; then
    echo "Error: Could not find <$name> in $path" >&2
    exit 1
  fi
  printf '%s\n' "$value"
}

TFM="$(get_csproj_property "$APP_CSPROJ" TargetFramework)"
VERSION="$(get_csproj_property "$APP_CSPROJ" Version)"
PUBLISH_DIR="$REPO_ROOT/bin/Release/$TFM/$RID/publish"

echo "Publishing $VERSION ($TFM / $RID, self-contained)..."
if [[ "$SKIP_TESTS" -eq 0 ]]; then
  echo "Running tests..."
  dotnet test ./Tests/CursorPace.Tests.csproj -c Release
fi

dotnet publish "$APP_CSPROJ" -c Release -r "$RID" --self-contained \
  -p:PublishSingleFile=false

PUBLISHED_EXE="$PUBLISH_DIR/CursorPace"
if [[ ! -f "$PUBLISHED_EXE" ]]; then
  echo "Error: Publish succeeded but did not produce $PUBLISHED_EXE" >&2
  exit 1
fi

echo "Published: $PUBLISH_DIR"

if [[ "$SKIP_INSTALLER" -eq 1 ]]; then
  echo "Skipping packaging (--skip-installer)."
  exit 0
fi

mkdir -p "$REPO_ROOT/installer"

case "$RID" in
  linux-x64|linux-arm64)
    chmod +x "$REPO_ROOT/scripts/build-appimage.sh"
    "$REPO_ROOT/scripts/build-appimage.sh" --version "$VERSION" --rid "$RID" --publish-dir "$PUBLISH_DIR"
    ;;
  osx-x64|osx-arm64)
    chmod +x "$REPO_ROOT/scripts/build-appbundle.sh"
    "$REPO_ROOT/scripts/build-appbundle.sh" --version "$VERSION" --rid "$RID" --publish-dir "$PUBLISH_DIR"
    ;;
  *)
    echo "Error: no packaging step for RID $RID" >&2
    exit 1
    ;;
esac
