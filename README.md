# Fluxo de Caixa Vertx

Implementação incremental do `Vertx.CashFlow`, uma base enterprise para API,
BFF, frontend, workers e documentação operacional de um sistema de fluxo de
caixa multi-tenant.

Este repositório já sobe uma fatia vertical executável em Docker Compose:
cadastros, lançamentos, estorno, idempotência, outbox, consolidação diária,
exportação de extrato, simulação de observabilidade e SPA em pt-BR.

## Sumário

- [Estado atual](#estado-atual)
- [Arquitetura em execução](#arquitetura-em-execucao)
- [Pré-requisitos](#pre-requisitos)
- [Subir a API em UAT](#subir-a-api-em-uat)
- [Testar a API](#testar-a-api)
- [Endpoints principais](#endpoints-principais)
- [Exemplos com curl](#exemplos-com-curl)
- [Ambientes e portas](#ambientes-e-portas)
- [Persistência, secrets e arquivos locais](#persistencia-secrets-e-arquivos-locais)
- [Publicação Cloudflare](#publicacao-cloudflare)
- [Operação diária](#operacao-diaria)
- [Troubleshooting](#troubleshooting)
- [Limites conhecidos](#limites-conhecidos)

## Estado atual

O projeto está pronto para demonstração técnica e UAT local/publicado. A entrega
inclui:

- BFF ASP.NET Core com proxy para os serviços internos.
- Management API com catálogo de permissões, usuários e perfis.
- Entries API com clientes, contas, categorias, centros de custo, lançamentos,
  estorno, idempotência, outbox local e jobs de extrato.
- Consolidation API lendo o read model projetado.
- Outbox Relay como processo separado.
- Consolidation Worker projetando eventos publicados.
- Reports Worker gerando CSV, XLSX e PDF.
- Observability Simulation API com cenários sintéticos claramente rotulados.
- SPA React/Vite em português do Brasil consumindo o BFF.
- Docker Compose para UAT e production.
- Scripts para bootstrap, build, teste, subida, smoke, status e documentação.
- Documento de arquitetura exportado em Markdown, HTML e PDF.

PostgreSQL, RabbitMQ, Redis, Keycloak, Vault e a stack
OpenTelemetry/Prometheus/Grafana/Loki estão preparados em arquivos de
infraestrutura. A fatia validada neste momento usa persistência local em arquivo
em `.runtime`, porque o objetivo desta etapa é permitir que qualquer pessoa suba
e teste a API rapidamente.

## Arquitetura em execução

Fluxo resumido:

```text
Browser/Cliente HTTP
  -> BFF :6210
    -> Entries API
    -> Management API
    -> Consolidation API
    -> Observability Simulation API
  -> Workers locais
    -> Outbox Relay
    -> Consolidation Worker
    -> Reports Worker
  -> Estado local .runtime/{env}/cashflow-state.json
```

O frontend roda separado em `:6230`, mas consome a API por caminhos relativos
(`/api/...`) quando publicado pelo mesmo domínio/tunnel.

## Pré-requisitos

Instale ou confirme:

```bash
git --version
docker --version
docker compose version
dotnet --version
node --version
corepack --version
curl --version
jq --version
python3 --version
```

Versões usadas na validação:

- .NET SDK `10.0.x` compatível com `global.json`.
- Node.js `20.20.x`.
- Docker Compose moderno com suporte a `name:` e profiles.
- pnpm ativado via Corepack.

O projeto isola cache local em `.runtime`, então os comandos não precisam gravar
pacotes dentro de diretórios globais do usuário.

## Subir a API em UAT

Clone o repositório:

```bash
git clone https://github.com/giovanigsilva/fluxo-caixa-arquitetura-enterprise.git
cd fluxo-caixa-arquitetura-enterprise
```

Prepare secrets locais e diretórios de runtime:

```bash
./scripts/bootstrap.sh --env uat
```

Esse passo também gera a credencial local do BFF em arquivos ignorados pelo Git:

```text
.secrets/uat/login.txt
.secrets/uat/bff.env
```

Use o login indicado em `.secrets/uat/login.txt` na tela inicial. Depois da
senha, escaneie o QR Code em um aplicativo compatível com Google Authenticator
e informe o código TOTP de 6 dígitos.

Compile backend, publique os serviços .NET em `.runtime/publish` e gere o build
do frontend:

```bash
./scripts/build.sh --env uat
```

Suba o ambiente UAT:

```bash
./scripts/up.sh --env uat
```

O comando acima executa `docker compose down` e depois `up -d` para o ambiente
selecionado, sem remover volumes. Use-o quando quiser recriar os containers do
grupo.

URLs locais após a subida:

- Frontend: <http://127.0.0.1:6230>
- BFF/API: <http://127.0.0.1:6210>
- Swagger UI: <http://127.0.0.1:6210/swagger>
- OpenAPI BFF: <http://127.0.0.1:6210/openapi/v1.json>
- Health: <http://127.0.0.1:6210/health/ready>

O grupo Docker do UAT aparece como `teste-pratico`.

## Testar a API

Rode a suíte automatizada:

```bash
./scripts/test.sh --env uat
```

Rode o smoke test funcional:

```bash
./scripts/smoke.sh --env uat
```

O smoke test:

- verifica `/health/ready`;
- busca uma conta;
- cria um lançamento com `Idempotency-Key`;
- aguarda projeção;
- consulta consolidado diário;
- solicita exportação de extrato;
- baixa o CSV para `artifacts/smoke-statement.csv`.

Veja os containers ativos:

```bash
./scripts/status.sh --env uat
```

## Endpoints principais

Todas as chamadas funcionais passam pelo BFF em `http://127.0.0.1:6210`.

Headers mínimos para dados de negócio:

```text
X-Tenant-Id: org-alpha
X-User-Id: user-admin-alpha
Content-Type: application/json
```

Health e documentação:

- `GET /health/live`
- `GET /health/ready`
- `GET /swagger`
- `GET /swagger/index.html`
- `GET /openapi/v1.json`

Entries:

- `GET /api/entries/customers`
- `POST /api/entries/customers`
- `PUT /api/entries/customers/{id}` com `If-Match`
- `DELETE /api/entries/customers/{id}` com `If-Match`
- `GET /api/entries/accounts`
- `POST /api/entries/accounts`
- `GET /api/entries/categories`
- `POST /api/entries/categories`
- `GET /api/entries/cost-centers`
- `POST /api/entries/cost-centers`
- `GET /api/entries/entries`
- `POST /api/entries/entries` com `Idempotency-Key`
- `POST /api/entries/entries/{id}/reverse`
- `POST /api/entries/statements/exports`
- `GET /api/entries/statements/exports/{jobId}`
- `GET /api/entries/statements/exports/{jobId}/download`
- `GET /api/entries/audit`

Management:

- `GET /api/management/permissions`
- `GET /api/management/capabilities`
- `GET /api/management/users`
- `GET /api/management/roles`

Consolidation:

- `GET /api/consolidated/daily`
- `POST /api/consolidated/rebuild`

Observability:

- `GET /api/observability/scenarios`
- `GET /api/observability/samples?scenario=NORMAL`
- `GET /api/observability/stream`

## Exemplos com curl

Defina variáveis:

```bash
BASE=http://127.0.0.1:6210
TENANT=org-alpha
USER_ID=user-admin-alpha
```

Health:

```bash
curl -fsS "$BASE/health/ready"
```

Listar contas:

```bash
curl -fsS \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  "$BASE/api/entries/accounts" | jq
```

Criar lançamento:

```bash
ACCOUNT_ID="$(curl -fsS \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  "$BASE/api/entries/accounts" | jq -r '.[0].id')"

curl -fsS -X POST "$BASE/api/entries/entries" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  -H "Idempotency-Key: manual-$(date +%s%N)" \
  --data "{
    \"accountId\":\"$ACCOUNT_ID\",
    \"type\":\"Credit\",
    \"amount\":\"150.00\",
    \"businessDate\":\"$(date -I)\",
    \"description\":\"Recebimento operacional\",
    \"customerId\":null,
    \"categoryId\":null,
    \"costCenterId\":null
  }" | jq
```

Consultar consolidado:

```bash
curl -fsS \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  "$BASE/api/consolidated/daily" | jq
```

Gerar e baixar extrato CSV:

```bash
JOB_ID="$(curl -fsS -X POST "$BASE/api/entries/statements/exports" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  --data "{
    \"format\":\"csv\",
    \"from\":\"$(date -I)\",
    \"to\":\"$(date -I)\",
    \"accountId\":\"$ACCOUNT_ID\"
  }" | jq -r '.id')"

sleep 3

curl -fsS \
  -H "X-Tenant-Id: $TENANT" \
  -H "X-User-Id: $USER_ID" \
  "$BASE/api/entries/statements/exports/$JOB_ID/download" \
  -o artifacts/manual-statement.csv
```

## Ambientes e portas

UAT:

- Compose: `infra/compose/docker-compose.uat.yml`
- Grupo Docker: `teste-pratico`
- Frontend local: `127.0.0.1:6230`
- BFF local: `127.0.0.1:6210`
- Runtime: `.runtime/uat`
- Secrets: `.secrets/uat`

Production:

- Compose: `infra/compose/docker-compose.production.yml`
- Grupo Docker: `vertx-production`
- Frontend local: `127.0.0.1:6330`
- BFF local: `127.0.0.1:6310`
- Runtime: `.runtime/production`
- Secrets: `.secrets/production`

Para validar o Compose sem subir:

```bash
docker compose -f infra/compose/docker-compose.uat.yml config --quiet
docker compose -f infra/compose/docker-compose.production.yml config --quiet
```

## Persistência, secrets e arquivos locais

Arquivos gerados localmente não entram no Git:

- `.runtime/`
- `.secrets/`
- `artifacts/`
- `node_modules/`
- `*.tsbuildinfo`

Estado da aplicação:

```text
.runtime/{env}/cashflow-state.json
```

Exportações:

```text
.runtime/{env}/exports/
```

Secrets gerados pelo bootstrap:

```text
.secrets/{env}/postgres_write_password
.secrets/{env}/postgres_read_password
.secrets/{env}/postgres_platform_password
.secrets/{env}/bootstrap_admin_password
.secrets/{env}/bff.env
.secrets/{env}/login.txt
```

O arquivo `bff.env` expõe ao container somente o login, usuário seed e hash
SHA-256 da senha. O valor de uso em texto claro fica apenas em `login.txt`; o
`bootstrap_admin_password` é material local de derivação. Ambos são ignorados
pelo Git.

Não coloque tokens, senhas, certificados privados ou arquivos `.env` reais no
Git. O repositório foi estruturado para operar com secrets locais ignorados.

## Publicação Cloudflare

Os domínios planejados são:

- `https://vertx.dwilon.com`
- `https://uat.vertx.dwilon.com`

Links públicos de documentação:

- Swagger UI: `https://vertx.dwilon.com/swagger`
- OpenAPI JSON: `https://vertx.dwilon.com/openapi/v1.json`

As rotas Cloudflare/Tunnel são configuração operacional externa e não ficam neste
repositório. Para publicar em um ambiente próprio, a recomendação é:

1. Subir o UAT local.
2. Validar `http://127.0.0.1:6210/health/ready`.
3. Criar CNAME proxied no Cloudflare apontando para o tunnel.
4. Configurar ingress do tunnel:
   - `/api*`, `/bff*`, `/health*`, `/openapi*`, `/swagger*` para o BFF.
   - demais caminhos para o frontend.
5. Validar por HTTPS público.

Observação importante: em zonas Cloudflare full setup, o Universal SSL cobre o
domínio raiz e subdomínios de primeiro nível, como `vertx.dwilon.com`. Um host
mais profundo, como `uat.vertx.dwilon.com`, não é coberto pelo certificado
Universal padrão e normalmente exige certificado avançado ou certificado
customizado compatível com Cloudflare Tunnel.

## Operação diária

Build:

```bash
./scripts/build.sh --env uat
```

Teste:

```bash
./scripts/test.sh --env uat
```

Recriar containers UAT:

```bash
./scripts/up.sh --env uat
```

Status:

```bash
./scripts/status.sh --env uat
```

Smoke:

```bash
./scripts/smoke.sh --env uat
```

Gerar documentação:

```bash
./scripts/generate-docs.sh --env uat
```

Arquivos exportados:

- `docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.md`
- `docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.html`
- `docs/exports/Definicao_Arquitetura_Software_Fluxo_de_Caixa.pdf`

## Troubleshooting

Porta ocupada:

```bash
docker ps --format '{{.Names}}\t{{.Ports}}'
ss -ltnp
```

API não responde:

```bash
docker logs --tail 100 teste-pratico-bff
docker logs --tail 100 teste-pratico-entries-api
curl -v http://127.0.0.1:6210/health/ready
```

Frontend abre, mas dados não carregam:

```bash
curl -fsS http://127.0.0.1:6210/api/entries/accounts \
  -H "X-Tenant-Id: org-alpha" \
  -H "X-User-Id: user-admin-alpha" | jq
```

Teste .NET falha com erro de socket em ambiente sandbox:

```text
System.Net.Sockets.SocketException (13): Permission denied
```

Execute `./scripts/test.sh --env uat` fora do sandbox/restrição, porque o runner
do VSTest precisa abrir comunicação local.

Refazer tudo do zero sem apagar volumes manualmente:

```bash
./scripts/bootstrap.sh --env uat
./scripts/build.sh --env uat
./scripts/up.sh --env uat
./scripts/smoke.sh --env uat
```

## Limites conhecidos

- A fatia validada usa storage file-backed local, não PostgreSQL operacional.
- RabbitMQ/Redis/Keycloak/Vault estão preparados, mas não integrados ao fluxo
  padrão validado.
- A Observability API retorna telemetria sintética rotulada.
- O módulo de assistente/IA existe apenas como boundary desabilitada.
- O script de performance k6 existe, mas benchmark formal não foi executado
  nesta entrega.
- A stack production tem Compose validado, mas deve receber secrets, DNS,
  certificado e banco reais antes de qualquer uso produtivo.

## Documentação adicional

- `docs/architecture/definition.md`
- `docs/architecture/baseline-delta.md`
- `docs/api/README.md`
- `docs/security/threat-model.md`
- `docs/testing/README.md`
- `docs/operations/README.md`
- `docs/implementation-status.md`
- `docs/dependencies.md`
