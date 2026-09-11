#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

REPORT="$ROOT/docs/operations/preflight-$ENVIRONMENT.md"
{
  echo "# Preflight $ENVIRONMENT"
  echo
  echo "- timestamp_utc: $(date -u +%FT%TZ)"
  echo "- kernel: $(uname -srmo)"
  echo "- dotnet: $(dotnet --version)"
  echo "- node: $(node --version)"
  echo "- docker: $(docker version --format '{{.Server.Version}}' 2>/dev/null || echo unavailable)"
  echo "- compose: $(docker compose version 2>/dev/null || echo unavailable)"
  echo
  echo "## Disk"
  df -h / /data /tmp 2>/dev/null | sed 's/^/- /'
  echo
  echo "## DNS"
  echo "- vertx.dwilon.com: $(getent hosts vertx.dwilon.com | awk '{print $1}' | paste -sd ',' - || true)"
  echo "- uat.vertx.dwilon.com: $(getent hosts uat.vertx.dwilon.com | awk '{print $1}' | paste -sd ',' - || true)"
} > "$REPORT"
echo "preflight report: $REPORT"
