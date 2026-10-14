#!/usr/bin/env bash
#
# Tailwind CSS drift guard.
#
# The compiled CSS (src/Weather.WebUI/wwwroot/app.css) is committed so that `dotnet build`
# and the Test Preflight need no Node. This guard fails if that committed file no longer
# matches tailwind.config.js + Styles/tailwind.css, i.e. someone edited the styles or the
# token config without recompiling.
#
# Usage: bash scripts/check-tailwind-css.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEBUI_DIR="$REPO_ROOT/src/Weather.WebUI"
CSS_REL="src/Weather.WebUI/wwwroot/app.css"

if ! command -v npm >/dev/null 2>&1; then
    echo "check-tailwind-css: npm is required but was not found on PATH." >&2
    exit 1
fi

echo "check-tailwind-css: installing pinned toolchain (npm ci)…"
npm --prefix "$WEBUI_DIR" ci

echo "check-tailwind-css: rebuilding the compiled stylesheet…"
npm --prefix "$WEBUI_DIR" run build:css

cd "$REPO_ROOT"
if ! git diff --exit-code -- "$CSS_REL"; then
    echo "" >&2
    echo "check-tailwind-css: $CSS_REL is out of date." >&2
    echo "Run: npm --prefix src/Weather.WebUI run build:css  and commit the result." >&2
    exit 1
fi

echo "check-tailwind-css: $CSS_REL is in sync with the Tailwind config."
