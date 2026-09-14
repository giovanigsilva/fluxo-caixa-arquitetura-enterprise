# Testes

Executado:

```bash
./scripts/test.sh --env uat
```

Resultado real em 2026-09-14:

- Unit: 6 passed.
- Security: 2 passed.
- Frontend typecheck: passed.
- Smoke UAT: health, lançamento via BFF, consolidação e download CSV passaram.
- Performance k6 50 RPS/10 min: passou com 30.001 requisições HTTP, 0.00% de
  falhas, p95 de 807.38 us e p99 de 1.24 ms.

Evidencia detalhada:

- `docs/testing/k6-consolidated-50rps-2026-09-14.md`

Pendente: testes de integração com PostgreSQL/RabbitMQ/Redis/Keycloak, E2E
Playwright, SAST, Gitleaks, SBOM e scan de imagens/IaC.
