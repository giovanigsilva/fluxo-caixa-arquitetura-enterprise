# Implementation Status

Atualizado em 2026-09-14.

## Implementado e validado

- Solução .NET 10 `Vertx.CashFlow.slnx` com projetos separados.
- BuildingBlocks com dinheiro canônico, permissões, seed UAT e store file-backed.
- Entries API: cadastros básicos, lançamentos, estorno, idempotência, outbox,
  auditoria e export job.
- Management API: permissions, capabilities, users e roles.
- BFF: proxy fixo, login com senha e Google reCAPTCHA v2 validado server-side.
- Consolidation API/Worker: projeção diária idempotente por entryId.
- Reports Worker: CSV, XLSX OOXML e PDF textual simples.
- Observability Simulation API: cenários sintéticos e SSE.
- SPA React/Vite em pt-BR.
- Build/publish via `scripts/build.sh --env uat`.
- Testes: 8 passaram via `scripts/test.sh --env uat`.
- UAT subiu em Docker Compose `teste-pratico` com 9 containers.
- Smoke UAT criou lançamento real via BFF, projetou consolidado e baixou CSV em
  `artifacts/smoke-statement.csv`.
- SPA respondeu em `http://127.0.0.1:6230`.
- BFF/OpenAPI respondeu em `http://127.0.0.1:6210/openapi/v1.json`.
- Swagger publico respondeu em `https://vertx.dwilon.com/swagger`.
- Manual publico respondeu em `https://vertx.dwilon.com/manual.html`.
- Benchmark k6 50 RPS/10 min passou em UAT local com 30.001 requisicoes,
  0.00% de falhas, p95 807.38 us e p99 1.24 ms.
- PDF arquitetural inicial gerado em
  `docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.pdf`.

## Preparado, não iniciado por padrão

- Compose UAT/Production para processos separados.
- Compose Production validado por `docker compose config`, mas não iniciado.
- Artefatos Docker/configuração para PostgreSQL, Redis, RabbitMQ, Keycloak e Vault.
- Artefatos Docker/configuração para OTel Collector, Prometheus, Grafana e Loki.
- Edge/proxy source em `infra/edge/vertx-hosts.example.conf`.

## Simulado

- `/monitoramento` e Observability Simulation: dados sintéticos rotulados.

## Pendente

- PostgreSQL real com EF/Npgsql, migrações completas, RLS, TLS e bancos separados.
- RabbitMQ real com publisher confirms, routing obrigatório, retry/DLQ e TLS.
- Keycloak real com OIDC Authorization Code + PKCE, MFA/passkey e tema.
- Vault real inicializado fora de dev mode com AppRole/leases.
- CSRF/cookies de sessão reais no BFF.
- E2E Playwright e integração real.
- DNS/TLS/proxy público para `uat.vertx.dwilon.com`.
