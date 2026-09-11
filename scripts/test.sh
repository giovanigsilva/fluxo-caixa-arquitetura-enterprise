#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

cd "$ROOT"
dotnet test tests/Unit/Vertx.CashFlow.UnitTests/Vertx.CashFlow.UnitTests.csproj --no-restore -v minimal
dotnet test tests/Security/Vertx.CashFlow.SecurityTests/Vertx.CashFlow.SecurityTests.csproj --no-restore -v minimal
cd "$ROOT/apps/web"
corepack pnpm lint
