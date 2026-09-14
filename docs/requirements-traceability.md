# Matriz De Aderencia Ao Desafio

Fonte analisada: `desafio-arquiteto-software-jan25.pdf`.

Esta matriz cruza os requisitos do desafio com as evidencias existentes no
repositorio. Ela separa implementacao executavel, decisao arquitetural,
documentacao e pendencias objetivas.

## Resultado Geral

Status: atende o nucleo obrigatorio do desafio, com ressalva para duas evidencias
operacionais externas ao codigo:

- os commits locais ainda precisam ser enviados ao GitHub publico;
- o teste formal de 50 RPS com ate 5% de falha esta scriptado, mas nao foi
  executado porque `k6` nao esta instalado neste ambiente.

## Requisitos De Negocio

| Requisito | Status | Evidencia |
| --- | --- | --- |
| Servico que faca controle de lancamentos | Atendido | `src/Entries/Vertx.CashFlow.Entries.Api/Program.cs`; Swagger `/api/entries/entries`; SPA com formulario de credito/debito. |
| Lancamentos de debito e credito | Atendido | `PostEntryRequest` aceita `type`, `amount`, `businessDate`, `description`, `accountId` e cliente opcional. |
| Servico do consolidado diario | Atendido | `src/Consolidation/Vertx.CashFlow.Consolidation.Api/Program.cs`; endpoint `/api/consolidated/daily`. |
| Relatorio com saldo diario consolidado | Atendido | `ReportsWorker` gera extrato CSV, XLSX e PDF; smoke baixa `artifacts/smoke-statement.csv`. |

## Requisitos Tecnicos Obrigatorios

| Requisito | Status | Evidencia |
| --- | --- | --- |
| Desenho da solucao | Atendido | `docs/architecture/definition.md`, `docs/c4/context.puml`, `docs/c4/containers.puml`, `docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.pdf`. |
| Deve ser feito usando C# | Atendido | Solucao `Vertx.CashFlow.slnx` e projetos `.csproj` em `src/`. |
| Testes | Atendido | `tests/Unit`, `tests/Security`, `scripts/test.sh --env uat`; execucao atual: 6 unitarios, 2 seguranca, frontend typecheck. |
| Boas praticas | Atendido | Separacao por servicos, BFF, outbox, idempotencia, audit trail, DTOs, validacoes, permissions, scripts e Docker Compose. |
| README claro | Atendido | `README.md` com funcionamento, comandos locais, endpoints, exemplos curl e operacao. |
| Repositorio publico GitHub | Parcial ate push | Repositorio remoto existe, mas branch local esta `ahead` e `git push` falha por falta de credencial HTTPS. |
| Todas as documentacoes no repositorio | Atendido localmente | `MANUAL-DO-EXAMINADOR.md`, `docs/manual-de-uso.md`, docs de arquitetura, seguranca, testes, operacao e exports. Fica pendente aparecer no GitHub ate o push. |

## Requisitos Nao Funcionais

| Requisito | Status | Evidencia |
| --- | --- | --- |
| Controle de lancamentos nao deve cair se consolidado cair | Atendido por arquitetura e codigo | Entries API grava em storage/outbox antes de responder; Consolidation Worker e Consolidation API rodam em containers separados. |
| Pico de 50 req/s no consolidado | Parcial | Existe `tests/performance/consolidated-50rps.k6.js` com `rate: 50` por 10 minutos. |
| No maximo 5% de perda | Parcial | O script k6 tem threshold `http_req_failed: rate<=0.05`, mas o benchmark formal nao foi executado neste ambiente. |
| Escalabilidade | Atendido em desenho/preparacao | Compose separa BFF, APIs e workers; infra preparada para PostgreSQL, RabbitMQ, Redis e observabilidade. |
| Resiliencia | Atendido em desenho/parcial executavel | Outbox, worker separado, rebuild de read model, health checks e restart policy; failover real ainda depende da stack production. |
| Seguranca | Atendido na fatia e documentado para alvo | BFF publico, login + reCAPTCHA server-side, secrets fora do Git, containers restritos; Keycloak/Vault/RLS ficam como evolucao preparada. |
| Desempenho | Parcial | API e smoke funcionais; benchmark 50 RPS formal ainda pendente. |
| Monitoramento proativo | Parcial | Observability Simulation API e dashboard sintetico; stack OTel/Prometheus/Grafana/Loki preparada, nao ativa por padrao. |

## Evidencias Validadas Nesta Auditoria

- `./scripts/test.sh --env uat`: passou com 6 testes unitarios, 2 testes de
  seguranca e typecheck frontend.
- `./scripts/smoke.sh --env uat`: criou lancamento real, projetou consolidado e
  baixou CSV.
- `docker compose -f infra/compose/docker-compose.uat.yml config --quiet`: OK.
- `docker compose -f infra/compose/docker-compose.production.yml config --quiet`: OK.
- `curl -fsS http://127.0.0.1:6210/health/ready`: retornou `ready`.
- `https://vertx.dwilon.com/swagger`: retornou HTML do Swagger via GET.
- `https://vertx.dwilon.com/openapi/v1.json`: lista os paths publicos do BFF.

## Pendencias Para Ficar Sem Ressalva

1. Configurar credencial GitHub HTTPS ou trocar o remoto para SSH e executar
   `git push origin main`.
2. Instalar `k6` ou usar imagem Docker equivalente e executar:

```bash
BASE_URL=http://127.0.0.1:6210 k6 run tests/performance/consolidated-50rps.k6.js
```

3. Atualizar `docs/testing/README.md` com o resultado real do k6 quando o
   benchmark for executado.
4. Para producao real, substituir storage file-backed por PostgreSQL/RLS/TLS e
   ativar RabbitMQ/Redis/Keycloak/Vault conforme arquitetura documentada.

