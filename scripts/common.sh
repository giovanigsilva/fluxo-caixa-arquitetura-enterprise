#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

ENVIRONMENT="uat"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)
      ENVIRONMENT="${2:?missing value for --env}"
      shift 2
      ;;
    --help|-h)
      echo "Usage: $0 [--env uat|production]"
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ "$ENVIRONMENT" != "uat" && "$ENVIRONMENT" != "production" ]]; then
  echo "--env must be uat or production" >&2
  exit 2
fi

export DOTNET_CLI_HOME="$ROOT/.runtime/dotnet-home"
export NUGET_PACKAGES="$ROOT/.runtime/nuget"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export COREPACK_HOME="$ROOT/.runtime/corepack"
export PNPM_HOME="$ROOT/.runtime/pnpm-home"
export PNPM_STORE_PATH="$ROOT/.runtime/pnpm-store"
export VERTX_UID="${VERTX_UID:-$(id -u)}"
export VERTX_GID="${VERTX_GID:-$(id -g)}"

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$COREPACK_HOME" "$PNPM_HOME" "$PNPM_STORE_PATH" "$ROOT/.runtime/$ENVIRONMENT" "$ROOT/artifacts"
