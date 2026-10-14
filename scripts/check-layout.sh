#!/usr/bin/env bash
# Layout guard for the modular monolith.
# Rule: a module may reference another module ONLY through *.IntegrationEvents.
# Forbidden: Weather.Modules.<Other>.{Domain,Application,Infrastructure} referenced from Weather.Modules.<This>.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
violations=0

while IFS= read -r file; do
  this_module="$(basename "$file" | sed -E 's/^Weather\.Modules\.([A-Za-z]+)\..*/\1/')"
  while IFS= read -r ref; do
    [ -z "$ref" ] && continue
    target="$(printf '%s' "$ref" | sed -E 's#.*[\\/]##; s#\.csproj$##')"
    case "$target" in
      Weather.Modules.*)
        other_module="$(printf '%s' "$target" | sed -E 's/^Weather\.Modules\.([A-Za-z]+)\..*/\1/')"
        layer="$(printf '%s' "$target" | sed -E 's/^Weather\.Modules\.[A-Za-z]+\.([A-Za-z]+).*/\1/')"
        if [ "$other_module" != "$this_module" ] && [ "$layer" != "IntegrationEvents" ]; then
          echo "VIOLATION: $(basename "$file") -> $target"
          violations=$((violations + 1))
        fi
        ;;
    esac
  done < <(grep -o 'Include="[^"]*\.csproj"' "$file" | sed 's/Include="//; s/"//')
done < <(find "$root/src/Modules" -name '*.csproj' | sort)

if [ "$violations" -gt 0 ]; then
  echo "FAILED: $violations forbidden cross-module reference(s)"
  exit 1
fi

echo "OK: no forbidden cross-module references"
