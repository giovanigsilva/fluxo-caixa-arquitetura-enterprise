#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

BACKUP="${BACKUP:?set BACKUP=path/to/local-backup.tar.gz}"
rm -rf "$ROOT/.runtime/restore-test"
mkdir -p "$ROOT/.runtime/restore-test"
tar -C "$ROOT/.runtime/restore-test" -xzf "$BACKUP"
echo "restore test extracted under .runtime/restore-test"
