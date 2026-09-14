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
      "name": "Support agent",
      "description": "Subagente de apoio com LLM local em GPU, RAG governado, MCP readonly e ligação Vero."
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
    "/api/agent/chat": {
      "post": {
        "tags": ["Support agent"],
        "operationId": "chatWithSupportAgent",
        "summary": "Conversa com o subagente",
        "description": "Encaminha a conversa para o SupportAgent.Api. O subagente usa RAG governado antes de chamar o LLM local em GPU. Quando a pergunta pede dados atuais da tela, ele consulta o MCP readonly do portal para saldo, créditos, débitos, consolidado, latência, filas, projeções, req/s, saúde e alertas.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/AgentChatRequest" },
              "example": {
                "sessionId": "ps_00000000000000000000000000000000",
                "channel": "portal-chat",
                "scenarioId": "NORMAL",
                "messages": [
                  { "role": "user", "text": "Como está o saldo projetado, créditos, débitos, latência, filas e total de req/s da tela?" }
                ]
              }
            }
          }
        },
        "responses": {
          "200": { "$ref": "#/components/responses/AgentChatResponse" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/agent/mcp/portal-snapshot": {
      "get": {
        "tags": ["Support agent"],
        "operationId": "getPortalMcpSnapshot",
        "summary": "Consulta MCP readonly do portal",
        "description": "Retorna o snapshot readonly usado pelo agente para responder perguntas sobre os dados atuais da tela. O MCP consolida Entries API, Consolidation API e Observability Simulation API sem executar ações financeiras.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          {
            "name": "scenarioId",
            "in": "query",
            "required": false,
            "schema": { "type": "string", "example": "NORMAL" },
            "description": "Cenário de monitoramento selecionado no dashboard."
          }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/PortalMcpSnapshotResponse" }
        }
      }
    },
    "/api/agent/tts/synthesize": {
      "post": {
        "tags": ["Support agent"],
        "operationId": "synthesizeSupportAgentSpeech",
        "summary": "Sintetiza voz do agente com Matcha TTS",
        "description": "Encaminha texto limpo para o Matcha TTS atual do backend freds-cml-stress-1000 e retorna áudio WAV tocável pelo navegador. É usado pela conversa local do portal como caminho principal de fala; a voz nativa do navegador fica apenas como fallback de resiliência.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/AgentTtsRequest" },
              "example": {
                "sessionId": "ps_00000000000000000000000000000000",
                "text": "Olá seja bem vindo, em que posso te ajudar?"
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Áudio WAV PCM 16-bit mono gerado pelo Matcha TTS.",
            "headers": {
              "X-Audio-Sample-Rate": { "schema": { "type": "integer", "example": 16000 } },
              "X-Matcha-Voice-Id": { "schema": { "type": "string", "example": "freds-cml-stress-1000" } },
              "X-Matcha-Pcm-Bytes": { "schema": { "type": "integer", "example": 96000 } }
            },
            "content": {
              "audio/wav": {
                "schema": { "type": "string", "format": "binary" }
              }
            }
          },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/agent/voice/turn": {
      "post": {
        "tags": ["Support agent"],
        "operationId": "talkWithSupportAgentByVoice",
        "summary": "Conversa local por microfone",
        "description": "Recebe um WAV capturado pelo navegador em multipart/form-data, transcreve no ASR local Qwen3-ASR e responde com o mesmo RAG/LLM governado do SupportAgent.Api. Quando a fala pede indicadores atuais, consulta o MCP readonly do portal e responde de forma curta. Áudio vazio, sem nexo ou sem contexto autorizado retorna voice-ignored/asr-empty sem fala no portal. A reprodução de voz usa Matcha TTS no backend freds-cml-stress-1000 como caminho principal e speechSynthesis do navegador apenas como fallback.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": {
          "required": true,
          "content": {
            "multipart/form-data": {
              "schema": { "$ref": "#/components/schemas/AgentVoiceTurnRequest" }
            }
          }
        },
        "responses": {
          "200": { "$ref": "#/components/responses/AgentVoiceTurnResponse" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/agent/call/health": {
      "get": {
        "tags": ["Support agent"],
        "operationId": "getSupportAgentCallHealth",
        "summary": "Consulta disponibilidade da ligação Vero",
        "description": "Consulta se o bridge Vero dedicado do agente está habilitado e pronto para receber solicitações de ligação do portal.",
        "responses": {
          "200": { "$ref": "#/components/responses/AgentTelephonyHealthResponse" }
        }
      }
    },
    "/api/agent/call/start": {
      "post": {
        "tags": ["Support agent"],
        "operationId": "startSupportAgentCall",
        "summary": "Solicita ligação com agente pela Vero",
        "description": "Recebe um telefone brasileiro com DDD, normaliza o número e solicita a chamada no bridge Vero dedicado. O start da ligação registra callId + sessionId + tenant + user para permitir orientação visual somente na tela que pediu a ligação, mas não dispara roteiro visual automático. Destaques de tela só acontecem por evento explícito de suporte apontando um item específico. O bridge não fixa caller ID na Vero, usa agente SIP dedicado, Matcha TTS freds-cml-stress-1000, normalização de pontuação e política de suporte restrita ao portal.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" }
        ],
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": { "$ref": "#/components/schemas/AgentCallStartRequest" },
              "example": {
                "sessionId": "ps_00000000000000000000000000000000",
                "phoneNumber": "(31) 99999-9999",
                "scenarioId": "NORMAL"
              }
            }
          }
        },
        "responses": {
          "200": { "$ref": "#/components/responses/AgentCallStartResponse" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "409": { "$ref": "#/components/responses/Conflict" },
          "503": { "$ref": "#/components/responses/ServiceUnavailable" }
        }
      }
    },
    "/api/agent/call/events": {
      "get": {
        "tags": ["Support agent"],
        "operationId": "streamSupportAgentCallEvents",
        "summary": "Escuta orientação visual da ligação",
        "description": "Abre um stream SSE de orientação visual para uma ligação já solicitada. O SupportAgent valida callId, sessionId, X-Tenant-Id e X-User-Id contra a sessão registrada no start, garantindo que apenas a aba/usuário que iniciou a chamada receba rolagem e destaque visual. O stream envia eventos portal-focus com um targetId permitido por vez; comentários keepalive mantêm a conexão aberta.",
        "parameters": [
          { "$ref": "#/components/parameters/TenantIdHeader" },
          { "$ref": "#/components/parameters/UserIdHeader" },
          {
            "name": "callId",
            "in": "query",
            "required": true,
            "schema": { "type": "string" },
            "description": "Identificador da chamada retornado por POST /api/agent/call/start.",
            "example": "portal-support-00000000000000000000000000000000"
          },
          {
            "name": "sessionId",
            "in": "query",
            "required": true,
            "schema": { "type": "string" },
            "description": "Sessão web autenticada que solicitou a chamada.",
            "example": "ps_00000000000000000000000000000000"
          }
        ],
        "responses": {
          "200": { "$ref": "#/components/responses/AgentCallScreenEventStream" },
          "400": { "$ref": "#/components/responses/BadRequest" },
          "403": { "$ref": "#/components/responses/Forbidden" },
          "404": { "$ref": "#/components/responses/NotFound" }
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
      "AgentChatResponse": {
        "description": "Resposta do subagente com modo de execução e fontes RAG/MCP.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/AgentChatResponse" },
            "example": {
              "reply": "Saldo projetado: R$ 120.000,00. Créditos: R$ 180.000,00. Débitos: R$ 60.000,00. Banco: 40 req/s.",
              "model": "Qwen/Qwen3.5-35B-A3B-GPTQ-Int4",
              "mode": "rag-mcp-grounded-llm",
              "citations": [
                { "id": "dashboard.metrics", "title": "Dashboard e cards executivos" },
                { "id": "agent.mcp_runtime", "title": "MCP readonly do portal" }
              ],
              "refusalReason": null
            }
          }
        }
      },
      "PortalMcpSnapshotResponse": {
        "description": "Snapshot readonly dos dados financeiros e operacionais atuais do portal usado pelo agente.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/PortalMcpSnapshot" }
          }
        }
      },
      "AgentVoiceTurnResponse": {
        "description": "Transcrição e resposta do agente por voz local.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/AgentVoiceTurnResponse" },
            "example": {
              "transcript": "Onde fica o novo lançamento?",
              "reply": "Novo lançamento fica abaixo dos gráficos, no painel da esquerda.",
              "model": "Qwen/Qwen3.5-35B-A3B-GPTQ-Int4",
              "mode": "voice-rag-grounded-llm",
              "citations": [
                { "id": "entries.form", "title": "Novo lançamento" }
              ],
              "refusalReason": null,
              "asrModel": "Qwen/Qwen3-ASR-0.6B",
              "language": "Portuguese",
              "asrLatencyMs": 180.4,
              "inputSampleRate": 48000
            }
          }
        }
      },
      "AgentTelephonyHealthResponse": {
        "description": "Estado do bridge Vero usado pelo agente.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/AgentTelephonyHealthResponse" },
            "example": {
              "enabled": true,
              "provider": "vero",
              "callerId": "",
              "status": "ready",
              "detail": "{\"status\":\"ready\"}"
            }
          }
        }
      },
      "AgentCallStartResponse": {
        "description": "Ligação solicitada pelo portal.",
        "content": {
          "application/json": {
            "schema": { "$ref": "#/components/schemas/AgentCallStartResponse" },
            "example": {
              "status": "requested",
              "callId": "portal-support-00000000000000000000000000000000",
              "phoneNumber": "(31) 99999-9999",
              "provider": "vero",
              "callerId": "",
              "message": "Chamada solicitada pela Vero. O agente Vertx vai orientar pelo telefone.",
              "guidedTargets": []
            }
          }
        }
      },
      "AgentCallScreenEventStream": {
        "description": "Stream SSE session-scoped com eventos de orientação visual da ligação.",
        "content": {
          "text/event-stream": {
            "schema": {
              "type": "string",
              "example": "event: portal-focus\ndata: {\"type\":\"portal-focus\",\"callId\":\"portal-support-00000000000000000000000000000000\",\"targetId\":\"new-entry-panel\",\"label\":\"Novo lançamento\",\"source\":\"telephony\",\"occurredAt\":\"2026-09-14T12:00:00Z\"}\n\n"
            }
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
      "AgentChatRequest": {
        "type": "object",
        "required": ["messages"],
        "properties": {
          "sessionId": { "type": "string", "nullable": true, "example": "ps_00000000000000000000000000000000" },
          "channel": { "type": "string", "enum": ["portal-chat", "portal-voice", "telephony-support"], "example": "portal-chat" },
          "scenarioId": { "type": "string", "nullable": true, "description": "Cenário atual do dashboard usado pelo MCP readonly quando a pergunta pede métricas da tela.", "example": "NORMAL" },
          "messages": {
            "type": "array",
            "minItems": 1,
            "maxItems": 16,
            "items": { "$ref": "#/components/schemas/AgentChatMessage" }
          }
        }
      },
      "AgentChatMessage": {
        "type": "object",
        "required": ["role", "text"],
        "properties": {
          "role": { "type": "string", "enum": ["user", "assistant", "agent"], "example": "user" },
          "text": { "type": "string", "minLength": 1, "maxLength": 2000, "example": "Onde fica o Dashboard?" }
        }
      },
      "AgentTtsRequest": {
        "type": "object",
        "required": ["text"],
        "properties": {
          "sessionId": { "type": "string", "nullable": true, "example": "ps_00000000000000000000000000000000" },
          "text": { "type": "string", "minLength": 1, "maxLength": 600, "description": "Texto em português já adequado para fala. O backend remove Markdown, fontes e URLs antes de chamar o Matcha.", "example": "Olá seja bem vindo, em que posso te ajudar?" }
        }
      },
      "AgentCallStartRequest": {
        "type": "object",
        "required": ["phoneNumber"],
        "properties": {
          "sessionId": { "type": "string", "nullable": true, "example": "ps_00000000000000000000000000000000" },
          "phoneNumber": { "type": "string", "description": "Telefone brasileiro com DDD. O backend aceita formatos com +55, espaços, parênteses e hífen, mas envia ao bridge Vero em DDD+número.", "example": "(31) 99999-9999" },
          "scenarioId": { "type": "string", "nullable": true, "description": "Cenário atual do dashboard usado para contextualizar a ligação e o roteiro visual.", "example": "NORMAL" }
        }
      },
      "AgentCallStartResponse": {
        "type": "object",
        "required": ["status", "callId", "phoneNumber", "provider", "callerId", "message", "guidedTargets"],
        "properties": {
          "status": { "type": "string", "enum": ["requested"], "example": "requested" },
          "callId": { "type": "string", "description": "Identificador do job/chamada aceito pelo bridge.", "example": "portal-support-00000000000000000000000000000000" },
          "phoneNumber": { "type": "string", "description": "Telefone normalizado para exibição.", "example": "(31) 99999-9999" },
          "provider": { "type": "string", "example": "vero" },
          "callerId": { "type": "string", "description": "Caller ID configurado para a chamada de suporte; vazio quando o bridge não fixa origem.", "example": "" },
          "message": { "type": "string", "description": "Mensagem operacional exibida pelo portal." },
          "guidedTargets": {
            "type": "array",
            "description": "Lista reservada para eventos explícitos de orientação; no start da ligação fica vazia para evitar destaque automático da tela inteira.",
            "items": { "$ref": "#/components/schemas/AgentCallGuideTarget" }
          }
        }
      },
      "AgentCallGuideTarget": {
        "type": "object",
        "required": ["targetId", "label", "delayMs"],
        "properties": {
          "targetId": { "type": "string", "description": "ID DOM do item destacado pelo portal.", "example": "new-entry-panel" },
          "label": { "type": "string", "description": "Nome humano do item.", "example": "Novo lançamento" },
          "delayMs": { "type": "integer", "format": "int32", "description": "Campo legado para roteiros antigos; o fluxo atual de ligação usa SSE session-scoped em /api/agent/call/events.", "example": 0 }
        }
      },
      "AgentCallScreenEvent": {
        "type": "object",
        "required": ["type", "callId", "targetId", "label", "source", "occurredAt"],
        "properties": {
          "type": { "type": "string", "enum": ["portal-focus"], "example": "portal-focus" },
          "callId": { "type": "string", "description": "Chamada à qual o evento pertence.", "example": "portal-support-00000000000000000000000000000000" },
          "targetId": { "type": "string", "description": "ID permitido do elemento que a tela deve rolar e destacar.", "example": "new-entry-panel" },
          "label": { "type": "string", "description": "Nome humano exibível do item.", "example": "Novo lançamento" },
          "source": { "type": "string", "description": "Origem interna do evento.", "example": "telephony" },
          "sourceText": { "type": "string", "nullable": true, "description": "Trecho de fala ou transcrição que motivou o destaque." },
          "occurredAt": { "type": "string", "format": "date-time" }
        }
      },
      "AgentTelephonyHealthResponse": {
        "type": "object",
        "required": ["enabled", "provider", "callerId", "status"],
        "properties": {
          "enabled": { "type": "boolean", "example": true },
          "provider": { "type": "string", "example": "vero" },
          "callerId": { "type": "string", "example": "" },
          "status": { "type": "string", "enum": ["disabled", "misconfigured", "ready", "unavailable", "timeout"], "example": "ready" },
          "detail": { "type": "string", "nullable": true, "description": "Resposta curta do bridge ou motivo de indisponibilidade." }
        }
      },
      "AgentChatResponse": {
        "type": "object",
        "required": ["reply", "model", "mode", "citations"],
        "properties": {
          "reply": { "type": "string", "description": "Resposta final gerada pelo LLM local, limitada pelo RAG governado e enriquecida pelo MCP readonly quando aplicável." },
          "model": { "type": "string", "example": "Qwen/Qwen3.5-35B-A3B-GPTQ-Int4" },
          "mode": { "type": "string", "enum": ["rag-grounded-llm", "rag-mcp-grounded-llm", "policy-refusal"], "example": "rag-mcp-grounded-llm" },
          "citations": {
            "type": "array",
            "items": { "$ref": "#/components/schemas/RagCitation" }
          },
          "refusalReason": { "type": "string", "nullable": true, "description": "Motivo quando a política ou falta de evidência bloqueia a resposta." }
        }
      },
      "AgentVoiceTurnRequest": {
        "type": "object",
        "required": ["file"],
        "properties": {
          "file": { "type": "string", "format": "binary", "description": "Arquivo WAV mono capturado pelo navegador." },
          "sessionId": { "type": "string", "nullable": true, "example": "ps_00000000000000000000000000000000" },
          "channel": { "type": "string", "enum": ["portal-voice"], "example": "portal-voice" },
          "scenarioId": { "type": "string", "nullable": true, "description": "Cenário atual do dashboard usado pelo MCP readonly quando a fala pede métricas da tela.", "example": "LOAD_100" },
          "sampleRate": { "type": "integer", "format": "int32", "nullable": true, "description": "Taxa de captura do navegador, normalmente 44100 ou 48000 Hz.", "example": 48000 }
        }
      },
      "AgentVoiceTurnResponse": {
        "type": "object",
        "required": ["transcript", "reply", "model", "mode", "citations", "asrModel"],
        "properties": {
          "transcript": { "type": "string", "description": "Texto reconhecido pelo ASR local." },
          "reply": { "type": "string", "description": "Resposta final do agente governada pelo RAG e, quando aplicável, enriquecida pelo MCP readonly. Pode vir vazia quando o modo for voice-ignored ou asr-empty." },
          "model": { "type": "string", "example": "Qwen/Qwen3.5-35B-A3B-GPTQ-Int4" },
          "mode": { "type": "string", "enum": ["voice-rag-grounded-llm", "voice-rag-mcp-grounded-llm", "voice-ignored", "policy-refusal", "asr-empty"], "example": "voice-rag-mcp-grounded-llm" },
          "citations": {
            "type": "array",
            "items": { "$ref": "#/components/schemas/RagCitation" }
          },
          "refusalReason": { "type": "string", "nullable": true },
          "asrModel": { "type": "string", "example": "Qwen/Qwen3-ASR-0.6B" },
          "language": { "type": "string", "nullable": true, "example": "Portuguese" },
          "asrLatencyMs": { "type": "number", "format": "double", "nullable": true, "example": 180.4 },
          "inputSampleRate": { "type": "integer", "format": "int32", "nullable": true, "example": 48000 }
        }
      },
      "PortalMcpSnapshot": {
        "type": "object",
        "required": ["available", "dataSource", "environment", "scenarioId", "generatedAt", "tenantId", "userId", "accountCount", "customerCount", "entryCount", "credits", "debits", "projectedBalance", "dailyRows", "consolidatedCredits", "consolidatedDebits", "consolidatedNetMovement", "consolidatedEntryCount", "readModelOutboxPending", "metrics", "health", "activeAlerts"],
        "properties": {
          "available": { "type": "boolean", "description": "Indica se a consulta MCP retornou os serviços internos com sucesso.", "example": true },
          "dataSource": { "type": "string", "example": "portal-mcp-readonly" },
          "environment": { "type": "string", "example": "uat" },
          "scenarioId": { "type": "string", "description": "Cenário de observabilidade usado no snapshot.", "example": "NORMAL" },
          "generatedAt": { "type": "string", "format": "date-time" },
          "tenantId": { "type": "string", "example": "org-alpha" },
          "userId": { "type": "string", "example": "user-admin-alpha" },
          "accountCount": { "type": "integer", "format": "int32", "example": 2 },
          "customerCount": { "type": "integer", "format": "int32", "example": 3 },
          "entryCount": { "type": "integer", "format": "int32", "example": 8 },
          "credits": { "type": "number", "format": "decimal", "example": 180000 },
          "debits": { "type": "number", "format": "decimal", "example": 60000 },
          "projectedBalance": { "type": "number", "format": "decimal", "example": 120000 },
          "dailyRows": { "type": "integer", "format": "int32", "description": "Quantidade de linhas do consolidado diário projetado.", "example": 5 },
          "consolidatedCredits": { "type": "number", "format": "decimal", "example": 180000 },
          "consolidatedDebits": { "type": "number", "format": "decimal", "example": 60000 },
          "consolidatedNetMovement": { "type": "number", "format": "decimal", "example": 120000 },
          "consolidatedEntryCount": { "type": "integer", "format": "int32", "example": 8 },
          "readModelOutboxPending": { "type": "integer", "format": "int32", "example": 0 },
          "readModelOldestPendingOccurredAt": { "type": "string", "format": "date-time", "nullable": true },
          "metrics": { "$ref": "#/components/schemas/PortalMcpMetrics" },
          "health": {
            "type": "object",
            "additionalProperties": { "type": "string" },
            "description": "Saúde dos componentes exibidos no dashboard."
          },
          "activeAlerts": {
            "type": "array",
            "description": "Alertas disparados pelas regras padrão do dashboard.",
            "items": { "$ref": "#/components/schemas/PortalMcpAlert" }
          },
          "failureReason": { "type": "string", "nullable": true, "description": "Motivo quando available=false." }
        }
      },
      "PortalMcpMetrics": {
        "type": "object",
        "required": ["readRps", "writeRps", "p50Ms", "p95Ms", "p99Ms", "errors4xx", "errors5xx", "errors429", "outboxPending", "rabbitReady", "projectedEntries", "duplicatesIgnored"],
        "properties": {
          "readRps": { "type": "number", "format": "double", "example": 30 },
          "writeRps": { "type": "number", "format": "double", "example": 10 },
          "p50Ms": { "type": "number", "format": "double", "example": 82 },
          "p95Ms": { "type": "number", "format": "double", "example": 160 },
          "p99Ms": { "type": "number", "format": "double", "example": 240 },
          "errors4xx": { "type": "integer", "format": "int32", "example": 1 },
          "errors5xx": { "type": "integer", "format": "int32", "example": 0 },
          "errors429": { "type": "integer", "format": "int32", "example": 0 },
          "outboxPending": { "type": "integer", "format": "int32", "example": 8 },
          "rabbitReady": { "type": "integer", "format": "int32", "example": 4 },
          "projectedEntries": { "type": "integer", "format": "int32", "example": 125 },
          "duplicatesIgnored": { "type": "integer", "format": "int32", "example": 0 },
          "errorBudgetRemaining": { "type": "number", "format": "double", "nullable": true, "example": 0.82 },
          "unit": { "type": "string", "nullable": true, "example": "requests" }
        }
      },
      "PortalMcpAlert": {
        "type": "object",
        "required": ["label", "severity", "value", "threshold"],
        "properties": {
          "label": { "type": "string", "example": "Latência p95" },
          "severity": { "type": "string", "enum": ["warning", "critical"], "example": "warning" },
          "value": { "type": "string", "example": "230 ms" },
          "threshold": { "type": "string", "example": ">= 220 ms" }
        }
      },
      "RagCitation": {
        "type": "object",
        "required": ["id", "title"],
        "properties": {
          "id": { "type": "string", "example": "dashboard.metrics" },
          "title": { "type": "string", "example": "Dashboard e cards executivos" }
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
