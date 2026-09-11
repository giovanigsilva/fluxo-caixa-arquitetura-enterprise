#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
tar -C "$ROOT/.runtime" -czf "$ROOT/artifacts/vertx-$ENVIRONMENT-runtime-$STAMP.tar.gz" "$ENVIRONMENT"
echo "local backup created: artifacts/vertx-$ENVIRONMENT-runtime-$STAMP.tar.gz"
