#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

docker compose -f "$ROOT/infra/compose/docker-compose.$ENVIRONMENT.yml" ps
