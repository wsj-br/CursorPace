#!/usr/bin/env bash
# Update NuGet PackageReference versions and refresh lock files.
#
# Usage:
#   ./scripts/update-packages.sh
#   ./scripts/update-packages.sh --list
#   ./scripts/update-packages.sh --vulnerable
#   ./scripts/update-packages.sh --test
#   ./scripts/update-packages.sh xunit
#   ./scripts/update-packages.sh Avalonia@12.1.2
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

SOLUTION_PATH="$REPO_ROOT/CursorPace.slnx"
APP_PROJECT="$REPO_ROOT/CursorPace.csproj"
TEST_PROJECT="$REPO_ROOT/Tests/CursorPace.Tests.csproj"

LIST=0
VULNERABLE=0
RUN_TESTS=0
PACKAGES=()
PROJECT_UPDATED=0

AVALONIA_ALIGNED=(
  Avalonia
  Avalonia.Desktop
  Avalonia.Themes.Fluent
  Avalonia.Fonts.Inter
)

usage() {
  cat <<'EOF'
Usage: ./scripts/update-packages.sh [options] [package[@version] ...]

  With no package list, update every direct PackageReference in the app
  and test projects to the highest version on the configured sources.
  Named packages are updated only in the project that references them.

  --list, -l          List outdated packages and exit (no writes).
  --vulnerable        Lift only packages NuGet Audit reports, to the
                      lowest safe version. With --list, show vulnerable
                      packages instead of outdated.
  --test, -t          Run unit tests after the restore.
  -h, --help          Show this help.

dotnet package update cannot take the .slnx (two projects). This script
updates CursorPace.csproj then Tests/CursorPace.Tests.csproj, then
restores with --force-evaluate so both lock files stay in sync.

Keep Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, and
Avalonia.Fonts.Inter on the same version. Do not force
Avalonia.Controls.WebView to a version that is not on nuget.org.
EOF
}

fail() {
  echo "Error: $*" >&2
  exit 1
}

require_dotnet() {
  if ! command -v dotnet >/dev/null 2>&1; then
    fail "dotnet is not on PATH. Install the .NET 10 SDK."
  fi
}

package_id() {
  local spec="$1"
  printf '%s\n' "${spec%%@*}"
}

project_has_package() {
  local project="$1"
  local id="$2"
  grep -Fq "<PackageReference Include=\"${id}\"" "$project"
}

package_version() {
  local project="$1"
  local id="$2"
  sed -n "s/.*<PackageReference Include=\"${id}\" Version=\"\\([^\"]*\\)\".*/\\1/p" "$project" | head -n 1
}

matching_specs() {
  local project="$1"
  local spec id
  for spec in "${PACKAGES[@]}"; do
    id="$(package_id "$spec")"
    if project_has_package "$project" "$id"; then
      printf '%s\n' "$spec"
    fi
  done
}

update_project() {
  local project="$1"
  local relative="$2"
  local -a args=(package update --project "$project")
  local -a matching=()
  local spec

  PROJECT_UPDATED=0

  if [[ "$VULNERABLE" -eq 1 ]]; then
    args+=(--vulnerable)
  fi

  if [[ ${#PACKAGES[@]} -gt 0 ]]; then
    while IFS= read -r spec; do
      [[ -n "$spec" ]] && matching+=("$spec")
    done < <(matching_specs "$project")
    if [[ ${#matching[@]} -eq 0 ]]; then
      echo "Skipping $relative (no matching PackageReference)."
      return 0
    fi
    args+=("${matching[@]}")
  fi

  echo "Updating $relative..."
  dotnet "${args[@]}"
  PROJECT_UPDATED=1
}

check_avalonia_alignment() {
  local id version first="" mismatched=0
  echo "Checking Avalonia package alignment..."
  for id in "${AVALONIA_ALIGNED[@]}"; do
    version="$(package_version "$APP_PROJECT" "$id")"
    if [[ -z "$version" ]]; then
      fail "Could not read Version for $id in CursorPace.csproj."
    fi
    echo "  $id $version"
    if [[ -z "$first" ]]; then
      first="$version"
    elif [[ "$version" != "$first" ]]; then
      mismatched=1
    fi
  done
  if [[ "$mismatched" -eq 1 ]]; then
    echo "Error: Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, and Avalonia.Fonts.Inter must share one version." >&2
    echo "Pin with: ./scripts/update-packages.sh Avalonia@$first Avalonia.Desktop@$first Avalonia.Themes.Fluent@$first Avalonia.Fonts.Inter@$first" >&2
    exit 1
  fi
  local webview
  webview="$(package_version "$APP_PROJECT" "Avalonia.Controls.WebView")"
  if [[ -n "$webview" ]]; then
    echo "  Avalonia.Controls.WebView $webview (may lag the Avalonia line; do not force a version that is not on nuget.org)"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --list|-l)
      LIST=1
      shift
      ;;
    --vulnerable)
      VULNERABLE=1
      shift
      ;;
    --test|-t)
      RUN_TESTS=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    --)
      shift
      PACKAGES+=("$@")
      break
      ;;
    -*)
      echo "Error: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
    *)
      PACKAGES+=("$1")
      shift
      ;;
  esac
done

if [[ ! -f "$SOLUTION_PATH" || ! -f "$APP_PROJECT" || ! -f "$TEST_PROJECT" ]]; then
  fail "Expected CursorPace.slnx, CursorPace.csproj, and Tests/CursorPace.Tests.csproj in the repository."
fi

require_dotnet

if [[ "$LIST" -eq 1 ]]; then
  if [[ ${#PACKAGES[@]} -gt 0 ]]; then
    fail "--list does not take package names."
  fi
  if [[ "$RUN_TESTS" -eq 1 ]]; then
    fail "--list cannot be combined with --test."
  fi
  if [[ "$VULNERABLE" -eq 1 ]]; then
    echo "Listing vulnerable NuGet packages..."
    exec dotnet list "$SOLUTION_PATH" package --vulnerable
  fi
  echo "Listing outdated NuGet packages..."
  exec dotnet list "$SOLUTION_PATH" package --outdated
fi

ANY_UPDATED=0
APP_TOUCHED=0

update_project "$APP_PROJECT" "CursorPace.csproj"
if [[ "$PROJECT_UPDATED" -eq 1 ]]; then
  ANY_UPDATED=1
  APP_TOUCHED=1
fi

update_project "$TEST_PROJECT" "Tests/CursorPace.Tests.csproj"
if [[ "$PROJECT_UPDATED" -eq 1 ]]; then
  ANY_UPDATED=1
fi

if [[ "$ANY_UPDATED" -eq 0 ]]; then
  fail "None of the named packages are referenced by CursorPace.csproj or Tests/CursorPace.Tests.csproj."
fi

if [[ "$APP_TOUCHED" -eq 1 ]]; then
  check_avalonia_alignment
fi

echo "Restoring lock files..."
dotnet restore "$SOLUTION_PATH" --force-evaluate

if [[ "$RUN_TESTS" -eq 1 ]]; then
  echo "Running tests..."
  dotnet test ./Tests/CursorPace.Tests.csproj
fi

echo "Commit CursorPace.csproj / Tests/CursorPace.Tests.csproj with packages.lock.json and Tests/packages.lock.json."
echo "Log the bump under ## [Unreleased] in dev/CHANGELOG.md."
