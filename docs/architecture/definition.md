# Definição Arquitetura Software Fluxo de Caixa

## Objetivo

Sistema B2B para registrar lançamentos financeiros, preservar ledger imutável,
projetar consolidado diário, gerar extratos e administrar tenants, usuários,
roles e permissões.

## Fatia executável atual

O commit local implementa uma fatia vertical em processos separados. A escrita de
lançamento persiste o lançamento, auditoria, idempotência e outbox local antes de
responder. O consolidado é atualizado por worker separado. Com o worker parado, a
Entries API continua aceitando writes enquanto houver armazenamento local.

## Containers

- React SPA: interface, sem acesso direto a storage.
- BFF: proxy fixo, sessão/approval boundary inicial.
- Management API: usuários, roles e permissões.
- Entries API: cadastros financeiros, lançamentos, estornos, export jobs e outbox.
- Outbox Relay: publica a fronteira local de eventos.
- Consolidation Worker: projeta saldos diários.
- Consolidation API: consulta read model.
- Reports Worker: gera PDF/XLSX/CSV.
- Observability Simulation: cenários sintéticos.

## Trade-offs

- Storage file-backed foi usado para validar a fatia em tempo curto e isolado.
  Ganho: execução local sem tocar serviços Dwilon. Custo: não substitui PostgreSQL,
  transações reais, RLS, pooling ou concorrência multi-réplica.
- Observabilidade real fica preparada, mas desabilitada. Ganho: atende a restrição
  do pedido. Custo: painéis sintéticos não são evidência medida.
- Login com senha e Google reCAPTCHA v2 foi implementado sem Keycloak real.
  Ganho: prova publica de protecao antirrobo na entrada web. Custo: falta
  OIDC/MFA/passkey real antes de producao e QR Code Fraud Defense depende de
  liberacao externa da Google.

## Evidência

Evidencias reais ficam em `docs/implementation-status.md`,
`docs/requirements-traceability.md` e nos outputs de `scripts/build.sh`,
`scripts/test.sh`, `scripts/up.sh` e `scripts/smoke.sh`.
