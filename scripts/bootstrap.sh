#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

umask 077
mkdir -p "$ROOT/.secrets/$ENVIRONMENT"
for name in postgres_write_password postgres_read_password postgres_platform_password bootstrap_admin_password; do
  if [[ ! -f "$ROOT/.secrets/$ENVIRONMENT/$name" ]]; then
    openssl rand -base64 36 > "$ROOT/.secrets/$ENVIRONMENT/$name"
  fi
  chmod 600 "$ROOT/.secrets/$ENVIRONMENT/$name"
done
echo "bootstrap ok: private files created under .secrets/$ENVIRONMENT"
