#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dalamud_home="${DALAMUD_HOME:-$repo_root/.dalamud}"
dalamud_url="${DALAMUD_DISTRIB_URL:-https://goatcorp.github.io/dalamud-distrib/stg/latest.zip}"
configuration="${CONFIGURATION:-Release}"
project="${PROJECT:-$repo_root/OnePiece/OnePiece.csproj}"

if [ ! -f "$dalamud_home/Dalamud.dll" ]; then
  echo "Downloading Dalamud SDK to $dalamud_home"
  mkdir -p "$dalamud_home"

  tmp_zip="$(mktemp)"
  trap 'rm -f "$tmp_zip"' EXIT

  curl -fsSL "$dalamud_url" -o "$tmp_zip"
  unzip -q -o "$tmp_zip" -d "$dalamud_home"
fi

echo "Building with DALAMUD_HOME=$dalamud_home"
DALAMUD_HOME="$dalamud_home" dotnet build "$project" -c "$configuration" "$@"
