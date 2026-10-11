#!/usr/bin/env bash
# Build a Debian package from a linux-x64 or linux-arm64 self-contained publish folder.
#
# Usage:
#   ./scripts/build-deb.sh --version 0.x.y --rid linux-x64 --publish-dir bin/Release/net10.0/linux-x64/publish
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

VERSION=""
RID=""
PUBLISH_DIR=""

usage() {
  cat <<'EOF'
Usage: ./scripts/build-deb.sh --version VERSION --rid RID --publish-dir PATH

  --version       App version (matches CursorPace.csproj).
  --rid           linux-x64 or linux-arm64.
  --publish-dir   Self-contained publish output directory matching --rid.
  -h, --help      Show this help.

Writes installer/CursorPace-<version>-<rid>.deb and a .sha256 file.
Copies the publish folder (not the AppImage AppDir) so the package uses
system WebKitGTK. Requires dpkg-deb and ImageMagick on the build host.
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

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "Error: build-deb.sh must run on Linux." >&2
  exit 1
fi

if [[ -z "$VERSION" || -z "$RID" || -z "$PUBLISH_DIR" ]]; then
  echo "Error: --version, --rid, and --publish-dir are required." >&2
  usage >&2
  exit 1
fi

case "$RID" in
  linux-x64)
    DEB_ARCH=amd64
    ;;
  linux-arm64)
    DEB_ARCH=arm64
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

PUBLISHED_BIN="$PUBLISH_DIR/CursorPace"
if [[ ! -f "$PUBLISHED_BIN" ]]; then
  echo "Error: publish folder is missing $PUBLISHED_BIN" >&2
  exit 1
fi

if ! command -v dpkg-deb >/dev/null 2>&1; then
  echo "Error: dpkg-deb is required to build the Debian package." >&2
  exit 1
fi

ICON_SOURCE="$REPO_ROOT/Assets/cursor_pace.png"
APPDATA_SOURCE="$REPO_ROOT/packaging/io.github.wsj_br.CursorPace.appdata.xml"
if [[ ! -f "$ICON_SOURCE" ]]; then
  echo "Error: icon not found: $ICON_SOURCE" >&2
  exit 1
fi
if [[ ! -f "$APPDATA_SOURCE" ]]; then
  echo "Error: AppStream metadata not found: $APPDATA_SOURCE" >&2
  exit 1
fi

BUILD_DIR="$REPO_ROOT/.deb-build"
PKG_ROOT="$BUILD_DIR/cursorpace"
OPT_DIR="$PKG_ROOT/opt/CursorPace"
BIN_DIR="$PKG_ROOT/usr/bin"
APP_DIR="$PKG_ROOT/usr/share/applications"
ICON_DIR="$PKG_ROOT/usr/share/icons/hicolor/256x256/apps"
META_DIR="$PKG_ROOT/usr/share/metainfo"
DEBIAN_DIR="$PKG_ROOT/DEBIAN"
mkdir -p "$BUILD_DIR" "$REPO_ROOT/installer"

prepare_deb_icon() {
  local source="$1"
  local dest="$2"
  if command -v magick >/dev/null 2>&1; then
    magick "$source" -resize 256x256! -strip "$dest"
  elif command -v convert >/dev/null 2>&1; then
    convert "$source" -resize 256x256! -strip "$dest"
  else
    echo "Error: ImageMagick (magick or convert) is required to resize the app icon for Debian packaging." >&2
    exit 1
  fi
}

rm -rf "$PKG_ROOT"
mkdir -p "$OPT_DIR" "$BIN_DIR" "$APP_DIR" "$ICON_DIR" "$META_DIR" "$DEBIAN_DIR"
cp -a "$PUBLISH_DIR/." "$OPT_DIR/"
chmod +x "$OPT_DIR/CursorPace"
# Optional .NET diagnostics pull lttng deps that are not needed at runtime.
rm -f "$OPT_DIR/createdump" "$OPT_DIR/libcoreclrtraceptprovider.so"
ln -s /opt/CursorPace/CursorPace "$BIN_DIR/CursorPace"

prepare_deb_icon "$ICON_SOURCE" "$ICON_DIR/cursor-pace.png"
cp "$APPDATA_SOURCE" "$META_DIR/io.github.wsj_br.CursorPace.appdata.xml"

cat >"$APP_DIR/cursor-pace.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=Cursor Pace
Comment=Track Cursor quota across a billing cycle
Exec=/opt/CursorPace/CursorPace
Icon=/usr/share/icons/hicolor/256x256/apps/cursor-pace.png
StartupWMClass=CursorPace
Categories=Utility;
Terminal=false
EOF

cat >"$DEBIAN_DIR/control" <<EOF
Package: cursorpace
Version: $VERSION
Section: utils
Priority: optional
Architecture: $DEB_ARCH
Depends: libc6, libgtk-3-0t64 | libgtk-3-0, libwebkit2gtk-4.1-0t64 | libwebkit2gtk-4.1-0, libsoup-3.0-0t64 | libsoup-3.0-0
Maintainer: Waldemar Scudeller Jr.
Homepage: https://github.com/wsj-br/CursorPace
Description: Track Cursor quota across a billing cycle
 Desktop app that tracks Cursor model quota across a billing cycle.
 Sign in with a Cursor account to pull usage automatically.
EOF

INSTALLER_NAME="CursorPace-$VERSION-$RID.deb"
INSTALLER_PATH="$REPO_ROOT/installer/$INSTALLER_NAME"
rm -f "$INSTALLER_PATH"
dpkg-deb --root-owner-group -Zxz --build "$PKG_ROOT" "$INSTALLER_PATH"

if command -v sha256sum >/dev/null 2>&1; then
  HASH="$(sha256sum "$INSTALLER_PATH" | awk '{print toupper($1)}')"
elif command -v shasum >/dev/null 2>&1; then
  HASH="$(shasum -a 256 "$INSTALLER_PATH" | awk '{print toupper($1)}')"
else
  echo "Error: need sha256sum or shasum to write the Debian package checksum." >&2
  exit 1
fi

HASH_PATH="$INSTALLER_PATH.sha256"
printf '%s  %s\n' "$HASH" "$INSTALLER_NAME" >"$HASH_PATH"

echo "Debian:   $INSTALLER_PATH"
echo "SHA256:   $HASH"
echo "Checksum: $HASH_PATH"
