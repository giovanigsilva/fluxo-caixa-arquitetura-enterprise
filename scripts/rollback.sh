#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

echo "rollback requires a previously retained publish artifact; no destructive database downgrade is performed"
exit 1
