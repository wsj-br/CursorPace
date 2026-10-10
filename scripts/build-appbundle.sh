#!/usr/bin/env bash
# Build a macOS .app bundle and wrap it in a DMG from a self-contained publish folder.
#
# Usage:
#   ./scripts/build-appbundle.sh --version 0.x.y --rid osx-arm64 --publish-dir bin/Release/net10.0/osx-arm64/publish
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

VERSION=""
RID=""
PUBLISH_DIR=""

usage() {
  cat <<'EOF'
Usage: ./scripts/build-appbundle.sh --version VERSION --rid RID --publish-dir PATH

  --version       App version (matches CursorPace.csproj).
  --rid           osx-x64 or osx-arm64.
  --publish-dir   Self-contained publish output directory.
  -h, --help      Show this help.

Must run on macOS. Writes installer/CursorPace-<version>-<rid>.dmg and a .sha256 file.
The disk image opens with CursorPace.app on the left, an arrow, and Applications on the right.
The first run installs dmgbuild into .appbundle-build/ (no Finder automation).
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

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "Error: build-appbundle.sh must run on macOS." >&2
  exit 1
fi

if [[ -z "$VERSION" || -z "$RID" || -z "$PUBLISH_DIR" ]]; then
  echo "Error: --version, --rid, and --publish-dir are required." >&2
  usage >&2
  exit 1
fi

case "$RID" in
  osx-x64|osx-arm64) ;;
  *)
    echo "Error: unsupported --rid '$RID' (use osx-x64 or osx-arm64)." >&2
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

ICON_PNG="$REPO_ROOT/Assets/cursor_pace.png"
if [[ ! -f "$ICON_PNG" ]]; then
  echo "Error: icon not found: $ICON_PNG" >&2
  exit 1
fi

BUILD_DIR="$REPO_ROOT/.appbundle-build"
BUNDLE_NAME="CursorPace.app"
BUNDLE_DIR="$BUILD_DIR/$BUNDLE_NAME"
MACOS_DIR="$BUNDLE_DIR/Contents/MacOS"
RESOURCES_DIR="$BUNDLE_DIR/Contents/Resources"
mkdir -p "$BUILD_DIR" "$REPO_ROOT/installer"

rm -rf "$BUNDLE_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"
cp -a "$PUBLISH_DIR/." "$MACOS_DIR/"
chmod +x "$MACOS_DIR/CursorPace"

ICON_BASENAME="cursor_pace"
if command -v iconutil >/dev/null 2>&1 && command -v sips >/dev/null 2>&1; then
  ICONSET="$BUILD_DIR/$ICON_BASENAME.iconset"
  rm -rf "$ICONSET"
  mkdir -p "$ICONSET"
  sips -z 16 16 "$ICON_PNG" --out "$ICONSET/icon_16x16.png" >/dev/null
  sips -z 32 32 "$ICON_PNG" --out "$ICONSET/icon_16x16@2x.png" >/dev/null
  sips -z 32 32 "$ICON_PNG" --out "$ICONSET/icon_32x32.png" >/dev/null
  sips -z 64 64 "$ICON_PNG" --out "$ICONSET/icon_32x32@2x.png" >/dev/null
  sips -z 128 128 "$ICON_PNG" --out "$ICONSET/icon_128x128.png" >/dev/null
  sips -z 256 256 "$ICON_PNG" --out "$ICONSET/icon_128x128@2x.png" >/dev/null
  sips -z 256 256 "$ICON_PNG" --out "$ICONSET/icon_256x256.png" >/dev/null
  sips -z 512 512 "$ICON_PNG" --out "$ICONSET/icon_256x256@2x.png" >/dev/null
  sips -z 512 512 "$ICON_PNG" --out "$ICONSET/icon_512x512.png" >/dev/null
  sips -z 1024 1024 "$ICON_PNG" --out "$ICONSET/icon_512x512@2x.png" >/dev/null
  iconutil -c icns "$ICONSET" -o "$RESOURCES_DIR/$ICON_BASENAME.icns"
else
  cp "$ICON_PNG" "$RESOURCES_DIR/$ICON_BASENAME.png"
fi

cat >"$BUNDLE_DIR/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>CursorPace</string>
  <key>CFBundleDisplayName</key>
  <string>CursorPace</string>
  <key>CFBundleIdentifier</key>
  <string>com.cursorpace.app</string>
  <key>CFBundleVersion</key>
  <string>$VERSION</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleSignature</key>
  <string>????</string>
  <key>CFBundleExecutable</key>
  <string>CursorPace</string>
  <key>CFBundleIconFile</key>
  <string>$ICON_BASENAME</string>
  <key>CFBundleIconName</key>
  <string>$ICON_BASENAME</string>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
EOF

if ! command -v python3 >/dev/null 2>&1; then
  echo "Error: python3 is required to lay out the macOS DMG." >&2
  exit 1
fi

VOLUME_NAME="CursorPace"
# Finder draws a disk-image background only from a TIFF. This is the 540x380
# arrow used by the default electron-builder DMG (the Transrewrt layout):
# 80px icons, app at (130, 220), Applications at (410, 220).
BACKGROUND_TIFF="$REPO_ROOT/packaging/dmg-background.tiff"
if [[ ! -f "$BACKGROUND_TIFF" ]]; then
  echo "Error: DMG background not found: $BACKGROUND_TIFF" >&2
  exit 1
fi

if [[ -d "/Volumes/$VOLUME_NAME" ]]; then
  echo "Error: /Volumes/$VOLUME_NAME is already mounted. Unmount it and retry." >&2
  exit 1
fi

DMGBUILD_VERSION="1.6.5"
PY_TARGET="$BUILD_DIR/py"
if ! PYTHONPATH="$PY_TARGET" python3 -c 'import dmgbuild' >/dev/null 2>&1; then
  echo "Installing dmgbuild $DMGBUILD_VERSION ..."
  pip_args=(install --disable-pip-version-check --target "$PY_TARGET" "dmgbuild==$DMGBUILD_VERSION")
  if python3 -m pip install --help 2>/dev/null | grep -q -- '--break-system-packages'; then
    pip_args+=(--break-system-packages)
  fi
  python3 -m pip "${pip_args[@]}"
fi

SETTINGS="$BUILD_DIR/dmg-settings.py"
python3 - "$SETTINGS" "$BUNDLE_DIR" "$BACKGROUND_TIFF" <<'PY'
import sys

settings_path, app_path, background_path = sys.argv[1:]
with open(settings_path, "w", encoding="utf-8") as handle:
    handle.write(
        "files = [(%r, 'CursorPace.app')]\n"
        "symlinks = {'Applications': '/Applications'}\n"
        "icon_locations = {\n"
        "    'CursorPace.app': (130, 220),\n"
        "    'Applications': (410, 220),\n"
        "}\n"
        "background = %r\n"
        "window_rect = ((200, 120), (540, 380))\n"
        "icon_size = 80\n"
        "text_size = 12\n"
        "format = 'UDZO'\n"
        "filesystem = 'HFS+'\n" % (app_path, background_path)
    )
PY

ARCHIVE_NAME="CursorPace-$VERSION-$RID.dmg"
ARCHIVE_PATH="$REPO_ROOT/installer/$ARCHIVE_NAME"
rm -f "$ARCHIVE_PATH"
# dmgbuild 1.6 also writes a pBBk bookmark. Finder on this macOS then ignores
# the background picture (the arrow) and shows the system window color instead.
# Skip that record so only the alias remains, matching a working Transrewrt DMG.
PYTHONPATH="$PY_TARGET${PYTHONPATH:+:$PYTHONPATH}" python3 - "$SETTINGS" "$VOLUME_NAME" "$ARCHIVE_PATH" <<'PY'
import sys

import dmgbuild
import ds_store.store as store

settings_path, volume_name, output_path = sys.argv[1:]
original = store.DSStore.Partial.__setitem__


def skip_background_bookmark(self, code, value):
    if code in ("pBBk", b"pBBk"):
        return
    original(self, code, value)


store.DSStore.Partial.__setitem__ = skip_background_bookmark
dmgbuild.build_dmg(output_path, volume_name, settings_path, lookForHiDPI=False)
PY

if command -v shasum >/dev/null 2>&1; then
  HASH="$(shasum -a 256 "$ARCHIVE_PATH" | awk '{print toupper($1)}')"
elif command -v sha256sum >/dev/null 2>&1; then
  HASH="$(sha256sum "$ARCHIVE_PATH" | awk '{print toupper($1)}')"
else
  echo "Error: need shasum or sha256sum to write the bundle checksum." >&2
  exit 1
fi

HASH_PATH="$ARCHIVE_PATH.sha256"
printf '%s  %s\n' "$HASH" "$ARCHIVE_NAME" >"$HASH_PATH"

echo "App bundle: $BUNDLE_DIR"
echo "DMG:        $ARCHIVE_PATH"
echo "SHA256:     $HASH"
echo "Checksum:   $HASH_PATH"
