#!/usr/bin/env bash
# Release guard: the published artifact must never contain runtime SQLite data (ADR-0013).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
out="$(mktemp -d)"
trap 'rm -rf "$out"' EXIT

dotnet publish "$root/src/Weather.WebUI/Weather.WebUI.csproj" --configuration Release --nologo --output "$out" >/dev/null

matches="$(find "$out" \( -iname '*.db' -o -iname '*.db-shm' -o -iname '*.db-wal' -o -type d -iname 'App_Data' \) -print)"

if [ -n "$matches" ]; then
  echo "FAILED: runtime data found in the published artifact:"
  printf '%s\n' "$matches"
  exit 1
fi

echo "OK: no runtime SQLite data in the published artifact"
