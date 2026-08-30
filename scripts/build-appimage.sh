#!/usr/bin/env bash
# Build an AppImage from a linux-x64 or linux-arm64 self-contained publish folder.
#
# Usage:
#   ./scripts/build-appimage.sh --version 0.2.0 --rid linux-x64 --publish-dir bin/Release/net10.0/linux-x64/publish
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

VERSION=""
RID=""
PUBLISH_DIR=""

usage() {
  cat <<'EOF'
Usage: ./scripts/build-appimage.sh --version VERSION --rid RID --publish-dir PATH

  --version       App version (matches CursorUsageProgress.csproj).
  --rid           linux-x64 or linux-arm64.
  --publish-dir   Self-contained publish output directory matching --rid.
  -h, --help      Show this help.

Requires linuxdeploy (downloaded automatically for the target architecture)
and WebKitGTK/GTK dev/runtime libraries on the build host so dependency
bundling can resolve native libs. The build host architecture must match
--rid because linuxdeploy bundles the host's native libraries.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="${2:-}"
      shift 2
      ;;
    --rid)
      RID="${2:-}"
      shift 2
      ;;
    --publish-dir)
      PUBLISH_DIR="${2:-}"
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

if [[ -z "$VERSION" || -z "$RID" || -z "$PUBLISH_DIR" ]]; then
  echo "Error: --version, --rid, and --publish-dir are required." >&2
  usage >&2
  exit 1
fi

case "$RID" in
  linux-x64)
    ARCH=x86_64
    ;;
  linux-arm64)
    ARCH=aarch64
    ;;
  *)
    echo "Error: unsupported --rid '$RID' (use linux-x64 or linux-arm64)." >&2
    exit 1
    ;;
esac

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Error: publish directory not found: $PUBLISH_DIR" >&2
  exit 1
fi

PUBLISHED_BIN="$PUBLISH_DIR/CursorUsageProgress"
if [[ ! -f "$PUBLISHED_BIN" ]]; then
  echo "Error: publish folder is missing $PUBLISHED_BIN" >&2
  exit 1
fi

DESKTOP_SOURCE="$REPO_ROOT/packaging/cursor-usage-progress.desktop"
ICON_SOURCE="$REPO_ROOT/Assets/cursor_usage_progress.png"
APPDATA_SOURCE="$REPO_ROOT/packaging/io.github.wsj_br.CursorUsageProgress.appdata.xml"
if [[ ! -f "$DESKTOP_SOURCE" ]]; then
  echo "Error: desktop file not found: $DESKTOP_SOURCE" >&2
  exit 1
fi
if [[ ! -f "$ICON_SOURCE" ]]; then
  echo "Error: icon not found: $ICON_SOURCE" >&2
  exit 1
fi
if [[ ! -f "$APPDATA_SOURCE" ]]; then
  echo "Error: AppStream metadata not found: $APPDATA_SOURCE" >&2
  exit 1
fi

BUILD_DIR="$REPO_ROOT/.appimage-build"
APPDIR="$BUILD_DIR/CursorUsageProgress.AppDir"
LINUXDEPLOY_VERSION="1-alpha-20251107-1"
# linuxdeploy-plugin-gtk has no tagged releases; its script only ever lives on
# master, so pin a commit to keep the AppImage build reproducible.
GTK_PLUGIN_REF="7a3fbc31a9e5075073ff8790f26effbac5f84453"
mkdir -p "$BUILD_DIR" "$REPO_ROOT/installer"

prepare_appimage_icon() {
  local source="$1"
  local dest="$2"
  if command -v magick >/dev/null 2>&1; then
    magick "$source" -resize 256x256! -strip "$dest"
  elif command -v convert >/dev/null 2>&1; then
    convert "$source" -resize 256x256! -strip "$dest"
  else
    echo "Error: ImageMagick (magick or convert) is required to resize the app icon for AppImage packaging." >&2
    exit 1
  fi
}

ICON_FOR_APPIMAGE="$BUILD_DIR/cursor-usage-progress.png"
prepare_appimage_icon "$ICON_SOURCE" "$ICON_FOR_APPIMAGE"

download_file() {
  local url="$1"
  local dest="$2"
  local mark_executable="${3:-0}"
  if [[ -f "$dest" ]]; then
    return 0
  fi

  echo "Downloading $(basename "$dest") ..."
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$url" -o "$dest"
  elif command -v wget >/dev/null 2>&1; then
    wget -q "$url" -O "$dest"
  else
    echo "Error: need curl or wget to download AppImage tooling." >&2
    exit 1
  fi
  if [[ "$mark_executable" == "1" ]]; then
    chmod +x "$dest"
  fi
}

# Cached filenames include the pinned ref so bumping LINUXDEPLOY_VERSION or
# GTK_PLUGIN_REF re-downloads instead of silently reusing a stale cached copy
# from .appimage-build/ left over by a previous run.
LINUXDEPLOY="$BUILD_DIR/linuxdeploy-$ARCH-$LINUXDEPLOY_VERSION.AppImage"
GTK_PLUGIN="$BUILD_DIR/linuxdeploy-plugin-gtk-$GTK_PLUGIN_REF.sh"
download_file \
  "https://github.com/linuxdeploy/linuxdeploy/releases/download/${LINUXDEPLOY_VERSION}/linuxdeploy-$ARCH.AppImage" \
  "$LINUXDEPLOY" \
  1
download_file \
  "https://raw.githubusercontent.com/linuxdeploy/linuxdeploy-plugin-gtk/${GTK_PLUGIN_REF}/linuxdeploy-plugin-gtk.sh" \
  "$GTK_PLUGIN" \
  1

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/metainfo"
cp -a "$PUBLISH_DIR/." "$APPDIR/usr/bin/"
cp "$APPDATA_SOURCE" "$APPDIR/usr/share/metainfo/io.github.wsj_br.CursorUsageProgress.appdata.xml"
# Optional .NET diagnostics pull lttng deps that are not needed at runtime.
rm -f "$APPDIR/usr/bin/createdump" "$APPDIR/usr/bin/libcoreclrtraceptprovider.so"

export ARCH
export VERSION
export DEPLOY_GTK_VERSION=3

echo "Bundling dependencies into AppDir ..."
"$LINUXDEPLOY" --appimage-extract-and-run \
  --appdir="$APPDIR" \
  --executable="$APPDIR/usr/bin/CursorUsageProgress" \
  --desktop-file="$DESKTOP_SOURCE" \
  --icon-file="$ICON_FOR_APPIMAGE" \
  --plugin gtk \
  --output appimage

APPIMAGE_PATH=""
shopt -s nullglob
for candidate in "$REPO_ROOT"/*.AppImage "$BUILD_DIR"/*.AppImage; do
  case "$(basename "$candidate")" in
    linuxdeploy*.AppImage|linuxdeploy-plugin*.AppImage) continue ;;
  esac
  APPIMAGE_PATH="$candidate"
  break
done
shopt -u nullglob

if [[ -z "$APPIMAGE_PATH" || ! -f "$APPIMAGE_PATH" ]]; then
  echo "Error: linuxdeploy did not produce an AppImage in $REPO_ROOT or $BUILD_DIR" >&2
  exit 1
fi

INSTALLER_NAME="CursorUsageProgress-$VERSION-$RID.AppImage"
INSTALLER_PATH="$REPO_ROOT/installer/$INSTALLER_NAME"
mv -f "$APPIMAGE_PATH" "$INSTALLER_PATH"
chmod +x "$INSTALLER_PATH"

if command -v sha256sum >/dev/null 2>&1; then
  HASH="$(sha256sum "$INSTALLER_PATH" | awk '{print toupper($1)}')"
elif command -v shasum >/dev/null 2>&1; then
  HASH="$(shasum -a 256 "$INSTALLER_PATH" | awk '{print toupper($1)}')"
else
  echo "Error: need sha256sum or shasum to write the AppImage checksum." >&2
  exit 1
fi

HASH_PATH="$INSTALLER_PATH.sha256"
printf '%s  %s\n' "$HASH" "$INSTALLER_NAME" >"$HASH_PATH"

echo "AppImage:  $INSTALLER_PATH"
echo "SHA256:    $HASH"
echo "Checksum:  $HASH_PATH"
