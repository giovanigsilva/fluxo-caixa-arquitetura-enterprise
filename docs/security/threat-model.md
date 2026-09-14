# Threat Model Inicial

## Fronteiras

- Browser não recebe tokens de acesso na arquitetura alvo.
- BFF é a fronteira pública.
- APIs internas continuam exigindo identidade/audience/policies na arquitetura alvo.
- `.runtime` é storage local de UAT nesta fatia, não controle de segurança final.

## Abuse cases cobertos parcialmente

- IDOR entre tenants: teste de lookup no BuildingBlocks.
- Reuso de Idempotency-Key com payload diferente: retorna erro de conflito lógico.
- QR reutilizado/expirado: challenge tem expiração e consumo único lógico.
- Formula injection CSV: campos textuais recebem neutralização.

## Riscos residuais

- Sem Keycloak/OIDC real nesta fatia.
- Sem RLS e PostgreSQL real nesta fatia.
- Sem Vault inicializado e auditado.
- Sem E2E e scans. Benchmark k6 local de 50 RPS foi executado, mas nao substitui
  ensaio produtivo com banco real e rede publica.
