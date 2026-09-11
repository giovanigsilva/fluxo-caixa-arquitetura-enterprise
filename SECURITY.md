# Segurança

Não registrar credenciais, tokens, cookies, QR secrets, payload financeiro completo
ou dados pessoais reais em logs, documentação, Swagger, fixtures ou relatórios.

## Implementado nesta fatia

- `Idempotency-Key` obrigatório para lançamentos e estornos.
- Dinheiro como string decimal canônica e `decimal` no backend.
- Separação de tenants no modelo e teste negativo de IDOR em BuildingBlocks.
- Estorno por lançamento inverso; lançamento confirmado não é editado.
- Outbox e projeção diária idempotente por entryId.
- Jobs de exportação com arquivo fora do webroot em `.runtime`.
- CSV com neutralização básica de formula injection em campos textuais.
- Challenge de aprovação por segundo dispositivo com segredo de 256 bits em hash,
  expiração de 120 segundos e consumo por POST.

## Pendente antes de produção real

- OIDC real com Keycloak, cookies HttpOnly/Secure, CSRF e revogação server-side.
- Persistência real em PostgreSQL separado, RLS, TLS interno e mTLS.
- RabbitMQ real com confirms, DLQ e credenciais mínimas.
- Vault bootstrap real, unseal custody, AppRole/leases e auditoria.
- Scans SAST/dependências/imagens/IaC e Gitleaks.
- Benchmark k6 de 50 RPS por 10 minutos em UAT isolada.
- DNS/TLS/proxy final e validação pelos hostnames públicos.
