# APIs

OpenAPI JSON é exposto por serviço em `/openapi/v1.json` na fatia local.

- BFF: `http://127.0.0.1:6210/openapi/v1.json`
- Entries: interno no Compose.
- Management: interno no Compose.
- Consolidation: interno no Compose.
- Observability Simulation: interno no Compose.

Headers usados na fatia UAT local:

- `X-Tenant-Id`: tenant selecionado pelo BFF.
- `X-User-Id`: usuário de aplicação.
- `Idempotency-Key`: obrigatório para commands financeiros.

Esses headers não são autorização confiável em produção; a versão final precisa de
sessão BFF aprovada, OIDC, CSRF e policies por endpoint.
