#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

"$ROOT/scripts/build.sh" --env "$ENVIRONMENT"
"$ROOT/scripts/up.sh" --env "$ENVIRONMENT"
"$ROOT/scripts/smoke.sh" --env "$ENVIRONMENT"
