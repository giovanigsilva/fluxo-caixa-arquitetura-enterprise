# Testes

Executado:

```bash
./scripts/test.sh --env uat
```

Resultado real em 2026-09-11:

- Unit: 6 passed.
- Security: 2 passed.
- Frontend typecheck: passed.
- Smoke UAT: health, lançamento via BFF, consolidação e download CSV passaram.

Pendente: testes de integração com PostgreSQL/RabbitMQ/Redis/Keycloak, E2E
Playwright, k6 50 RPS/10 min, SAST, Gitleaks, SBOM e scan de imagens/IaC.
