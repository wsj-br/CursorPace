#!/usr/bin/env bash
# Show or set the Cursor Pace version.
#
# With no argument, prints the current version from CursorPace.csproj.
# With a version argument, writes that version to CursorPace.csproj and
# the default MyAppVersion in setup.iss.
#
# Usage:
#   ./scripts/version.sh
#   ./scripts/version.sh 0.2.4
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

CSPROJ_PATH="$REPO_ROOT/CursorPace.csproj"
ISS_PATH="$REPO_ROOT/setup.iss"

usage() {
  cat <<'EOF'
Usage: ./scripts/version.sh [version]

  With no argument, print the current version from CursorPace.csproj.
  With x.y.z, set that version in CursorPace.csproj and setup.iss.

  -h, --help    Show this help.
EOF
}

fail() {
  echo "Error: $*" >&2
  exit 1
}

get_csproj_version() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    fail "CursorPace.csproj not found in repository root."
  fi
  local value
  value="$(sed -n 's/^[[:space:]]*<Version>\([^<]*\)<\/Version>[[:space:]]*$/\1/p' "$path" | head -n 1 | tr -d '[:space:]')"
  if [[ -z "$value" ]]; then
    fail "Could not find <Version> in CursorPace.csproj"
  fi
  printf '%s\n' "$value"
}

get_iss_version() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    fail "setup.iss not found in repository root."
  fi
  local value
  value="$(sed -n 's/^#define MyAppVersion "\([^"]*\)".*/\1/p' "$path" | head -n 1 | tr -d '[:space:]')"
  if [[ -z "$value" ]]; then
    fail "Could not find MyAppVersion in setup.iss"
  fi
  printf '%s\n' "$value"
}

is_app_version() {
  [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
}

replace_first_line() {
  local path="$1"
  local pattern="$2"
  local replacement="$3"
  local tmp
  tmp="$(mktemp)"
  if ! PAT="$pattern" REPL="$replacement" awk '
    BEGIN { pat = ENVIRON["PAT"]; repl = ENVIRON["REPL"]; done = 0 }
    {
      if (!done && $0 ~ pat) {
        sub(pat, repl)
        done = 1
      }
      print
    }
    END {
      if (!done) {
        exit 2
      }
    }
  ' "$path" > "$tmp"; then
    rm -f "$tmp"
    return 1
  fi
  mv "$tmp" "$path"
}

REQUESTED=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    -*)
      echo "Error: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
    *)
      if [[ -n "$REQUESTED" ]]; then
        fail "Unexpected extra argument: $1"
      fi
      REQUESTED="$1"
      shift
      ;;
  esac
done

CURRENT="$(get_csproj_version "$CSPROJ_PATH")"
ISS_VERSION="$(get_iss_version "$ISS_PATH")"

if [[ -z "$REQUESTED" ]]; then
  printf '%s\n' "$CURRENT"
  if [[ "$ISS_VERSION" != "$CURRENT" ]]; then
    echo "Warning: setup.iss default MyAppVersion is $ISS_VERSION (expected $CURRENT)." >&2
  fi
  exit 0
fi

if [[ "$REQUESTED" == v* || "$REQUESTED" == V* ]]; then
  REQUESTED="${REQUESTED:1}"
fi

if ! is_app_version "$REQUESTED"; then
  fail "Version must be x.y.z (digits), for example 0.2.4."
fi

if [[ "$CURRENT" == "$REQUESTED" && "$ISS_VERSION" == "$REQUESTED" ]]; then
  printf '%s\n' "$REQUESTED"
  exit 0
fi

if [[ "$CURRENT" != "$REQUESTED" ]]; then
  if ! replace_first_line "$CSPROJ_PATH" '<Version>[^<]+</Version>' "<Version>$REQUESTED</Version>"; then
    fail "Could not update <Version> in CursorPace.csproj"
  fi
fi

if [[ "$ISS_VERSION" != "$REQUESTED" ]]; then
  if ! replace_first_line "$ISS_PATH" '#define MyAppVersion "[^"]+"' "#define MyAppVersion \"$REQUESTED\""; then
    fail "Could not update MyAppVersion in setup.iss"
  fi
fi

printf '%s\n' "$REQUESTED"
if [[ "$CURRENT" != "$REQUESTED" ]]; then
  echo "Updated CursorPace.csproj <Version> $CURRENT -> $REQUESTED" >&2
else
  echo "CursorPace.csproj already $REQUESTED" >&2
fi
if [[ "$ISS_VERSION" != "$REQUESTED" ]]; then
  echo "Updated setup.iss MyAppVersion $ISS_VERSION -> $REQUESTED" >&2
else
  echo "setup.iss already $REQUESTED" >&2
fi
