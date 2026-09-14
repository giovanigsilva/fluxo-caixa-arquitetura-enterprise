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
LOGIN_EMAIL="${VERTX_LOGIN_EMAIL:-admin@admin.com}"
LOGIN_USER_ID="${VERTX_LOGIN_USER_ID:-user-admin-alpha}"
LOGIN_TOTP_ISSUER="${VERTX_LOGIN_TOTP_ISSUER:-Vertx CashFlow}"
LOGIN_APPROVAL_HOST="${VERTX_LOGIN_APPROVAL_HOST:-vertx.dwilon.com}"
BOOTSTRAP_PASSWORD="$(tr -d '\r\n' < "$ROOT/.secrets/$ENVIRONMENT/bootstrap_admin_password")"
LOGIN_PASSWORD="${VERTX_LOGIN_PASSWORD:-Vtx-$(printf "%s" "$BOOTSTRAP_PASSWORD" | sha256sum | awk '{print substr($1, 1, 24)}')}"
LOGIN_PASSWORD_HASH="$(printf "%s" "$LOGIN_PASSWORD" | sha256sum | awk '{print $1}')"
cat > "$ROOT/.secrets/$ENVIRONMENT/bff.env" <<EOF
VERTX_LOGIN_EMAIL=$LOGIN_EMAIL
VERTX_LOGIN_USER_ID=$LOGIN_USER_ID
VERTX_LOGIN_PASSWORD_SHA256=$LOGIN_PASSWORD_HASH
VERTX_LOGIN_TOTP_ISSUER=$LOGIN_TOTP_ISSUER
VERTX_LOGIN_APPROVAL_HOST=$LOGIN_APPROVAL_HOST
EOF
cat > "$ROOT/.secrets/$ENVIRONMENT/login.txt" <<EOF
login=$LOGIN_EMAIL
senha=$LOGIN_PASSWORD
EOF
chmod 600 "$ROOT/.secrets/$ENVIRONMENT/bff.env" "$ROOT/.secrets/$ENVIRONMENT/login.txt"
echo "bootstrap ok: private files created under .secrets/$ENVIRONMENT"
