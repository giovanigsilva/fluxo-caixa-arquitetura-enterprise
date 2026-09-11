#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" --env uat

rm -f "$ROOT/.runtime/uat/cashflow-state.json"
echo "UAT seed will be materialized by the first API read/write."
