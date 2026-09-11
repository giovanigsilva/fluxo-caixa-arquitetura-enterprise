# Fluxo de Caixa Vertx

Implementação incremental do sistema `Vertx.CashFlow` no diretório
`teste-pratico-placeholder`.

## Estado atual

Esta entrega contém uma fatia vertical executável:

- BFF ASP.NET Core com proxy fixo e challenge first-party de aprovação por celular.
- Management API com catálogo de permissões, users e roles.
- Entries API com clientes, contas, categorias, centros de custo, lançamentos,
  estorno, idempotência, outbox local e jobs de extrato.
- Outbox Relay, Consolidation Worker e Reports Worker como processos separados.
- Consolidation API lendo read model projetado.
- Observability Simulation API com cenários sintéticos rotulados.
- SPA React/Vite em pt-BR consumindo o BFF.

PostgreSQL/RabbitMQ/Redis/Keycloak/Vault e a stack OTel/Prometheus/Grafana/Loki
estão preparados em artefatos Docker/configuração, mas a fatia validada usa storage
file-backed local em `.runtime` e registra essa limitação em `docs/implementation-status.md`.

## Comandos

```bash
./scripts/bootstrap.sh --env uat
./scripts/build.sh --env uat
./scripts/test.sh --env uat
./scripts/up.sh --env uat
./scripts/smoke.sh --env uat
./scripts/status.sh --env uat
```

URLs locais UAT após `up`:

- SPA: http://127.0.0.1:6230
- BFF: http://127.0.0.1:6210
- OpenAPI BFF: http://127.0.0.1:6210/openapi/v1.json

O grupo Docker visível fica como `teste-pratico`. O diretório real do projeto é
`teste-pratico-placeholder`.

Domínios finais planejados:

- UAT: `uat.vertx.dwilon.com`
- Production: `vertx.dwilon.com`

DNS/TLS/proxy real não foram aplicados automaticamente.
