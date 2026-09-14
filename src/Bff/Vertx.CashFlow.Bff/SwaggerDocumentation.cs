internal static class SwaggerDocumentation
{
    public const string Html = """
<!doctype html>
<html lang="pt-BR">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Swagger - Fluxo de Caixa Vertx</title>
    <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
    <style>
      body { margin: 0; background: #f7f8fa; }
      .swagger-ui .topbar { background: #102033; }
      .swagger-ui .topbar .download-url-wrapper .select-label span { color: #fff; }
      .swagger-ui .info .title small { background: #0f766e; }
    </style>
  </head>
  <body>
    <div id="swagger-ui"></div>
    <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
    <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
    <script>
      window.ui = SwaggerUIBundle({
        url: "/openapi/v1.json",
        dom_id: "#swagger-ui",
        deepLinking: true,
        displayRequestDuration: true,
        filter: true,
        persistAuthorization: false,
        tryItOutEnabled: true,
        presets: [
          SwaggerUIBundle.presets.apis,
          SwaggerUIStandalonePreset
        ],
        layout: "StandaloneLayout"
      });
    </script>
  </body>
</html>
""";

    public const string OpenApiJson = """
{
  "openapi": "3.0.3",
  "info": {
    "title": "Vertx CashFlow Public API",
    "version": "v1",
    "description": "Swagger completo da superfície pública do BFF do Fluxo de Caixa Vertx. Todas as rotas abaixo são chamadas pelo BFF e encaminhadas aos serviços internos correspondentes. A entrada web usa login local com senha e Google reCAPTCHA v2 checkbox; as rotas de negócio desta fatia técnica ainda recebem tenant e usuário por headers."
  },
  "servers": [
    {
      "url": "https://vertx.dwilon.com",
      "description": "Publicação Cloudflare disponível"
    },
    {
      "url": "https://uat.vertx.dwilon.com",
      "description": "UAT público quando o certificado do subdomínio profundo estiver ativo"
    },
    {
      "url": "http://127.0.0.1:6210",
      "description": "BFF local"
    }
  ],
  "tags": [
    {
      "name": "Health",
      "description": "Sinais de vida e prontidão do BFF."
    },
    {
      "name": "Login approval",
      "description": "Login local com senha e validação Google reCAPTCHA v2 checkbox."
    },
    {
      "name": "Entries - customers",
      "description": "Clientes e contrapartes comerciais por tenant."
    },
    {
      "name": "Entries - accounts",
      "description": "Contas financeiras usadas nos lançamentos."
    },
    {
      "name": "Entries - categories",
      "description": "Categorias financeiras de crédito e débito."
    },
    {
      "name": "Entries - cost centers",
      "description": "Centros de custo simples."
    },
    {
      "name": "Entries - postings",
      "description": "Lançamentos financeiros, idempotência e estornos."
    },
    {
      "name": "Entries - statements",
      "description": "Geração e download de extratos."
    },
    {
      "name": "Entries - audit",
      "description": "Trilha de auditoria sanitizada."
    },
    {
      "name": "Management",
      "description": "Permissões, capabilities, usuários e perfis."
    },
    {
      "name": "Consolidation",
      "description": "Read model de saldos diários e reconstrução controlada."
    },
    {
      "name": "Observability",
      "description": "Cenários sintéticos para testes de monitoramento."
    }
  ],
  "paths": {
    "/": {
      "get": {
        "tags": ["Health"],
        "operationId": "getRoot",
        "summary": "Identifica o BFF",
        "description": "Retorna um payload pequeno indicando que o BFF do Fluxo de Caixa está respondendo.",
        "responses": {
          "200": {
            "description": "BFF respondendo.",
            "content": {
              "application/json": {
                "schema": {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "example": "Fluxo de Caixa" },
                    "bff": { "type": "string", "example": "ready" }
                  }
                }
              }
            }
          }
        }
      }
    },
    "/health/live": {
      "get": {
        "tags": ["Health"],
        "operationId": "getLiveHealth",
        "summary": "Liveness do BFF",
        "description": "Usado por orquestradores para saber se o processo HTTP está vivo.",
        "responses": {
          "200": { "$ref": "#/components/responses/LiveHealth" }
        }
      }
    },
    "/health/ready": {
      "get": {
        "tags": ["Health"],
        "operationId": "getReadyHealth",
        "summary": "Readiness do BFF",
        "description": "Usado para smoke test e publicação pública. Nesta fatia, a entrada web usa senha local e Google reCAPTCHA v2 checkbox.",
        "responses": {
          "200": { "$ref": "#/components/responses/ReadyHealth" }
        }
      }
    },
    "/bff/login/recaptcha/config": {
      "get": {
        "tags": ["Login approval"],
        "operationId": "getRecaptchaConfig",
        "summary": "Obtém configuração pública do reCAPTCHA",
        "description": "Retorna a site key pública e indica se o Google reCAPTCHA v2 checkbox está habilitado no BFF.",
        "responses": {
          "200": { "$ref": "#/components/responses/RecaptchaConfigResponse" }
        }
      }
    },
    "/bff/login/start": {
      "post": {
        "tags": ["Login approval"],
        "operationId": "startLoginApproval",
        "summary": "Valida senha e reCAPTCHA",
        "description": "Valida login, senha e token Google reCAPTCHA v2 checkbox para liberar a sessão web.",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/LoginStartRequest" },
              "example": {
                "email": "admin@admin.com",
                "password": "senha-local",
                "recaptchaToken": "token-gerado-pelo-widget-google"
              }
            }
          }
        },
        "responses": {
          "200": { "$ref": "#/components/responses/LoginSessionResponse" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "401": { "$ref": "#/components/responses/Unauthorized" },
          "403": { "$ref": "#/components/responses/Forbidden" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/entries/customers": {
      "get": {
        "tags": ["Entries - customers"],
        "operationId": "listCustomers",
        "summary": "Lista clientes",
        "description": "Lista clientes não excluídos do tenant informado.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/CustomerList" }
        }
      },
      "post": {
        "tags": ["Entries - customers"],
        "operationId": "createCustomer",
        "summary": "Cria cliente",
        "description": "Cria cliente ou contraparte comercial. Dados marcados como restritos permanecem sinalizados no registro.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertCustomer" },
        "responses": {
          "201": { "$ref": "#/components/responses/Customer" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/customers/{id}": {
      "put": {
        "tags": ["Entries - customers"],
        "operationId": "updateCustomer",
        "summary": "Atualiza cliente",
        "description": "Atualiza cliente com controle otimista. Informe a versão atual no header If-Match.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/ResourceIdPath" },
          { "$ref": "#/components/parameters/IfMatchHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertCustomer" },
        "responses": {
          "200": { "$ref": "#/components/responses/Customer" },
          "404": { "$ref": "#/components/responses/NotFound" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      },
      "delete": {
        "tags": ["Entries - customers"],
        "operationId": "deleteCustomer",
        "summary": "Exclui cliente logicamente",
        "description": "Marca o cliente como excluído sem apagar histórico financeiro. Exige If-Match.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/ResourceIdPath" },
          { "$ref": "#/components/parameters/IfMatchHeader" }
        ],
        "responses": {
          "204": { "description": "Cliente excluído logicamente." },
          "404": { "$ref": "#/components/responses/NotFound" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/accounts": {
      "get": {
        "tags": ["Entries - accounts"],
        "operationId": "listAccounts",
        "summary": "Lista contas",
        "description": "Retorna contas financeiras ativas do tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/AccountList" }
        }
      },
      "post": {
        "tags": ["Entries - accounts"],
        "operationId": "createAccount",
        "summary": "Cria conta",
        "description": "Cria uma conta de caixa. Moeda padrão: BRL.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertAccount" },
        "responses": {
          "201": { "$ref": "#/components/responses/Account" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/categories": {
      "get": {
        "tags": ["Entries - categories"],
        "operationId": "listCategories",
        "summary": "Lista categorias",
        "description": "Retorna categorias financeiras ativas do tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/CategoryList" }
        }
      },
      "post": {
        "tags": ["Entries - categories"],
        "operationId": "createCategory",
        "summary": "Cria categoria",
        "description": "Cria categoria de crédito ou débito.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertCategory" },
        "responses": {
          "201": { "$ref": "#/components/responses/Category" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/cost-centers": {
      "get": {
        "tags": ["Entries - cost centers"],
        "operationId": "listCostCenters",
        "summary": "Lista centros de custo",
        "description": "Retorna centros de custo ativos do tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/CostCenterList" }
        }
      },
      "post": {
        "tags": ["Entries - cost centers"],
        "operationId": "createCostCenter",
        "summary": "Cria centro de custo",
        "description": "Cria centro de custo simples para classificação gerencial.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertCostCenter" },
        "responses": {
          "201": { "$ref": "#/components/responses/CostCenter" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/entries": {
      "get": {
        "tags": ["Entries - postings"],
        "operationId": "listEntries",
        "summary": "Lista lançamentos",
        "description": "Lista lançamentos confirmados e estornos do tenant, ordenados por data de negócio e criação.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/EntryList" }
        }
      },
      "post": {
        "tags": ["Entries - postings"],
        "operationId": "postEntry",
        "summary": "Confirma lançamento financeiro",
        "description": "Cria lançamento financeiro imutável. O header Idempotency-Key é obrigatório para evitar duplicidade em retry.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/IdempotencyKeyHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/PostEntry" },
        "responses": {
          "201": { "$ref": "#/components/responses/Entry" },
          "200": { "$ref": "#/components/responses/EntryReplay" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/entries/{id}/reverse": {
      "post": {
        "tags": ["Entries - postings"],
        "operationId": "reverseEntry",
        "summary": "Estorna lançamento",
        "description": "Gera lançamento inverso e vincula o estorno ao lançamento original. Exige Idempotency-Key.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/ResourceIdPath" },
          { "$ref": "#/components/parameters/IdempotencyKeyHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/ReverseEntry" },
        "responses": {
          "201": { "$ref": "#/components/responses/Entry" },
          "200": { "$ref": "#/components/responses/EntryReplay" },
          "404": { "$ref": "#/components/responses/NotFound" },
          "409": { "$ref": "#/components/responses/Conflict" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/statements/exports": {
      "post": {
        "tags": ["Entries - statements"],
        "operationId": "createStatementExport",
        "summary": "Solicita extrato",
        "description": "Cria job assíncrono para gerar extrato em CSV, XLSX ou PDF.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/CreateStatementExport" },
        "responses": {
          "202": { "$ref": "#/components/responses/StatementExportJob" },
          "422": { "$ref": "#/components/responses/ValidationError" }
        }
      }
    },
    "/api/entries/statements/exports/{id}": {
      "get": {
        "tags": ["Entries - statements"],
        "operationId": "getStatementExport",
        "summary": "Consulta status do extrato",
        "description": "Retorna Pending, Completed ou Failed para o job informado.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/ResourceIdPath" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/StatementExportJob" },
          "404": { "$ref": "#/components/responses/NotFound" }
        }
      }
    },
    "/api/entries/statements/exports/{id}/download": {
      "get": {
        "tags": ["Entries - statements"],
        "operationId": "downloadStatementExport",
        "summary": "Baixa extrato gerado",
        "description": "Baixa o arquivo do job concluído. Retorna 409 enquanto a geração ainda estiver pendente.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "$ref": "#/components/parameters/ResourceIdPath" }
        ],
        "responses": {
          "200": {
            "description": "Arquivo CSV, XLSX ou PDF.",
            "content": {
              "text/csv": { "schema": { "type": "string", "format": "binary" } },
              "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": { "schema": { "type": "string", "format": "binary" } },
              "application/pdf": { "schema": { "type": "string", "format": "binary" } }
            }
          },
          "404": { "$ref": "#/components/responses/NotFound" },
          "409": { "$ref": "#/components/responses/Conflict" }
        }
      }
    },
    "/api/entries/audit": {
      "get": {
        "tags": ["Entries - audit"],
        "operationId": "listAuditRecords",
        "summary": "Lista auditoria",
        "description": "Retorna até 200 eventos de auditoria sanitizados do tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/AuditList" }
        }
      }
    },
    "/api/management/permissions": {
      "get": {
        "tags": ["Management"],
        "operationId": "listPermissions",
        "summary": "Lista catálogo de permissões",
        "description": "Retorna catálogo versionado e allowlist de permissões conhecidas pela aplicação.",
        "responses": {
          "200": { "$ref": "#/components/responses/PermissionCatalog" }
        }
      }
    },
    "/api/management/capabilities": {
      "get": {
        "tags": ["Management"],
        "operationId": "getCapabilities",
        "summary": "Consulta permissões efetivas",
        "description": "Retorna capabilities efetivas para menus, botões, colunas e bloqueios de interface.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/Capabilities" }
        }
      }
    },
    "/api/management/users": {
      "get": {
        "tags": ["Management"],
        "operationId": "listUsers",
        "summary": "Lista usuários",
        "description": "Lista usuários ativos do tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/UserList" }
        }
      },
      "post": {
        "tags": ["Management"],
        "operationId": "createUser",
        "summary": "Cria usuário de aplicação",
        "description": "Cria usuário de aplicação e vincula perfis existentes. Não manipula credenciais do provedor de identidade.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertUser" },
        "responses": {
          "201": { "$ref": "#/components/responses/User" },
          "400": { "$ref": "#/components/responses/BadRequest" }
        }
      }
    },
    "/api/management/roles": {
      "get": {
        "tags": ["Management"],
        "operationId": "listRoles",
        "summary": "Lista perfis",
        "description": "Lista perfis editáveis por tenant.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/RoleList" }
        }
      },
      "post": {
        "tags": ["Management"],
        "operationId": "createRole",
        "summary": "Cria perfil",
        "description": "Cria perfil usando apenas permissões existentes no catálogo allowlist.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": { "$ref": "#/components/requestBodies/UpsertRole" },
        "responses": {
          "201": { "$ref": "#/components/responses/Role" },
          "400": { "$ref": "#/components/responses/BadRequest" }
        }
      }
    },
    "/api/consolidated/daily": {
      "get": {
        "tags": ["Consolidation"],
        "operationId": "getDailyBalances",
        "summary": "Consulta consolidado diário",
        "description": "Consulta read model diário por tenant, com filtros opcionais e indicador de defasagem do outbox.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          { "name": "from", "in": "query", "schema": { "type": "string", "format": "date" }, "description": "Data inicial inclusive.", "example": "2026-09-01" },
          { "name": "to", "in": "query", "schema": { "type": "string", "format": "date" }, "description": "Data final inclusive.", "example": "2026-09-30" },
          { "name": "accountId", "in": "query", "schema": { "type": "string" }, "description": "Filtra por conta de caixa.", "example": "acc_00000000000000000000000000000000" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/DailyBalances" }
        }
      }
    },
    "/api/consolidated/rebuild": {
      "post": {
        "tags": ["Consolidation"],
        "operationId": "rebuildDailyBalances",
        "summary": "Reconstrói projeção do tenant",
        "description": "Marca os eventos do tenant para reprojeção controlada do read model.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "responses": {
          "202": {
            "description": "Reconstrução aceita.",
            "content": {
              "application/json": {
                "schema": {
                  "type": "object",
                  "properties": {
                    "tenantId": { "type": "string", "example": "org-alpha" },
                    "status": { "type": "string", "example": "accepted" },
                    "generation": { "type": "string", "example": "19fe1c2a7bf64a49b0c9ef736c4d0fd7" }
                  }
                }
              }
            }
          }
        }
      }
    },
    "/api/observability/scenarios": {
      "get": {
        "tags": ["Observability"],
        "operationId": "listObservabilityScenarios",
        "summary": "Lista cenários sintéticos",
        "description": "Retorna todos os cenários disponíveis para simular telemetria.",
        "responses": {
          "200": {
            "description": "Lista de cenários.",
            "content": {
              "application/json": {
                "schema": { "type": "array", "items": { "type": "string" } },
                "example": ["NORMAL", "LOAD_50", "LOAD_100", "SPIKE_200", "WORKER_DOWN", "BROKER_DOWN", "READ_DB_DOWN", "REDIS_DOWN", "DUPLICATE_EVENT", "RECOVERY"]
              }
            }
          }
        }
      }
    },
    "/api/observability/samples": {
      "get": {
        "tags": ["Observability"],
        "operationId": "getObservabilitySample",
        "summary": "Obtém amostra sintética",
        "description": "Retorna uma amostra determinística para o ambiente e cenário informados. O campo dataSource sempre indica synthetic.",
        "parameters": [
          { "name": "environment", "in": "query", "schema": { "type": "string", "default": "uat" }, "description": "Ambiente lógico.", "example": "uat" },
          { "name": "scenario", "in": "query", "schema": { "type": "string", "default": "NORMAL" }, "description": "Cenário sintético.", "example": "NORMAL" }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/SyntheticSample" }
        }
      }
    },
    "/api/observability/scenarios/{scenario}": {
      "post": {
        "tags": ["Observability"],
        "operationId": "selectObservabilityScenario",
        "summary": "Seleciona cenário sintético",
        "description": "Seleciona cenário para simulação sem alterar variáveis de processo, containers ou infraestrutura real.",
        "parameters": [
          { "name": "scenario", "in": "path", "required": true, "schema": { "type": "string" }, "description": "Nome do cenário.", "example": "LOAD_50" }
        ],
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/ScenarioSelection" },
              "example": { "environment": "uat" }
            }
          }
        },
        "responses": {
          "200": { "$ref": "#/components/responses/SyntheticSample" }
        }
      }
    },
    "/api/observability/stream": {
      "get": {
        "tags": ["Observability"],
        "operationId": "streamObservabilitySamples",
        "summary": "Abre stream SSE de telemetria",
        "description": "Envia uma amostra sintética por segundo em formato Server-Sent Events.",
        "parameters": [
          { "name": "environment", "in": "query", "schema": { "type": "string", "default": "uat" }, "description": "Ambiente lógico.", "example": "uat" },
          { "name": "scenario", "in": "query", "schema": { "type": "string", "default": "NORMAL" }, "description": "Cenário sintético.", "example": "NORMAL" }
        ],
        "responses": {
          "200": {
            "description": "Stream SSE com eventos data.",
            "content": {
              "text/event-stream": {
                "schema": { "type": "string", "example": "data: {\"dataSource\":\"synthetic\",\"environment\":\"uat\"}\\n\\n" }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "parameters": {
      "TenantIdHeader": {
        "name": "X-Tenant-Id",
        "in": "header",
        "required": false,
        "description": "Tenant lógico. Se omitido, usa o seed org-alpha.",
        "schema": { "type": "string", "default": "org-alpha" },
        "example": "org-alpha"
      },
      "UserIdHeader": {
        "name": "X-User-Id",
        "in": "header",
        "required": false,
        "description": "Usuário ator para auditoria e capabilities. Se omitido, usa o admin seed.",
        "schema": { "type": "string", "default": "user-admin-alpha" },
        "example": "user-admin-alpha"
      },
      "IdempotencyKeyHeader": {
        "name": "Idempotency-Key",
        "in": "header",
        "required": true,
        "description": "Chave única por operação financeira. Reutilizar a mesma chave com payload diferente retorna conflito de validação.",
        "schema": { "type": "string", "minLength": 8 },
        "example": "manual-202609111830000000"
      },
      "IfMatchHeader": {
        "name": "If-Match",
        "in": "header",
        "required": true,
        "description": "Versão atual do recurso para controle otimista. Exemplo: 1 ou \"1\".",
        "schema": { "type": "string" },
        "example": "1"
      },
      "ResourceIdPath": {
        "name": "id",
        "in": "path",
        "required": true,
        "description": "Identificador do recurso.",
        "schema": { "type": "string" },
        "example": "ent_00000000000000000000000000000000"
      }
    },
    "requestBodies": {
      "UpsertCustomer": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertCustomerRequest" },
            "example": {
              "legalName": "Empresa Cliente Alfa Ltda",
              "tradeName": "Cliente Alfa",
              "email": "financeiro@cliente.example",
              "phone": "+55 11 4002-8922",
              "restrictedData": false
            }
          }
        }
      },
      "UpsertAccount": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertAccountRequest" },
            "example": {
              "name": "Banco Principal",
              "currency": "BRL",
              "description": "Conta operacional"
            }
          }
        }
      },
      "UpsertCategory": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertCategoryRequest" },
            "example": { "name": "Receita recorrente", "type": "Credit" }
          }
        }
      },
      "UpsertCostCenter": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertCostCenterRequest" },
            "example": { "name": "Operações" }
          }
        }
      },
      "PostEntry": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/PostEntryRequest" },
            "example": {
              "accountId": "acc_00000000000000000000000000000000",
              "type": "Credit",
              "amount": "150.00",
              "businessDate": "2026-09-11",
              "description": "Recebimento operacional",
              "customerId": null,
              "categoryId": null,
              "costCenterId": null
            }
          }
        }
      },
      "ReverseEntry": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/ReverseEntryRequest" },
            "example": { "reason": "Correção operacional", "businessDate": "2026-09-11" }
          }
        }
      },
      "CreateStatementExport": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/CreateStatementExportRequest" },
            "example": {
              "format": "csv",
              "from": "2026-09-01",
              "to": "2026-09-30",
              "accountId": "acc_00000000000000000000000000000000"
            }
          }
        }
      },
      "UpsertUser": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertUserRequest" },
            "example": {
              "username": "operador",
              "displayName": "Operador Financeiro",
              "roleIds": ["role-finance-alpha"]
            }
          }
        }
      },
      "UpsertRole": {
        "required": true,
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/UpsertRoleRequest" },
            "example": {
              "name": "Analista financeiro",
              "permissions": ["cashflow.entries.read", "cashflow.entries.write"]
            }
          }
        }
      }
    },
    "responses": {
      "LiveHealth": {
        "description": "Processo vivo.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/LiveHealthResponse" },
            "example": { "status": "live" }
          }
        }
      },
      "ReadyHealth": {
        "description": "BFF pronto para receber chamadas.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/ReadyHealthResponse" },
            "example": { "status": "ready", "auth": "password-recaptcha-v2" }
          }
        }
      },
      "RecaptchaConfigResponse": {
        "description": "Configuração pública do Google reCAPTCHA v2.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/RecaptchaConfigResponse" }
          }
        }
      },
      "LoginSessionResponse": {
        "description": "Sessão aprovada.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/LoginSessionResponse" }
          }
        }
      },
      "Customer": {
        "description": "Cliente.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/Customer" } }
        }
      },
      "CustomerList": {
        "description": "Lista de clientes.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Customer" } } }
        }
      },
      "Account": {
        "description": "Conta de caixa.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/CashAccount" } }
        }
      },
      "AccountList": {
        "description": "Lista de contas.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/CashAccount" } } }
        }
      },
      "Category": {
        "description": "Categoria financeira.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/Category" } }
        }
      },
      "CategoryList": {
        "description": "Lista de categorias.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Category" } } }
        }
      },
      "CostCenter": {
        "description": "Centro de custo.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/CostCenter" } }
        }
      },
      "CostCenterList": {
        "description": "Lista de centros de custo.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/CostCenter" } } }
        }
      },
      "Entry": {
        "description": "Lançamento criado.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/Entry" } }
        }
      },
      "EntryReplay": {
        "description": "Replay idempotente de lançamento já criado.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/Entry" } }
        }
      },
      "EntryList": {
        "description": "Lista de lançamentos.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Entry" } } }
        }
      },
      "StatementExportJob": {
        "description": "Job de extrato.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/StatementExportJob" } }
        }
      },
      "AuditList": {
        "description": "Lista de auditoria.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/AuditRecord" } } }
        }
      },
      "PermissionCatalog": {
        "description": "Catálogo versionado de permissões.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/PermissionCatalogResponse" } }
        }
      },
      "Capabilities": {
        "description": "Capabilities efetivas.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/CapabilitiesResponse" } }
        }
      },
      "User": {
        "description": "Usuário.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/User" } }
        }
      },
      "UserList": {
        "description": "Lista de usuários.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/User" } } }
        }
      },
      "Role": {
        "description": "Perfil.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/Role" } }
        }
      },
      "RoleList": {
        "description": "Lista de perfis.",
        "content": {
          "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Role" } } }
        }
      },
      "DailyBalances": {
        "description": "Read model diário.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/DailyBalancesResponse" } }
        }
      },
      "SyntheticSample": {
        "description": "Amostra sintética.",
        "content": {
          "application/json": { "schema": { "$ref": "#/components/schemas/SyntheticSample" } }
        }
      },
      "BadRequest": {
        "description": "Requisição inválida.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      },
      "Unauthorized": {
        "description": "Credenciais inválidas.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      },
      "Forbidden": {
        "description": "Operação recusada.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      },
      "NotFound": {
        "description": "Recurso não encontrado."
      },
      "Conflict": {
        "description": "Conflito operacional ou recurso ainda não pronto.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      },
      "ServiceUnavailable": {
        "description": "Configuração local obrigatória ausente.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      },
      "ValidationError": {
        "description": "Erro de validação de negócio.",
        "content": {
          "application/problem+json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } },
          "application/json": { "schema": { "$ref": "#/components/schemas/ProblemDetails" } }
        }
      }
    },
    "schemas": {
      "LiveHealthResponse": {
        "type": "object",
        "required": ["status"],
        "properties": {
          "status": { "type": "string", "example": "live" }
        }
      },
      "ReadyHealthResponse": {
        "type": "object",
        "required": ["status", "auth"],
        "properties": {
          "status": { "type": "string", "example": "ready" },
          "auth": { "type": "string", "example": "password-recaptcha-v2" }
        }
      },
      "RecaptchaConfigResponse": {
        "type": "object",
        "required": ["provider", "enabled"],
        "properties": {
          "provider": { "type": "string", "enum": ["google-recaptcha-v2-checkbox"], "example": "google-recaptcha-v2-checkbox" },
          "enabled": { "type": "boolean", "example": true },
          "siteKey": { "type": "string", "nullable": true, "description": "Site key pública emitida pelo Google reCAPTCHA." }
        }
      },
      "LoginStartRequest": {
        "type": "object",
        "required": ["email", "password", "recaptchaToken"],
        "properties": {
          "email": { "type": "string", "format": "email", "example": "admin@admin.com" },
          "password": { "type": "string", "format": "password", "writeOnly": true, "example": "senha-local" },
          "recaptchaToken": { "type": "string", "writeOnly": true, "description": "Token retornado pelo widget Google reCAPTCHA v2 checkbox no navegador." }
        }
      },
      "LoginSessionResponse": {
        "type": "object",
        "required": ["status", "pendingSessionId", "userId", "displayName", "expiresAt"],
        "properties": {
          "status": { "type": "string", "enum": ["approved"], "example": "approved" },
          "pendingSessionId": { "type": "string", "example": "ps_00000000000000000000000000000000" },
          "userId": { "type": "string", "example": "user-admin-alpha" },
          "displayName": { "type": "string", "example": "Administrador Financeiro Alfa" },
          "expiresAt": { "type": "string", "format": "date-time" }
        }
      },
      "TenantEntityBase": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "tenantId": { "type": "string", "example": "org-alpha" },
          "version": { "type": "integer", "format": "int64", "example": 1 },
          "createdAt": { "type": "string", "format": "date-time" },
          "updatedAt": { "type": "string", "format": "date-time" },
          "isDeleted": { "type": "boolean", "example": false }
        }
      },
      "Customer": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "required": ["id", "tenantId", "legalName"],
            "properties": {
              "legalName": { "type": "string", "example": "Empresa Cliente Alfa Ltda" },
              "tradeName": { "type": "string", "nullable": true, "example": "Cliente Alfa" },
              "email": { "type": "string", "nullable": true, "example": "financeiro@cliente.example" },
              "phone": { "type": "string", "nullable": true, "example": "+55 11 4002-8922" },
              "restrictedData": { "type": "boolean", "example": false }
            }
          }
        ]
      },
      "UpsertCustomerRequest": {
        "type": "object",
        "required": ["legalName"],
        "properties": {
          "legalName": { "type": "string", "minLength": 1 },
          "tradeName": { "type": "string", "nullable": true },
          "email": { "type": "string", "nullable": true },
          "phone": { "type": "string", "nullable": true },
          "restrictedData": { "type": "boolean", "default": false }
        }
      },
      "CashAccount": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "required": ["id", "tenantId", "name", "currency"],
            "properties": {
              "name": { "type": "string", "example": "Banco Principal" },
              "currency": { "type": "string", "example": "BRL" },
              "description": { "type": "string", "nullable": true, "example": "Conta operacional" }
            }
          }
        ]
      },
      "UpsertAccountRequest": {
        "type": "object",
        "required": ["name"],
        "properties": {
          "name": { "type": "string", "minLength": 1 },
          "currency": { "type": "string", "nullable": true, "default": "BRL" },
          "description": { "type": "string", "nullable": true }
        }
      },
      "Category": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "required": ["id", "tenantId", "name", "type"],
            "properties": {
              "name": { "type": "string", "example": "Receita recorrente" },
              "type": { "type": "string", "enum": ["Credit", "Debit"], "example": "Credit" }
            }
          }
        ]
      },
      "UpsertCategoryRequest": {
        "type": "object",
        "required": ["name", "type"],
        "properties": {
          "name": { "type": "string", "minLength": 1 },
          "type": { "type": "string", "enum": ["Credit", "Debit", "credito", "debito"] }
        }
      },
      "CostCenter": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "required": ["id", "tenantId", "name"],
            "properties": {
              "name": { "type": "string", "example": "Operações" }
            }
          }
        ]
      },
      "UpsertCostCenterRequest": {
        "type": "object",
        "required": ["name"],
        "properties": {
          "name": { "type": "string", "minLength": 1 }
        }
      },
      "Entry": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "required": ["id", "tenantId", "accountId", "type", "amount", "businessDate", "description", "actorUserId"],
            "properties": {
              "accountId": { "type": "string", "example": "acc_00000000000000000000000000000000" },
              "type": { "type": "string", "enum": ["Credit", "Debit"], "example": "Credit" },
              "amount": { "type": "string", "pattern": "^[0-9]+\\.[0-9]{2}$", "example": "150.00" },
              "businessDate": { "type": "string", "format": "date", "example": "2026-09-11" },
              "description": { "type": "string", "example": "Recebimento operacional" },
              "customerId": { "type": "string", "nullable": true },
              "categoryId": { "type": "string", "nullable": true },
              "costCenterId": { "type": "string", "nullable": true },
              "reversalEntryId": { "type": "string", "nullable": true },
              "reversesEntryId": { "type": "string", "nullable": true },
              "reason": { "type": "string", "nullable": true },
              "actorUserId": { "type": "string", "example": "user-admin-alpha" }
            }
          }
        ]
      },
      "PostEntryRequest": {
        "type": "object",
        "required": ["accountId", "type", "amount", "businessDate", "description"],
        "properties": {
          "accountId": { "type": "string" },
          "type": { "type": "string", "enum": ["Credit", "Debit", "credito", "debito"] },
          "amount": { "type": "string", "description": "Valor positivo decimal com duas casas.", "example": "150.00" },
          "businessDate": { "type": "string", "format": "date" },
          "description": { "type": "string", "minLength": 1 },
          "customerId": { "type": "string", "nullable": true },
          "categoryId": { "type": "string", "nullable": true },
          "costCenterId": { "type": "string", "nullable": true }
        }
      },
      "ReverseEntryRequest": {
        "type": "object",
        "required": ["reason"],
        "properties": {
          "reason": { "type": "string", "minLength": 1 },
          "businessDate": { "type": "string", "format": "date", "nullable": true }
        }
      },
      "CreateStatementExportRequest": {
        "type": "object",
        "required": ["format", "from", "to"],
        "properties": {
          "format": { "type": "string", "enum": ["csv", "xlsx", "pdf"] },
          "from": { "type": "string", "format": "date" },
          "to": { "type": "string", "format": "date" },
          "accountId": { "type": "string", "nullable": true }
        }
      },
      "StatementExportJob": {
        "type": "object",
        "required": ["id", "tenantId", "format", "status", "requestedBy", "from", "to", "createdAt"],
        "properties": {
          "id": { "type": "string", "example": "stx_00000000000000000000000000000000" },
          "tenantId": { "type": "string", "example": "org-alpha" },
          "format": { "type": "string", "enum": ["csv", "xlsx", "pdf"] },
          "status": { "type": "string", "enum": ["Pending", "Completed", "Failed"], "example": "Pending" },
          "requestedBy": { "type": "string", "example": "user-admin-alpha" },
          "from": { "type": "string", "format": "date" },
          "to": { "type": "string", "format": "date" },
          "accountId": { "type": "string", "nullable": true },
          "fileName": { "type": "string", "nullable": true },
          "mimeType": { "type": "string", "nullable": true },
          "createdAt": { "type": "string", "format": "date-time" },
          "completedAt": { "type": "string", "format": "date-time", "nullable": true },
          "error": { "type": "string", "nullable": true }
        }
      },
      "AuditRecord": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "tenantId": { "type": "string" },
          "actorUserId": { "type": "string" },
          "action": { "type": "string", "example": "entries.post" },
          "resourceId": { "type": "string" },
          "occurredAt": { "type": "string", "format": "date-time" }
        }
      },
      "PermissionCatalogResponse": {
        "type": "object",
        "properties": {
          "catalogVersion": { "type": "integer", "example": 1 },
          "permissions": { "type": "array", "items": { "type": "string" }, "example": ["cashflow.entries.read", "cashflow.entries.write"] }
        }
      },
      "CapabilitiesResponse": {
        "type": "object",
        "properties": {
          "tenantId": { "type": "string", "example": "org-alpha" },
          "userId": { "type": "string", "example": "user-admin-alpha" },
          "permissions": { "type": "array", "items": { "type": "string" } },
          "expiresInSeconds": { "type": "integer", "example": 30 }
        }
      },
      "User": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "properties": {
              "username": { "type": "string", "example": "admin" },
              "displayName": { "type": "string", "example": "Administrador" },
              "roleIds": { "type": "array", "items": { "type": "string" } },
              "disabled": { "type": "boolean", "example": false }
            }
          }
        ]
      },
      "UpsertUserRequest": {
        "type": "object",
        "required": ["username", "displayName", "roleIds"],
        "properties": {
          "username": { "type": "string" },
          "displayName": { "type": "string" },
          "roleIds": { "type": "array", "items": { "type": "string" } }
        }
      },
      "Role": {
        "allOf": [
          { "$ref": "#/components/schemas/TenantEntityBase" },
          {
            "type": "object",
            "properties": {
              "name": { "type": "string", "example": "Financeiro" },
              "permissions": { "type": "array", "items": { "type": "string" } }
            }
          }
        ]
      },
      "UpsertRoleRequest": {
        "type": "object",
        "required": ["name", "permissions"],
        "properties": {
          "name": { "type": "string" },
          "permissions": { "type": "array", "items": { "type": "string" } }
        }
      },
      "DailyBalancesResponse": {
        "type": "object",
        "properties": {
          "tenantId": { "type": "string", "example": "org-alpha" },
          "generatedAt": { "type": "string", "format": "date-time" },
          "dataSource": { "type": "string", "example": "read-model" },
          "lag": {
            "type": "object",
            "properties": {
              "outboxPending": { "type": "integer", "example": 0 },
              "oldestPendingOccurredAt": { "type": "string", "format": "date-time", "nullable": true }
            }
          },
          "rows": {
            "type": "array",
            "items": { "$ref": "#/components/schemas/DailyBalanceRow" }
          }
        }
      },
      "DailyBalanceRow": {
        "type": "object",
        "properties": {
          "tenantId": { "type": "string" },
          "accountId": { "type": "string" },
          "businessDate": { "type": "string", "format": "date" },
          "credits": { "type": "number", "format": "double", "example": 150.00 },
          "debits": { "type": "number", "format": "double", "example": 0.00 },
          "dayMovement": { "type": "number", "format": "double", "example": 150.00 },
          "entryCount": { "type": "integer", "example": 1 },
          "updatedAt": { "type": "string", "format": "date-time" }
        }
      },
      "ScenarioSelection": {
        "type": "object",
        "required": ["environment"],
        "properties": {
          "environment": { "type": "string", "example": "uat" }
        }
      },
      "SyntheticSample": {
        "type": "object",
        "properties": {
          "dataSource": { "type": "string", "example": "synthetic" },
          "environment": { "type": "string", "example": "uat" },
          "scenarioId": { "type": "string", "example": "NORMAL" },
          "timestamp": { "type": "string", "format": "date-time" },
          "window": { "type": "string", "example": "PT60S" },
          "health": { "type": "object", "additionalProperties": true },
          "metrics": { "type": "object", "additionalProperties": true }
        }
      },
      "ProblemDetails": {
        "type": "object",
        "properties": {
          "type": { "type": "string", "nullable": true },
          "title": { "type": "string", "nullable": true },
          "status": { "type": "integer", "nullable": true },
          "detail": { "type": "string", "nullable": true },
          "instance": { "type": "string", "nullable": true },
          "code": { "type": "string", "nullable": true, "example": "idempotency.required" }
        }
      }
    }
  }
}
""";
}
