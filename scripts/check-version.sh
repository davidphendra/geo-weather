#!/usr/bin/env bash
# Pipeline guard for application versioning.
# Reads the build-info.json produced by the WebUI publish target and fails when the version is
# missing, empty, or a bogus 0.0.0 fallback (no git / shallow clone).
#
# Usage: scripts/check-version.sh <path/to/build-info.json>
set -euo pipefail

info="${1:-}"
if [ -z "$info" ]; then
  echo "usage: $0 <path/to/build-info.json>" >&2
  exit 2
fi

if [ ! -f "$info" ]; then
  echo "FAILED: build-info.json not found at $info" >&2
  exit 1
fi

version="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1])).get("version",""))' "$info")"

if [ -z "$version" ]; then
  echo "FAILED: build-info.json contains no version ($info)" >&2
  exit 1
fi

case "$version" in
  0.0.0*)
    echo "FAILED: no semantic version could be derived (got '$version'); is this a full git clone?" >&2
    exit 1
    ;;
esac

echo "OK: version $version"
