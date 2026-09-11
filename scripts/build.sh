#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

cd "$ROOT"
while IFS= read -r project; do
  dotnet restore "$project"
done < <(find src tests -name '*.csproj' | sort)

publish() {
  local name="$1"
  local project="$2"
  local out="$ROOT/.runtime/publish/$name"
  rm -rf "$out"
  dotnet publish "$project" -c Release -o "$out" --no-restore
}

publish bff src/Bff/Vertx.CashFlow.Bff/Vertx.CashFlow.Bff.csproj
publish management-api src/Management/Vertx.CashFlow.Management.Api/Vertx.CashFlow.Management.Api.csproj
publish entries-api src/Entries/Vertx.CashFlow.Entries.Api/Vertx.CashFlow.Entries.Api.csproj
publish consolidation-api src/Consolidation/Vertx.CashFlow.Consolidation.Api/Vertx.CashFlow.Consolidation.Api.csproj
publish observability-api src/ObservabilitySimulation/Vertx.CashFlow.ObservabilitySimulation.Api/Vertx.CashFlow.ObservabilitySimulation.Api.csproj
publish outbox-relay src/OutboxRelay/Vertx.CashFlow.OutboxRelay/Vertx.CashFlow.OutboxRelay.csproj
publish consolidation-worker src/ConsolidationWorker/Vertx.CashFlow.ConsolidationWorker/Vertx.CashFlow.ConsolidationWorker.csproj
publish reports-worker src/ReportsWorker/Vertx.CashFlow.ReportsWorker/Vertx.CashFlow.ReportsWorker.csproj

cd "$ROOT/apps/web"
corepack pnpm install --frozen-lockfile
corepack pnpm build
