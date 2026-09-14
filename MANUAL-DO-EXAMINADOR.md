# Manual Do Examinador - Fluxo de Caixa Vertx

Versao: UAT publica de avaliacao em 2026-09-14.

Este arquivo e a porta de entrada do examinador dentro do repositorio. O manual
completo de uso e operacao esta em [`docs/manual-de-uso.md`](docs/manual-de-uso.md)
e tambem foi publicado no portal em `https://vertx.dwilon.com/manual.html`.

## Leitura Recomendada

1. Leia este arquivo para entender rapidamente o que avaliar.
2. Leia [`README.md`](README.md) para subir a API, testar, operar e consultar a
   arquitetura geral.
3. Leia [`docs/manual-de-uso.md`](docs/manual-de-uso.md) para o manual de
   instrucao completo de cada item da aplicacao.
4. Leia [`docs/requirements-traceability.md`](docs/requirements-traceability.md)
   para cruzar requisito do PDF, status e evidencia.
5. Abra o Swagger para ver contratos, schemas, exemplos e status codes:
   `https://vertx.dwilon.com/swagger`.

## Enderecos De Avaliacao

Ambiente publico:

- Aplicacao: `https://vertx.dwilon.com/`
- Manual no portal: `https://vertx.dwilon.com/manual.html`
- Swagger UI: `https://vertx.dwilon.com/swagger`
- OpenAPI JSON: `https://vertx.dwilon.com/openapi/v1.json`
- Health ready: `https://vertx.dwilon.com/health/ready`

Ambiente local UAT:

- Frontend: `http://127.0.0.1:6230`
- Manual local: `http://127.0.0.1:6230/manual.html`
- BFF/API: `http://127.0.0.1:6210`
- Swagger UI local: `http://127.0.0.1:6210/swagger`
- OpenAPI JSON local: `http://127.0.0.1:6210/openapi/v1.json`
- Health ready local: `http://127.0.0.1:6210/health/ready`

## Credenciais De Teste

Estas credenciais foram autorizadas para exposicao ao examinador neste ambiente
tecnico:

```text
Login: admin@admin.com
Senha: Vtx-1d7d875ea260072afc7fa86d
```

O login pelo portal exige Google reCAPTCHA v2 checkbox. O token do reCAPTCHA e
validado no BFF antes da criacao da sessao.

## O Que O Sistema Entrega

A fatia executavel demonstra uma arquitetura enterprise para fluxo de caixa
multi-tenant:

- BFF publico em ASP.NET Core.
- SPA React/Vite em portugues do Brasil.
- Login com senha e reCAPTCHA validado server-side.
- API de lancamentos financeiros.
- Lancamentos de credito e debito com valor, data, conta, descricao e cliente.
- Idempotencia em criacao de lancamentos.
- Estorno de lancamentos.
- Consolidacao diaria.
- Exportacao de extrato em CSV, XLSX e PDF.
- Management API com usuarios, roles, permissoes e capabilities.
- Observability Simulation API.
- Dashboard com graficos financeiros e operacionais.
- Monitoramento sintetico de APIs, banco, filas e error budget.
- Controle visual de alertas.
- Menu de teste de carga sintetico.
- Agente Vertx flutuante no canto inferior direito, com chat real via
  SupportAgent API, RAG governado, LLM local em GPU e respostas longas
  formatadas, alem de opcoes visuais para conversa local e ligacao.
- Swagger/OpenAPI documentado.
- Scripts de bootstrap, build, teste, smoke, status e subida.
- Docker Compose para UAT e production.

## Como Avaliar Pelo Portal

1. Acesse `https://vertx.dwilon.com/`.
2. Entre com `admin@admin.com`.
3. Use a senha `Vtx-1d7d875ea260072afc7fa86d`.
4. Resolva o Google reCAPTCHA.
5. Navegue pelo menu lateral.
6. Crie lancamentos de credito e debito em datas diferentes.
7. Veja o dashboard atualizar os totais e graficos.
8. Alterne os cenarios no menu `Teste de carga`.
9. Ative e silencie regras em `Alertas`.
10. Consulte `https://vertx.dwilon.com/swagger` para revisar a API.

## Como Avaliar Pelo Codigo

Subir UAT do zero:

```bash
./scripts/bootstrap.sh --env uat
./scripts/build.sh --env uat
./scripts/up.sh --env uat
```

Validar:

```bash
./scripts/test.sh --env uat
./scripts/smoke.sh --env uat
./scripts/status.sh --env uat
```

Verificar manual publicado localmente:

```bash
curl -fsSI http://127.0.0.1:6230/manual.html
```

Verificar readiness da API:

```bash
curl -fsS http://127.0.0.1:6210/health/ready
```

## Como Cada Item Funciona

O detalhe funcional de cada item esta documentado em
[`docs/manual-de-uso.md`](docs/manual-de-uso.md). O arquivo cobre:

- acesso e credenciais;
- menu principal;
- dashboard;
- graficos;
- lancamentos;
- tabela de lancamentos;
- clientes;
- monitoramento;
- controle de alertas;
- teste de carga;
- agente Vertx;
- Swagger;
- seguranca;
- QR Code anti-IA e status da liberacao Google;
- operacao local;
- troubleshooting;
- limites conhecidos.

## Aderencia Ao Desafio

A matriz [`docs/requirements-traceability.md`](docs/requirements-traceability.md)
cruza cada requisito do PDF com o que esta implementado, documentado, validado ou
pendente. Ela deve ser usada como checklist tecnico do examinador.

## Seguranca Implementada

Controles ativos nesta entrega:

- BFF como fronteira publica.
- Login com senha.
- Google reCAPTCHA v2 validado no servidor.
- Secret key do reCAPTCHA apenas no BFF.
- Hash de senha em variavel de ambiente do container.
- Secrets fora do Git em `.secrets/{env}`.
- Publicacao por Cloudflare Tunnel.
- Portas locais vinculadas a `127.0.0.1`.
- Containers com usuario nao-root.
- Filesystem read-only onde aplicavel.
- `tmpfs` para escrita temporaria.
- `no-new-privileges`.
- `cap_drop: ALL`.
- Idempotencia em lancamentos.
- Controle de concorrencia com `If-Match` em cadastros.
- Auditoria basica.
- Outbox antes da consolidacao.

Controles preparados para producao real:

- Keycloak/OIDC com PKCE.
- MFA/passkey.
- PostgreSQL com RLS/TLS.
- Vault.
- RabbitMQ com retry, DLQ e TLS.
- Redis para cache/quota.
- Rate limit, CSRF e cookies de sessao reais.
- SAST, DAST, SBOM e scan de imagens.

## AI-First

A entrega foi conduzida em modelo AI-first: requisitos foram transformados em
contratos executaveis, Swagger, scripts, documentacao operacional e validacoes.
Isso torna o sistema legivel para humanos e agentes de IA, sem delegar decisoes
financeiras automaticas a uma IA em tempo de execucao.

## Pontos De Atencao

- A telemetria do dashboard e sintetica e rotulada.
- O menu de teste de carga nao executa carga real contra o ambiente publico.
- O storage validado da UAT e file-backed em `.runtime/uat`.
- O QR Code anti-IA depende de allowlist externa da Google no reCAPTCHA Fraud
  Defense; hoje esta ativo o reCAPTCHA v2 checkbox.
- `uat.vertx.dwilon.com` pode exigir certificado Cloudflare avancado ou
  customizado por ser subdominio profundo.
