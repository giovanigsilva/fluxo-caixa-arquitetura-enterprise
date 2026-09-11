#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

mkdir -p "$ROOT/.runtime/$ENVIRONMENT"
echo "{\"schemaVersion\":1,\"environment\":\"$ENVIRONMENT\",\"note\":\"file-backed state initialized; PostgreSQL migrations pending\"}" > "$ROOT/.runtime/$ENVIRONMENT/migration-status.json"
echo "migration status recorded for $ENVIRONMENT"
