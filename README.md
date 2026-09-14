# Fluxo de Caixa Vertx

Implementação incremental do `Vertx.CashFlow`, uma base enterprise para API,
BFF, frontend, workers e documentação operacional de um sistema de fluxo de
caixa multi-tenant.

Este repositório já sobe uma fatia vertical executável em Docker Compose:
cadastros, lançamentos, estorno, idempotência, outbox, consolidação diária,
exportação de extrato, simulação de observabilidade e SPA em pt-BR.

## Sumário

- [Manual do examinador](#manual-do-examinador)
- [Estado atual](#estado-atual)
- [Credenciais de avaliação](#credenciais-de-avaliacao)
- [Endereços publicados](#enderecos-publicados)
- [AI-first](#ai-first)
- [Login, reCAPTCHA e QR Code](#login-recaptcha-e-qr-code)
- [Segurança enterprise implementada](#seguranca-enterprise-implementada)
- [Monitoria, alertas e notificações](#monitoria-alertas-e-notificacoes)
- [Arquitetura em execução](#arquitetura-em-execucao)
- [Trade-offs arquiteturais](#trade-offs-arquiteturais)
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

## Manual do examinador

O arquivo [`MANUAL-DO-EXAMINADOR.md`](MANUAL-DO-EXAMINADOR.md) e a entrada
principal para avaliacao pelo repositorio. Ele consolida os links publicos,
credenciais de teste, roteiro de avaliacao, comandos locais e aponta para o
manual completo em [`docs/manual-de-uso.md`](docs/manual-de-uso.md).

A aderencia ao PDF do desafio esta rastreada em
[`docs/requirements-traceability.md`](docs/requirements-traceability.md).

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
- SupportAgent API com RAG governado e LLM local em GPU.
- SPA React/Vite em português do Brasil consumindo o BFF.
- Tela inicial pública com login, senha e Google reCAPTCHA v2 validado no BFF.
- Agente Vertx flutuante no canto inferior direito, com chat real via
  subagente, respostas longas formatadas, conversa local visual e ligação
  preparada visualmente.
- Swagger/OpenAPI 100% documentado para todas as rotas expostas pelo BFF.
- Benchmark k6 do consolidado diário validado a 50 RPS por 10 minutos, com
  0.00% de falhas.
- Docker Compose para UAT e production.
- Scripts para bootstrap, build, teste, subida, smoke, status e documentação.
- Documento de arquitetura exportado em Markdown, HTML e PDF.

PostgreSQL, RabbitMQ, Redis, Keycloak, Vault e a stack
OpenTelemetry/Prometheus/Grafana/Loki estão preparados em arquivos de
infraestrutura. A fatia validada neste momento usa persistência local em arquivo
em `.runtime`, porque o objetivo desta etapa é permitir que qualquer pessoa suba
e teste a API rapidamente.

## Credenciais de avaliação

Estas credenciais são de demonstração, foram autorizadas para exposição ao
examinador e devem ser usadas somente neste ambiente técnico:

```text
Login: admin@admin.com
Senha: Vtx-1d7d875ea260072afc7fa86d
```

O acesso pela tela inicial também exige a validação do Google reCAPTCHA no
navegador. Chamadas diretas ao endpoint de login sem token reCAPTCHA válido são
recusadas pelo BFF.

## Endereços publicados

Produção pública de avaliação:

- Aplicação: <https://vertx.dwilon.com/>
- Manual de uso: <https://vertx.dwilon.com/manual.html>
- Swagger UI: <https://vertx.dwilon.com/swagger>
- OpenAPI JSON: <https://vertx.dwilon.com/openapi/v1.json>
- Health ready: <https://vertx.dwilon.com/health/ready>

UAT:

- Frontend local: <http://127.0.0.1:6230>
- Manual local: <http://127.0.0.1:6230/manual.html>
- BFF/API local: <http://127.0.0.1:6210>
- Swagger UI local: <http://127.0.0.1:6210/swagger>
- OpenAPI JSON local: <http://127.0.0.1:6210/openapi/v1.json>
- Health ready local: <http://127.0.0.1:6210/health/ready>
- Domínio UAT reservado: <https://uat.vertx.dwilon.com/>

Nota operacional: o domínio `uat.vertx.dwilon.com` é um host profundo. Em zona
Cloudflare full setup, esse formato normalmente exige certificado avançado ou
customizado, porque o Universal SSL cobre o domínio raiz e subdomínios de
primeiro nível, como `vertx.dwilon.com`.

## AI-first

Esta entrega foi feita em modelo AI-first: os requisitos foram transformados em
contratos executáveis, documentação operacional, Swagger, scripts de build/teste
e validações automatizadas junto com a implementação. O objetivo foi deixar a API
legível tanto para pessoas quanto para agentes de IA, com rotas previsíveis,
schemas completos, exemplos, erros documentados e comandos reproduzíveis.

AI-first aqui não significa que uma IA decide lançamentos financeiros em tempo de
execução. Significa que a arquitetura foi preparada para colaboração com IA,
auditoria rápida, automação de operações e evolução assistida sem esconder
comportamentos críticos fora do código ou da documentação.

## Login, reCAPTCHA e QR Code

Implementação ativa:

- A primeira tela exige login e senha.
- O frontend carrega o widget oficial Google reCAPTCHA v2 checkbox.
- O BFF expõe `GET /bff/login/recaptcha/config` somente com a site key pública.
- O BFF valida `recaptchaToken` no servidor pela API oficial do Google antes de
  aceitar `POST /bff/login/start`.
- Token ausente, inválido ou falso retorna erro e não cria sessão local.
- A secret key do reCAPTCHA fica somente no servidor, em `.secrets/{env}`.

Sobre o desafio por QR Code: o reCAPTCHA v2 checkbox pode exibir seleção de
imagens, porque esse é um comportamento normal do produto clássico do Google.
Para desafio com QR Code, a documentação oficial direciona para o Google Cloud
reCAPTCHA Fraud Defense com challenge policy de QR Code, recurso que exige
allowlist da Google.

Solicitação já enviada à Google:

- Produto solicitado: reCAPTCHA Fraud Defense com QR Code challenge.
- Universal key informada: `6LfYQrstAAAAAPPY-Fg6wXWE6cPa5jH35GJNT8ke`.
- Domínios solicitados: `vertx.dwilon.com` e `uat.vertx.dwilon.com`.
- Destino: `fraud-defense@google.com`.
- Evidência de envio: mailserver `dwilon.com`, DKIM ativo, status SMTP `250 OK`,
  message-id `<20260914154541.2CC7F2041097@mail.dwilon.com>`.

Quando a Google liberar a allowlist, o próximo passo é ativar o fluxo Enterprise:
criar assessment server-side no Google Cloud, aplicar a policy de QR Code para a
Universal key e trocar o provider do gate de entrada sem remover a validação de
senha no BFF.

Referências oficiais:

- <https://developers.google.com/recaptcha/docs/versions>
- <https://support.google.com/recaptcha/faq/6080947>
- <https://docs.cloud.google.com/recaptcha/docs/select-challenge-types>
- <https://docs.cloud.google.com/recaptcha/docs/challenge-policies>
- <https://docs.cloud.google.com/recaptcha/docs/choose-key-type>

## Segurança enterprise implementada

Controles ativos nesta fatia executável:

- Fronteira pública no BFF: o navegador fala com o BFF, não com os serviços
  internos diretamente.
- Login de entrada com senha e Google reCAPTCHA v2 verificado no servidor.
- Senha armazenada no container como hash SHA-256, não como texto claro.
- Secrets reais ficam fora do Git, em `.secrets/{env}`, e são injetados no
  runtime por `env_file`.
- Portas do BFF e frontend são expostas somente em `127.0.0.1`; a publicação
  pública passa pelo Cloudflare Tunnel.
- Containers .NET rodam sem root, com filesystem read-only, `tmpfs` limitado,
  `no-new-privileges` e `cap_drop: ALL`.
- Serviços internos ficam em rede Docker privada do grupo `teste-pratico`.
- Headers de tenant e usuário separam o escopo lógico das chamadas de negócio
  nesta fatia UAT.
- Catálogo de permissões, usuários, roles e capabilities disponível na
  Management API.
- Idempotência obrigatória em criação de lançamentos com `Idempotency-Key`,
  bloqueando replay com payload divergente.
- Controle de concorrência com `If-Match` em alterações e exclusões de cadastros.
- Auditoria de operações sensíveis disponível em `GET /api/entries/audit`.
- Outbox pattern antes da projeção, reduzindo perda de eventos entre escrita e
  consolidação.
- Exportação de extrato gerada por job assíncrono, evitando acoplamento pesado
  no request principal.
- Documentação OpenAPI descreve parâmetros, headers, payloads, status codes,
  exemplos e erros esperados.

Controles preparados para evolução produtiva:

- Keycloak/OIDC com PKCE, MFA/passkey e políticas por audience.
- PostgreSQL com bancos separados, migrações, transações reais, TLS e RLS.
- Vault para secrets gerenciados, leases e auditoria de acesso.
- RabbitMQ com retry, DLQ, publisher confirms e TLS.
- Rate limit, CSRF e cookies de sessão reais no BFF produtivo.
- Scans SAST/containers, DAST e E2E antes de promover para uso financeiro real.

## Monitoria, alertas e notificações

Monitoria ativa:

- `GET /health/live` para vida do processo.
- `GET /health/ready` para readiness da entrada pública.
- `scripts/status.sh --env uat` para visão dos containers do grupo.
- Logs por container com `docker logs`.
- Observability Simulation API com cenários rotulados em
  `/api/observability/scenarios`.
- Amostras de telemetria sintética em `/api/observability/samples`.
- Stream SSE em `/api/observability/stream` para dashboards em tempo quase real.
- Tela de monitoramento na SPA exibindo RPS, p95, Rabbit ready, outbox pendente e
  orçamento de erro sintético.

Stack preparada:

- OpenTelemetry Collector.
- Prometheus com scrape configurado para BFF, Entries, Consolidation, Management
  e Observability API.
- Grafana provisionado com datasource Prometheus e Loki.
- Loki preparado para centralização de logs.

Alertas e notificações documentados para operação enterprise:

- Health não ready deve acionar incidente de disponibilidade.
- Aumento de p95/p99 deve acionar alerta de latência.
- Outbox pendente acima do limite operacional deve acionar alerta de fila.
- Erros 4xx de login por reCAPTCHA devem alimentar painel antifraude.
- Erros 5xx devem acionar alerta de confiabilidade.
- Falha de geração/download de extrato deve acionar alerta funcional.
- Notificação externa por e-mail já foi comprovada pelo mailserver Dwilon no
  envio à Google; a automação contínua de alertas por e-mail/pager deve ser
  conectada ao Prometheus/Alertmanager ou ferramenta equivalente no ambiente
  final.

## Arquitetura em execução

Fluxo resumido:

```text
Browser/Cliente HTTP
  -> BFF :6210
    -> Entries API
    -> Management API
    -> Consolidation API
    -> Observability Simulation API
    -> SupportAgent API
      -> RAG governado
      -> Qwen/Qwen3.5-35B-A3B-GPTQ-Int4 via vLLM em GPU
  -> Workers locais
    -> Outbox Relay
    -> Consolidation Worker
    -> Reports Worker
  -> Estado local .runtime/{env}/cashflow-state.json
```

O frontend roda separado em `:6230`, mas consome a API por caminhos relativos
(`/api/...`) quando publicado pelo mesmo domínio/tunnel.

## Support Agent e RAG

O Agente Vertx do portal usa um subagente real, exposto internamente como
`SupportAgent API`. O navegador não decide respostas por palavras-chave: ele
envia a conversa para `POST /api/agent/chat`, o BFF encaminha para o subagente e
o subagente chama o LLM local em GPU (`Qwen/Qwen3.5-35B-A3B-GPTQ-Int4` via
vLLM).

O controle de resposta é feito antes do LLM:

- política de bloqueio para prompt injection, secrets, tokens, arquivos
  sensíveis e comandos destrutivos;
- recuperação RAG em base curada do portal, com documentos sobre login, menu,
  topo, dashboard, gráficos, lançamentos, clientes, monitoramento, alertas,
  teste de carga, manual, Swagger, agente, segurança e cordialidades simples;
- conversa social controlada para cumprimentos, agradecimentos, despedidas e
  identidade do agente, sempre redirecionando para apoio no Vertx;
- recusa quando não existe evidência suficiente no RAG;
- prompt final contendo somente o contexto autorizado;
- resposta formatada em português; a API retorna fontes internas separadas para
  auditoria e a interface exibe nomes amigáveis quando relevante.

O mesmo subagente já aceita o canal lógico `telephony-support`, reservado para a
telefonia de apoio. Nesta etapa ele ainda não aciona ASR, TTS, microfone,
discagem real nem criação automática de lançamentos.

## Trade-offs arquiteturais

Esta entrega prioriza uma fatia vertical executável, auditável e fácil de subir
localmente, sem esconder o desenho alvo enterprise. Os trade-offs abaixo
explicam o que foi implementado agora, por que a decisão foi tomada, quais ganhos
ela trouxe, quais custos permanecem e como evoluir para produção real.

| Decisão | Por que foi escolhida | Ganho | Custo/risco assumido | Evolução natural |
| --- | --- | --- | --- | --- |
| Separar BFF, Entries API, Consolidation API e workers em processos distintos | O desafio exige que o controle de lançamentos não dependa da disponibilidade do consolidado diário. | Falha no worker/API de consolidado não impede a Entries API de aceitar lançamentos; responsabilidades ficam isoladas. | Mais containers e mais pontos de configuração do que um monólito simples. | Orquestrar em Kubernetes/ECS/Nomad, com autoscaling separado por serviço e probes reais. |
| Usar storage file-backed em `.runtime/{env}` na UAT | A prova precisa ser reproduzível rapidamente por qualquer avaliador, sem provisionar banco externo. | Bootstrap simples, sem dependência de infraestrutura paga ou credenciais sensíveis; smoke e k6 rodam localmente. | Não substitui PostgreSQL em produção: não há pooling, réplica, RLS, backup gerenciado ou concorrência multi-instância real. | Migrar para PostgreSQL com EF/Npgsql, migrations, transações, TLS, RLS e bancos/roles por ambiente. |
| Persistir lançamento + outbox antes de responder | O consolidado deve ser assíncrono e recuperável, sem acoplar escrita financeira à projeção. | Reduz perda entre escrita e projeção; permite reprocessamento e reconstrução do read model. | Outbox local é suficiente para a UAT, mas não tem garantias de broker distribuído. | Usar RabbitMQ real com publisher confirms, mandatory routing, retry, DLQ, TLS e consumidores idempotentes. |
| Read model diário separado da escrita | Consultas de consolidado não devem disputar o mesmo fluxo lógico dos comandos de lançamento. | Leitura fica otimizada para saldo diário e expõe defasagem por `outboxPending`. | Consistência é eventual; o saldo pode ficar alguns segundos atrás da escrita. | Adotar CQRS com banco de leitura dedicado, métricas de lag e alertas de atraso de projeção. |
| BFF como fronteira pública | O navegador não deve conhecer os serviços internos nem suas portas. | Centraliza entrada pública, login, proxy, Swagger e health; reduz exposição dos containers internos. | O BFF vira ponto crítico de entrada e precisa de hardening adicional em produção. | Adicionar rate limit, WAF rules, CSRF, cookies seguros, session store, OIDC e policies por rota. |
| Login local com senha + Google reCAPTCHA v2 | O usuário pediu tela inicial protegida e validação antirrobô pública; Keycloak real aumentaria tempo e dependências da prova. | Entrega proteção real na entrada web com validação server-side do token reCAPTCHA. | Não é MFA/OIDC enterprise completo; QR Code Fraud Defense depende de liberação externa da Google. | Trocar para Keycloak/OIDC Authorization Code + PKCE, MFA/passkey e reCAPTCHA Enterprise/Fraud Defense quando liberado. |
| Observability Simulation API no dashboard | Era necessário mostrar monitoramento, alertas e carga sem derrubar ou sobrecarregar o ambiente público. | Demonstra a experiência operacional com RPS, latência, filas, error budget e cenários controlados. | Métricas do dashboard são sintéticas; não provam telemetria real de produção. | Ativar OpenTelemetry Collector, Prometheus, Grafana, Loki e Alertmanager com métricas reais dos serviços. |
| Benchmark k6 em UAT local | O requisito do PDF pede 50 RPS no consolidado com até 5% de perda. | Evidência objetiva: 30.001 requisições em 10 min, 50.001571/s, 0.00% falhas, p95 807.38 us. | Mede a UAT file-backed local, não uma topologia produtiva com rede pública e banco gerenciado. | Repetir o teste em produção com PostgreSQL real, réplicas, TLS, observabilidade e janela estatística maior. |
| Reports Worker assíncrono para CSV/XLSX/PDF | Relatórios podem ser mais pesados que comandos online. | Evita prender o request principal e prepara o fluxo para processamento em background. | Na UAT, os arquivos ficam em storage local; não há object storage nem retenção governada. | Usar R2/S3, antivírus, expiração, trilha de download e políticas de retenção por tenant. |
| Docker Compose para UAT e production | O desafio valoriza execução local clara e documentação no repositório. | Qualquer avaliador consegue subir a solução com comandos simples e ver os containers separados. | Compose não entrega HA real, rolling deploy, autoscaling nem self-healing avançado. | Empacotar imagens versionadas, publicar em registry e operar em orquestrador com blue/green ou canary. |
| Cloudflare Tunnel para publicação pública | Publicar sem expor portas externas do host diretamente. | Reduz superfície de rede, mantém BFF/frontend bound em `127.0.0.1` e entrega HTTPS público no domínio principal. | Configuração operacional fica fora do Git por conter IDs/secrets; subdomínio profundo UAT depende de certificado compatível. | Documentar IaC do edge com secrets externos, certificado avançado/customizado e ambientes separados. |
| Segurança alvo documentada mesmo quando não ativa na UAT | O PDF permite demonstrar premissas em decisões e representações arquiteturais, não só em codificação. | Mostra conhecimento de RLS, Vault, OIDC, TLS, DLQ, scans, observabilidade e resposta a falhas. | Exige honestidade: controles preparados não podem ser vendidos como ativos. | Promover os itens preparados por fase, sempre com teste, evidência e runbook. |
| AI-first como processo, não como decisão financeira automática | O usuário pediu destacar a implementação AI-first, mas o domínio financeiro exige previsibilidade e auditoria. | Documentação, Swagger, scripts e matriz de aderência ficam legíveis para humanos e agentes de IA. | Não há IA executando lançamentos ou aprovando decisões financeiras na UAT. | Usar IA apenas como assistente auditável para suporte operacional, análise de logs e geração de relatórios, com fronteira explícita. |

Resumo da decisão principal: para a prova, a escolha foi entregar o fluxo crítico
fim a fim funcionando e medido; para produção real, a evolução correta é trocar
as dependências locais por serviços gerenciados/clusterizados sem mudar o
contrato público da API.

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

Para habilitar o Google reCAPTCHA v2 real, salve as chaves emitidas no console
do Google antes de rodar o bootstrap:

```text
.secrets/uat/recaptcha_site_key
.secrets/uat/recaptcha_secret_key
```

Em uma instalação limpa, use o login indicado em `.secrets/uat/login.txt` na tela
inicial. No ambiente público de avaliação desta entrega, use:

```text
Login: admin@admin.com
Senha: Vtx-1d7d875ea260072afc7fa86d
```

A entrada exige senha e validação pelo checkbox Google reCAPTCHA.

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

Support Agent:

- `POST /api/agent/chat`

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
- SupportAgent API: interno no Compose, sem porta pública direta
- LLM local consumido pelo subagente: `qwen3-llm-realtime-test` via rede Docker
  `census-realtime-agent-test_realtime`
- Runtime: `.runtime/uat`
- Secrets: `.secrets/uat`

Production:

- Compose: `infra/compose/docker-compose.production.yml`
- Grupo Docker: `vertx-production`
- Frontend local: `127.0.0.1:6330`
- BFF local: `127.0.0.1:6310`
- SupportAgent API: interno no Compose, sem porta pública direta
- LLM local consumido pelo subagente: `qwen3-llm-realtime-test` via rede Docker
  `census-realtime-agent-test_realtime`
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
.secrets/{env}/recaptcha_site_key
.secrets/{env}/recaptcha_secret_key
```

O arquivo `bff.env` expõe ao container somente o login, usuário seed e hash
SHA-256 da senha. Quando os arquivos `recaptcha_site_key` e
`recaptcha_secret_key` existem, o bootstrap também injeta as variáveis
`VERTX_RECAPTCHA_SITE_KEY` e `VERTX_RECAPTCHA_SECRET_KEY` no BFF. O valor de uso
da senha em texto claro fica apenas em `login.txt`; o `bootstrap_admin_password`
é material local de derivação. Esses arquivos são ignorados pelo Git.

Não coloque tokens, senhas, certificados privados ou arquivos `.env` reais no
Git. O repositório foi estruturado para operar com secrets locais ignorados.

## Publicação Cloudflare

Domínios da entrega:

- `https://vertx.dwilon.com`: endpoint público principal da avaliação.
- `https://uat.vertx.dwilon.com`: domínio UAT reservado para rota dedicada.

Links públicos de documentação:

- Manual de uso: `https://vertx.dwilon.com/manual.html`
- Swagger UI: `https://vertx.dwilon.com/swagger`
- OpenAPI JSON: `https://vertx.dwilon.com/openapi/v1.json`
- Health ready: `https://vertx.dwilon.com/health/ready`

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
- O agente usa LLM local em GPU com RAG governado; conversa local por microfone,
  TTS e discagem real ainda são etapas futuras.
- O QR Code do Google Fraud Defense depende de allowlist da Google e ativação do
  fluxo reCAPTCHA Enterprise; hoje está ativo o Google reCAPTCHA v2 checkbox.
- O domínio profundo `uat.vertx.dwilon.com` pode exigir certificado Cloudflare
  avançado/customizado para HTTPS público válido.
- O benchmark k6 foi executado na UAT local; para produção real, reexecutar com
  banco gerenciado, réplicas, rede pública e janela estatística maior.
- A stack production tem Compose validado, mas deve receber secrets, DNS,
  certificado e banco reais antes de qualquer uso produtivo.

## Documentação adicional

- `MANUAL-DO-EXAMINADOR.md`
- `docs/requirements-traceability.md`
- `docs/architecture/definition.md`
- `docs/manual-de-uso.md`
- `docs/architecture/baseline-delta.md`
- `docs/api/README.md`
- `docs/security/threat-model.md`
- `docs/testing/README.md`
- `docs/operations/README.md`
- `docs/implementation-status.md`
- `docs/dependencies.md`
