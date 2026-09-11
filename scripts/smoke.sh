#!/usr/bin/env bash
source "$(dirname "${BASH_SOURCE[0]}")/common.sh" "$@"

BASE="${BASE:-http://127.0.0.1:6210}"
TENANT="org-alpha"
USER_ID="user-admin-alpha"

curl -fsS "$BASE/health/ready" >/dev/null
ACCOUNT_ID="$(curl -fsS -H "X-Tenant-Id: $TENANT" -H "X-User-Id: $USER_ID" "$BASE/api/entries/accounts" | python3 -c 'import json,sys; print(json.load(sys.stdin)[0]["id"])')"
ENTRY_ID="$(curl -fsS -X POST "$BASE/api/entries/entries" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  -H "Idempotency-Key: smoke-$(date +%s%N)" \
  --data "{\"accountId\":\"$ACCOUNT_ID\",\"type\":\"Credit\",\"amount\":\"150.00\",\"businessDate\":\"$(date -I)\",\"description\":\"Smoke UAT\",\"customerId\":null,\"categoryId\":null,\"costCenterId\":null}" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')"
sleep 4
curl -fsS -H "X-Tenant-Id: $TENANT" -H "X-User-Id: $USER_ID" "$BASE/api/consolidated/daily" >/dev/null
JOB_ID="$(curl -fsS -X POST "$BASE/api/entries/statements/exports" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  --data "{\"format\":\"csv\",\"from\":\"$(date -I)\",\"to\":\"$(date -I)\",\"accountId\":\"$ACCOUNT_ID\"}" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')"
sleep 3
curl -fsS -H "X-Tenant-Id: $TENANT" -H "X-User-Id: $USER_ID" "$BASE/api/entries/statements/exports/$JOB_ID/download" -o "$ROOT/artifacts/smoke-statement.csv"
echo "smoke ok: entry=$ENTRY_ID job=$JOB_ID artifact=artifacts/smoke-statement.csv"
