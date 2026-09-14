# Manual De Uso - Fluxo de Caixa Vertx

Versao: UAT publica de avaliacao em 2026-09-14.

Este manual explica como acessar, navegar e operar a fatia executavel do Fluxo de
Caixa Vertx. Ele foi escrito para avaliadores tecnicos, operadores financeiros e
pessoas que precisam entender rapidamente como cada item do sistema funciona.

## Enderecos

Ambiente publico principal:

- Aplicacao: https://vertx.dwilon.com/
- Manual HTML: https://vertx.dwilon.com/manual.html
- Swagger UI: https://vertx.dwilon.com/swagger
- OpenAPI JSON: https://vertx.dwilon.com/openapi/v1.json
- Health ready: https://vertx.dwilon.com/health/ready

Ambiente UAT local:

- Frontend: http://127.0.0.1:6230
- BFF/API: http://127.0.0.1:6210
- Swagger UI: http://127.0.0.1:6210/swagger
- OpenAPI JSON: http://127.0.0.1:6210/openapi/v1.json
- Health ready: http://127.0.0.1:6210/health/ready

O dominio `uat.vertx.dwilon.com` esta reservado, mas por ser subdominio profundo
pode exigir certificado Cloudflare avancado ou customizado para HTTPS publico.

## Credenciais De Teste

Estas credenciais sao exclusivas para avaliacao:

```text
Login: admin@admin.com
Senha: Vtx-1d7d875ea260072afc7fa86d
```

O acesso tambem exige Google reCAPTCHA v2 checkbox. Se o Google apresentar
desafio de imagens, conclua o desafio no navegador para liberar o login.

## Visao Geral

O sistema registra e consulta fluxo de caixa multi-tenant. A entrega atual inclui:

- login protegido por senha e reCAPTCHA;
- dashboard financeiro;
- lancamentos de credito e debito;
- consolidado diario;
- cadastro visual de clientes seed;
- monitoramento sintetico;
- controle de alertas simulado;
- teste de carga simulado;
- agente Vertx flutuante com chat real via subagente, RAG governado e modos de
  atendimento visual;
- Swagger com todas as rotas expostas pelo BFF;
- health checks;
- workers de outbox, consolidacao e relatorios.

## Como Acessar

1. Abra https://vertx.dwilon.com/.
2. Informe `admin@admin.com`.
3. Informe `Vtx-1d7d875ea260072afc7fa86d`.
4. Resolva o Google reCAPTCHA.
5. Clique em `Entrar`.

Depois do login, a sessao fica no `sessionStorage` do navegador ate expirar ou
ate o usuario clicar em `Sair`.

## Menu Principal

O menu lateral organiza a operacao por area:

- `Dashboard`: visao executiva de fluxo financeiro, banco, latencia e alertas.
- `Monitoramento`: saude sintetica de APIs, banco de leitura, RabbitMQ, Redis e
  camada de observabilidade.
- `Alertas`: regras operacionais que podem ser ativadas ou silenciadas na tela.
- `Teste de carga`: cenarios sinteticos para simular carga e incidentes.
- `Lancamentos`: lista de movimentacoes financeiras confirmadas.
- `Clientes`: clientes seed disponiveis para demonstracao.

## Agente Vertx

O simbolo do agente fica fixo no canto inferior direito da tela autenticada.
Ao clicar nele, aparecem tres opcoes acima do simbolo:

- `Conversar por chat`: abre um chat real no portal. O navegador envia a
  conversa para o BFF, que encaminha ao `SupportAgent API`. Esse subagente usa
  RAG governado e o LLM local em GPU `Qwen/Qwen3.5-35B-A3B-GPTQ-Int4`.
  As respostas sao maiores e formatadas em blocos/listas. A API retorna fontes
  internas separadas para auditoria, e a interface mostra nomes amigaveis quando
  for relevante.
- `Conversar local`: ativa conversa por microfone no computador ou celular. O
  navegador grava WAV na taxa nativa do dispositivo, envia automaticamente apos
  pausa na fala ao `SupportAgent API`, o subagente transcreve com Qwen3-ASR
  local, consulta o mesmo RAG/LLM governado e reproduz uma resposta curta em
  texto plano com a voz nativa do navegador.
- `Conversar por ligacao`: abre a tela visual com campo para numero de telefone.
  A discagem real e a escolha do provedor telefonico serao detalhadas depois.

O RAG do agente contem documentos curados sobre login, menu lateral, topo,
dashboard, graficos, lancamentos, clientes, monitoramento, alertas, teste de
carga, manual, Swagger, seguranca, cordialidades simples e o proprio agente. Se
a pergunta nao tiver evidencia nesses documentos, o agente recusa. Ele tambem
bloqueia prompt injection, pedidos de secrets/tokens, arquivos sensiveis,
comandos destrutivos e operacoes financeiras automaticas.

Cumprimentos como "oi", "ola", "bom dia", agradecimentos, despedidas e
perguntas simples sobre quem e o agente sao permitidos para que a conversa fique
natural. Assuntos fora do portal Vertx continuam bloqueados.

O chat nao cria lancamentos automaticamente e nao chama APIs financeiras sozinho.
Ele orienta o operador a usar a tela correta.

## Dashboard

O dashboard e a primeira visao apos o login. Ele mostra:

- `Creditos`: soma dos lancamentos de entrada.
- `Debitos`: soma dos lancamentos de saida.
- `Saldo projetado`: creditos menos debitos no escopo atual.
- `Banco total req/s`: soma sintetica de leitura e escrita por segundo.
- `p95 API`: percentil 95 sintetico da latencia.
- `Alertas ativos`: quantidade de regras de alerta disparadas no cenario atual.

Os graficos do dashboard sao:

- `Fluxo diario`: compara creditos e debitos por data de negocio.
- `Banco req/s`: mostra leitura, escrita e total simulado de requisicoes por
  segundo.
- `Latencia`: mostra p50, p95 e p99 simulados.
- `Filas e projecao`: mostra outbox pendente, mensagens prontas no broker,
  entradas projetadas e duplicados ignorados.

## Lancamentos

O painel `Novo lancamento` permite registrar movimentacoes reais no UAT.

Campos:

- `Credito` ou `Debito`: define se o valor entra ou sai do caixa.
- `Conta`: conta financeira usada no lancamento.
- `Valor`: valor positivo com ate duas casas decimais. Aceita `150,00` ou
  `150.00`.
- `Data`: data de negocio do lancamento.
- `Descricao`: texto obrigatorio para identificar a movimentacao.
- `Cliente`: campo opcional para vincular a um cliente cadastrado.

Como criar:

1. Escolha `Credito` ou `Debito`.
2. Selecione a conta.
3. Informe o valor.
4. Escolha a data.
5. Preencha a descricao.
6. Opcionalmente selecione o cliente.
7. Clique em `Registrar lancamento`.

O sistema envia o lancamento para `POST /api/entries/entries` com
`Idempotency-Key`, evitando duplicidade acidental com payload divergente. Apos o
sucesso, a tabela e o consolidado sao atualizados.

## Tabela De Lancamentos

A tabela `Lancamentos` lista movimentacoes confirmadas:

- `Data`: data de negocio usada no lancamento.
- `Descricao`: identificacao operacional.
- `Tipo`: `Credito` ou `Debito`.
- `Valor`: valor formatado em BRL.

Lancamentos criados por teste ficam persistidos no storage local `.runtime/uat`.

## Clientes

A area `Clientes` mostra os clientes seed do tenant `org-alpha`. No UAT atual,
ela serve para demonstrar vinculacao opcional de lancamentos e leitura de dados
basicos de cadastro.

Cada item mostra:

- razao social ou nome legal;
- versao do registro.

## Monitoramento Do Sistema

O monitoramento exibido na aplicacao usa a Observability Simulation API. Os dados
sao sinteticos e rotulados para demonstracao.

Indicadores:

- `RPS leitura`: requisicoes sinteticas de leitura por segundo.
- `RPS escrita`: requisicoes sinteticas de escrita por segundo.
- `Outbox pendente`: volume sintetico de eventos aguardando publicacao.
- `Error budget`: orcamento sintetico restante de erro.

Saude dos componentes:

- `Entries API`: API de cadastros e lancamentos.
- `Read DB`: leitura do consolidado.
- `RabbitMQ`: broker planejado/simulado.
- `Redis`: cache/quota planejado/simulado.
- `Observability`: stack preparada.
- `AI boundary`: indica o subagente de apoio com RAG governado, LLM local em GPU
  e voz local por Qwen3-ASR.

## Controle De Alertas

O painel `Controle de alertas` permite simular regras operacionais:

- `Latencia p95`: alerta quando o p95 passa do limite.
- `Outbox pendente`: alerta quando a fila local passa do limite.
- `Banco req/s`: alerta quando o total de requisicoes por segundo passa do
  limite definido.
- `Erros 5xx`: alerta critico para erro de servidor.
- `Rate limit 429`: alerta para saturacao/limite de chamadas.
- `Error budget`: alerta critico quando o orcamento cai abaixo do limite.

Cada regra pode ser marcada ou desmarcada. Desmarcar uma regra silencia a
avaliacao visual daquela condicao no dashboard.

## Teste De Carga

O menu `Teste de carga` muda o cenario sintetico de observabilidade. Ele nao
executa carga real no servidor publico; ele altera a amostra exibida para mostrar
como a operacao reagiria.

Cenarios:

- `Normal`: operacao estavel.
- `Carga 50`: carga moderada.
- `Carga 100`: carga alta.
- `Pico 200`: pico agressivo de leitura/escrita.
- `Recuperacao`: simulacao de retorno apos incidente.

Ao selecionar um cenario, o frontend chama:

```text
POST /api/observability/scenarios/{scenario}
```

Depois, os graficos e alertas passam a refletir a amostra do cenario escolhido.

## Swagger E Rotas Documentadas

O Swagger esta disponivel em:

- https://vertx.dwilon.com/swagger
- http://127.0.0.1:6210/swagger

O OpenAPI JSON esta em:

- https://vertx.dwilon.com/openapi/v1.json
- http://127.0.0.1:6210/openapi/v1.json

Rotas principais:

- `GET /health/live`
- `GET /health/ready`
- `GET /bff/login/recaptcha/config`
- `POST /bff/login/start`
- `GET /api/entries/accounts`
- `GET /api/entries/customers`
- `GET /api/entries/entries`
- `POST /api/entries/entries`
- `POST /api/entries/entries/{id}/reverse`
- `POST /api/entries/statements/exports`
- `GET /api/entries/statements/exports/{id}`
- `GET /api/entries/statements/exports/{id}/download`
- `GET /api/entries/audit`
- `GET /api/management/permissions`
- `GET /api/management/capabilities`
- `GET /api/management/users`
- `GET /api/management/roles`
- `GET /api/consolidated/daily`
- `POST /api/consolidated/rebuild`
- `GET /api/observability/scenarios`
- `GET /api/observability/samples`
- `POST /api/observability/scenarios/{scenario}`
- `GET /api/observability/stream`

Headers de negocio usados nos exemplos e na SPA:

```text
X-Tenant-Id: org-alpha
X-User-Id: user-admin-alpha
Content-Type: application/json
```

## Seguranca

Controles ativos:

- BFF como fronteira publica.
- Login com senha.
- Google reCAPTCHA v2 validado no servidor.
- Secret key do reCAPTCHA somente no BFF.
- Hash de senha no ambiente do container.
- Secrets fora do Git em `.secrets/{env}`.
- Portas locais vinculadas a `127.0.0.1`.
- Publicacao via Cloudflare Tunnel.
- Containers com usuario nao-root, filesystem read-only, `tmpfs`,
  `no-new-privileges` e `cap_drop: ALL`.
- Idempotencia em lancamentos.
- Controle de concorrencia com `If-Match` em cadastros.
- Auditoria basica de operacoes.
- Outbox antes da consolidacao.

Controles preparados para producao real:

- Keycloak/OIDC com PKCE.
- MFA/passkey.
- PostgreSQL com RLS/TLS.
- Vault.
- RabbitMQ com retry/DLQ/TLS.
- Rate limit, CSRF e cookies de sessao reais.
- Scans SAST, DAST, SBOM e imagens.

## QR Code Anti-IA

O desafio ativo hoje e Google reCAPTCHA v2 checkbox. O QR Code anti-IA depende do
Google Cloud reCAPTCHA Fraud Defense com challenge policy de QR Code.

Status:

- Universal key informada: `6LfYQrstAAAAAPPY-Fg6wXWE6cPa5jH35GJNT8ke`.
- Dominios solicitados: `vertx.dwilon.com` e `uat.vertx.dwilon.com`.
- Solicitacao enviada para `fraud-defense@google.com`.
- Envio SMTP validado com status `250 OK`.

Quando a Google liberar a allowlist, o fluxo pode ser ativado no BFF com
assessment server-side do reCAPTCHA Enterprise.

## Operacao Local

Subir UAT:

```bash
./scripts/bootstrap.sh --env uat
./scripts/build.sh --env uat
./scripts/up.sh --env uat
```

Ver status:

```bash
./scripts/status.sh --env uat
```

Testar:

```bash
./scripts/test.sh --env uat
```

Smoke funcional:

```bash
./scripts/smoke.sh --env uat
```

## Troubleshooting

Se o site nao abrir:

1. Verifique `docker ps`.
2. Confirme se `teste-pratico-web` esta healthy.
3. Acesse `http://127.0.0.1:6230`.

Se a API nao responder:

1. Acesse `http://127.0.0.1:6210/health/ready`.
2. Veja `docker logs --tail 100 teste-pratico-bff`.
3. Veja `docker logs --tail 100 teste-pratico-entries-api`.

Se o login nao concluir:

1. Confirme que o reCAPTCHA carregou.
2. Resolva o desafio do Google.
3. Use as credenciais exatas deste manual.
4. Se necessario, recarregue a pagina para resetar o widget.

Se os graficos nao atualizarem:

1. Confirme `GET /api/observability/scenarios`.
2. Confirme `GET /api/observability/samples?scenario=NORMAL`.
3. Troque o cenario no menu `Teste de carga`.

## Limites Conhecidos

- O storage validado e file-backed em `.runtime/uat`, nao PostgreSQL produtivo.
- A telemetria e sintetica e rotulada.
- O teste de carga do dashboard e simulado; o benchmark k6 real de 50 RPS/10 min
  foi executado na UAT local e documentado em
  `docs/testing/k6-consolidated-50rps-2026-09-14.md`.
- O QR Code do Google depende de liberacao externa da Google.
- O dominio profundo `uat.vertx.dwilon.com` depende de certificado Cloudflare
  compativel para HTTPS publico.
