using System.Text.RegularExpressions;

internal sealed record RagDocument(string Id, string Title, string[] Tags, string Content);
internal sealed record RagSearchResult(RagDocument[] Documents, RagCitation[] Citations, double Score)
{
    public bool HasEvidence => Documents.Length > 0;
}

internal sealed partial class PortalRagIndex
{
    private static readonly string[] StopWords =
    [
        "a", "ao", "aos", "as", "com", "como", "da", "das", "de", "do", "dos",
        "e", "em", "fica", "ficam", "me", "no", "nos", "o", "os", "para",
        "por", "qual", "quais", "que", "um", "uma"
    ];

    private readonly IndexedDocument[] documents = PortalKnowledge.Documents
        .Select(document => new IndexedDocument(
            document,
            Tokenize($"{document.Id} {document.Title} {string.Join(' ', document.Tags)} {document.Content}").ToArray()))
        .ToArray();

    public int DocumentCount => documents.Length;

    public RagSearchResult Search(string query, double minScore)
    {
        var normalizedQuery = Normalize(query);
        var wantsSmallTalk = IsAllowedSmallTalk(normalizedQuery);
        var queryTokens = Tokenize(query).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (queryTokens.Length == 0 && !wantsSmallTalk)
        {
            return new RagSearchResult([], [], 0);
        }

        var wantsOverview = queryTokens.Any(token => token is "ajuda" or "mapa" or "portal" or "tela" or "tudo" or "onde" or "posicao" or "posicoes" or "localizacao" or "layout" or "visual" or "objeto" or "objetos" or "painel" or "paineis");
        var scored = documents
            .Select(document => new
            {
                document.Document,
                Score = Score(document, queryTokens, wantsOverview, wantsSmallTalk)
            })
            .Where(item => item.Score >= minScore)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Document.Id, StringComparer.OrdinalIgnoreCase)
            .Take(wantsOverview || wantsSmallTalk ? 8 : 5)
            .ToArray();

        var selected = scored.Select(item => item.Document).ToArray();
        return new RagSearchResult(
            selected,
            selected.Select(document => new RagCitation(document.Id, document.Title)).ToArray(),
            scored.Sum(item => item.Score));
    }

    public static string Normalize(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var withoutMarks = new string(normalized.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        return TokenCleanupRegex().Replace(withoutMarks.ToLowerInvariant(), " ").Trim();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return Normalize(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2 && !StopWords.Contains(token, StringComparer.OrdinalIgnoreCase));
    }

    private static double Score(IndexedDocument document, string[] queryTokens, bool wantsOverview, bool wantsSmallTalk)
    {
        var score = 0.0;
        foreach (var token in queryTokens)
        {
            var matches = document.Tokens.Count(item => item.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (matches > 0)
            {
                score += Math.Min(matches, 4);
            }

            if (document.Document.Tags.Any(tag => Normalize(tag) == token))
            {
                score += 2.0;
            }
        }

        if (wantsOverview && document.Document.Tags.Contains("overview", StringComparer.OrdinalIgnoreCase))
        {
            score += 8.0;
        }

        if (wantsSmallTalk && document.Document.Tags.Contains("conversa-social", StringComparer.OrdinalIgnoreCase))
        {
            score += 10.0;
        }

        return score;
    }

    private static bool IsAllowedSmallTalk(string normalizedQuery)
    {
        if (normalizedQuery is "oi" or "ola" or "bom dia" or "boa tarde" or "boa noite" or "tudo bem" or "valeu" or "obrigado" or "obrigada" or "tchau")
        {
            return true;
        }

        var phrases = new[]
        {
            "e ai",
            "como vai",
            "quem e voce",
            "qual seu nome",
            "voce pode ajudar",
            "pode me ajudar",
            "me ajuda",
            "ate logo",
            "ate mais"
        };

        return phrases.Any(phrase => normalizedQuery.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex TokenCleanupRegex();

    private sealed record IndexedDocument(RagDocument Document, string[] Tokens);
}

internal static class PortalKnowledge
{
    public static readonly RagDocument[] Documents =
    [
        new(
            "agent.social",
            "Conversa social permitida",
            ["conversa-social", "cumprimento", "saudacao", "oi", "ola", "bom-dia", "boa-tarde", "boa-noite", "obrigado", "tchau", "identidade", "ajuda"],
            """
O agente pode conversar de forma natural em cumprimentos, agradecimentos, despedidas e perguntas simples de identidade ou disponibilidade, como oi, olá, bom dia, boa tarde, boa noite, tudo bem, obrigado, tchau, quem é você, qual seu nome e você pode ajudar. A resposta deve ser cordial, em português do Brasil, e direcionar a conversa para ajuda no portal Vertx. Se o usuário pedir assuntos pessoais, notícias, clima, programação, política, entretenimento ou qualquer tema fora do portal e fora dessa cordialidade simples, o agente deve recusar e explicar que só pode apoiar o uso do sistema Vertx.
"""),
        new(
            "portal.overview",
            "Mapa geral do portal",
            ["overview", "ajuda", "mapa", "portal", "localizacao", "posicao"],
            """
O portal autenticado possui três áreas fixas de referência: menu lateral escuro à esquerda, topo operacional acima do conteúdo principal e corpo principal à direita do menu. A ordem visual do corpo principal é: título Centro de comando financeiro, cards executivos, grade de gráficos, bloco de trabalho com Novo lançamento à esquerda e Teste de carga à direita, tabela Lançamentos, seção Clientes, seção Monitoramento do sistema e seção Controle de alertas. O Agente Vertx fica fixo no canto inferior direito. Swagger fica fora do menu lateral, em /swagger.
"""),
        new(
            "portal.visual_layout_snapshot",
            "Mapa visual detalhado da tela autenticada",
            ["overview", "layout", "visual", "imagem", "objetos", "tela", "mapa", "localizacao", "posicao", "posicoes"],
            """
Na tela autenticada em desktop, o usuário deve se orientar assim:
1. Menu lateral: ocupa toda a coluna esquerda, com fundo escuro. No topo do menu ficam o ícone e o texto Fluxo de Caixa, e abaixo aparece o ambiente Produção ou UAT. A navegação vem logo abaixo, nesta ordem de cima para baixo: Dashboard, Monitoramento, Alertas, Teste de carga, Lançamentos, Clientes e Manual.
2. Topo operacional: fica no alto da área principal, à direita do menu lateral. À esquerda do topo aparecem o breadcrumb Produção / Organização Alfa e o título Centro de comando financeiro. À direita aparecem, em linha, os badges Banco req/s, Alertas, Administrador Financeiro Alfa e o botão Sair.
3. Primeira linha do conteúdo: logo abaixo do título ficam seis cards horizontais, da esquerda para a direita: Créditos, Débitos, Saldo projetado, Banco total req/s, p95 API e Alertas ativos.
4. Grade principal de gráficos: abaixo dos cards há dois gráficos grandes lado a lado. À esquerda fica Fluxo diário, com créditos e débitos por data. À direita fica Banco req/s, com leitura, escrita e total.
5. Segunda linha de gráficos: abaixo da primeira grade, novamente lado a lado. À esquerda fica Latência, com p50, p95 e p99. À direita fica Filas e projeção, com Outbox, Rabbit, Projetados e Duplicados.
6. Área operacional abaixo dos gráficos: abaixo da seção Latência e Filas e projeção fica o bloco com dois painéis. À esquerda, estreito, fica Novo lançamento. À direita, largo, fica Teste de carga.
7. Novo lançamento: dentro do painel esquerdo da área operacional, a ordem dos campos é Crédito/Débito, Conta, Valor, Data, Descrição, Cliente e botão Registrar lançamento.
8. Teste de carga: dentro do painel direito da área operacional, os botões de cenário ficam no topo em duas linhas. Abaixo deles ficam os cards Leitura banco, Escrita banco, Total banco e Erros 5xx.
9. Lançamentos: abaixo da área operacional fica uma tabela larga chamada Lançamentos, com colunas Data, Descrição, Tipo e Valor.
10. Clientes: abaixo da tabela Lançamentos fica a seção Clientes.
11. Monitoramento do sistema: mais abaixo, após Clientes, fica a seção Monitoramento do sistema. Primeiro aparecem os cards RPS leitura, RPS escrita, Outbox pendente e Error budget. Abaixo desses cards vem a grade de saúde com Entries API, Read DB, RabbitMQ, Redis, Observability e AI boundary.
12. Controle de alertas: no fim da página fica a seção Controle de alertas. Ela mostra regras com checkbox em duas colunas, status ok/ativo/silenciado e, abaixo, o feed de evento ativo ou Sem alerta ativo.
13. Agente: o botão flutuante Posso te ajudar? fica sempre no canto inferior direito, sobreposto ao conteúdo. Ao clicar nele aparecem as opções Conversar por chat, Conversar local e Conversar por ligação acima do botão.

Ao responder onde fica algo, cite primeiro o caminho pelo menu lateral quando existir e depois a posição física na página. Não confunda a ordem do menu lateral com a ordem das seções no corpo da página: no menu, Monitoramento e Alertas aparecem antes de Teste de carga; no corpo visual, Monitoramento e Controle de alertas aparecem depois de Lançamentos e Clientes, mais abaixo na rolagem.
"""),
        new(
            "login.access",
            "Login e acesso",
            ["login", "senha", "recaptcha", "acesso", "entrar"],
            """
A tela de login aparece antes do portal autenticado e fica centralizada. A ordem visual é: marca Fluxo de Caixa, título Acesso operacional, campo Login, campo Senha, caixa Google reCAPTCHA v2, botão Entrar e link Manual de uso. A validação acontece no BFF com senha local e token reCAPTCHA. Credenciais de teste autorizadas para o examinador: login admin@admin.com e senha Vtx-1d7d875ea260072afc7fa86d.
"""),
        new(
            "layout.sidebar",
            "Menu lateral",
            ["menu", "lateral", "sidebar", "navegacao"],
            """
O menu lateral fica fixo na coluna esquerda depois do login, com fundo escuro. No topo aparecem a marca Fluxo de Caixa e o ambiente Produção ou UAT. Abaixo ficam os atalhos nesta ordem vertical: Dashboard, Monitoramento, Alertas, Teste de carga, Lançamentos, Clientes e Manual. Esses atalhos levam para seções da mesma página ou abrem o manual. No rodapé do menu há o status Sistema ready, com req/s atual e o cenário selecionado.
"""),
        new(
            "layout.topbar",
            "Topo operacional",
            ["topo", "cabecalho", "usuario", "sair", "badges"],
            """
O topo operacional fica acima do conteúdo principal, à direita do menu lateral. À esquerda mostra o breadcrumb de ambiente e organização e o título Centro de comando financeiro. À direita ficam os badges, nesta ordem: Banco req/s, Alertas, usuário logado Administrador Financeiro Alfa e o botão Sair.
"""),
        new(
            "dashboard.metrics",
            "Dashboard e cards executivos",
            ["dashboard", "metricas", "saldo", "credito", "debito"],
            """
Dashboard é a primeira seção abaixo do topo e também o primeiro item do menu lateral. Logo abaixo do título Centro de comando financeiro ficam seis cards em linha, da esquerda para a direita: Créditos, Débitos, Saldo projetado, Banco total req/s, p95 API e Alertas ativos. Serve para enxergar rapidamente posição financeira, carga do banco, latência e quantidade de alertas.
"""),
        new(
            "dashboard.charts",
            "Gráficos do dashboard",
            ["graficos", "grafico", "fluxo", "latencia", "filas", "projecao"],
            """
Os gráficos ficam logo abaixo dos cards do Dashboard. A primeira linha da grade mostra Fluxo diário à esquerda e Banco req/s à direita. A segunda linha mostra Latência à esquerda e Filas e projeção à direita. Fluxo diário compara créditos e débitos por data. Banco req/s mostra leitura, escrita e total. Latência mostra p50, p95 e p99. Filas e projeção mostra outbox, Rabbit, projetados e duplicados.
"""),
        new(
            "agent.mcp_runtime",
            "MCP readonly do portal",
            ["mcp", "runtime", "dados", "saldo", "creditos", "debitos", "latencia", "filas", "projecoes", "req", "rps", "cliente", "monitoramento"],
            """
Quando a pergunta pedir números atuais da tela, o SupportAgent.Api consulta um MCP readonly interno do portal antes de chamar o LLM. Esse MCP lê os mesmos serviços do dashboard: Entries API para créditos, débitos, lançamentos, contas e clientes; Consolidation API para consolidado diário, saldo projetado no read model e lag/outbox; Observability Simulation API para cenário atual, req/s de leitura e escrita, total de banco, p50, p95, p99, erros, filas, projeções, duplicados, error budget, saúde dos componentes e alertas ativos pelas regras padrão. O MCP é somente leitura, não cria lançamento, não altera alerta, não muda cenário e não acessa secrets.
"""),
        new(
            "entries.form",
            "Novo lançamento",
            ["lancamento", "lancamentos", "registrar", "credito", "debito", "valor", "data"],
            """
Novo lançamento fica abaixo dos gráficos Latência e Filas e projeção, no painel estreito da esquerda da área operacional. Primeiro o usuário escolhe Crédito ou Débito. Depois preenche Conta, Valor, Data, Descrição e Cliente. O botão Registrar lançamento fica na parte inferior do painel e grava a movimentação. O agente apenas orienta o uso da tela; ele não cria lançamento automaticamente.
"""),
        new(
            "entries.list",
            "Tabela de lançamentos",
            ["tabela", "lancamentos", "historico", "valor"],
            """
A seção Lançamentos fica abaixo do bloco de Novo lançamento e do Teste de carga. Ela ocupa a largura principal da página e lista o histórico confirmado em tabela com colunas Data, Descrição, Tipo e Valor. Tipo aparece como Crédito ou Débito.
"""),
        new(
            "customers.seed",
            "Clientes",
            ["clientes", "cliente", "cadastro", "seed"],
            """
Clientes fica no menu lateral e também como seção abaixo da tabela de Lançamentos. Ela lista clientes seed da UAT em cards simples com nome e versão. No formulário Novo lançamento, o campo Cliente usa essa base para vincular uma movimentação.
"""),
        new(
            "observability.monitoring",
            "Monitoramento",
            ["monitoramento", "monitor", "sistema", "banco", "outbox", "health", "rabbit", "redis", "error budget"],
            """
Monitoramento fica como segundo item do menu lateral, mas a seção Monitoramento do sistema aparece mais abaixo no corpo da página, depois de Clientes. Primeiro ela mostra quatro cards em linha: RPS leitura, RPS escrita, Outbox pendente e Error budget. Logo abaixo há uma grade de saúde com Entries API, Read DB, RabbitMQ, Redis, Observability e AI boundary.
"""),
        new(
            "observability.alerts",
            "Controle de alertas",
            ["alertas", "alerta", "regras", "incidente", "silenciado"],
            """
Alertas fica como terceiro item do menu lateral, mas a seção Controle de alertas aparece no fim do corpo da página, abaixo de Monitoramento do sistema. O título Controle de alertas mostra badge Normal ou Incidente simulado. A lista de regras possui checkbox para habilitar ou silenciar cada alerta, métrica atual, limite e status em duas colunas. Abaixo fica o feed de eventos ativos ou a mensagem Sem alerta ativo.
"""),
        new(
            "observability.loadtest",
            "Teste de carga",
            ["teste", "carga", "k6", "rps", "cenario", "spike", "recovery"],
            """
Teste de carga fica abaixo dos gráficos Latência e Filas e projeção, no painel largo à direita da área operacional. Também tem atalho no menu lateral. No topo do painel ficam os botões de cenário em duas linhas: Normal, Carga 50, Carga 100, Pico 200 e Recuperação. Abaixo dos botões aparecem os cards Leitura banco, Escrita banco, Total banco e Erros 5xx. O benchmark k6 real de 50 RPS por 10 minutos está documentado em docs/testing.
"""),
        new(
            "docs.manual",
            "Manual de uso",
            ["manual", "documentacao", "instrucoes"],
            """
Manual aparece em dois lugares: na tela de login, abaixo do botão Entrar, e no último item do menu lateral depois do login. Ele abre /manual.html em nova aba com instruções de uso, segurança, rotas, operação, RAG do agente e troubleshooting.
"""),
        new(
            "docs.swagger",
            "Swagger e OpenAPI",
            ["swagger", "openapi", "rotas", "endpoint", "api"],
            """
Swagger não fica no menu lateral. Ele deve ser acessado em /swagger no mesmo domínio. O contrato OpenAPI bruto fica em /openapi/v1.json. As principais áreas expostas pelo BFF são login/recaptcha, agente, entries, consolidated, observability e health checks.
"""),
        new(
            "agent.modes",
            "Agente Vertx",
            ["agente", "robo", "chat", "conversar", "ligacao", "telefone", "local", "microfone", "voz", "asr", "tts", "matcha"],
            """
O Agente Vertx fica fixo no canto inferior direito da tela autenticada. Ao clicar no ícone, aparecem três opções acima dele: Conversar por chat, Conversar local e Conversar por ligação. Conversar por chat chama o SupportAgent.Api, que usa RAG governado, MCP readonly do portal para dados atuais da tela e LLM Qwen local em GPU. Conversar local usa o microfone do computador ou celular, grava WAV na taxa nativa do navegador, transcreve com Qwen3-ASR local, ignora áudio vazio, sem nexo ou sem contexto autorizado, consulta o mesmo RAG/LLM governado e pode usar o MCP readonly quando a fala pedir saldo, créditos, débitos, latência, filas, projeções, req/s ou alertas. A resposta por voz continua curta e em texto plano. A fala principal usa Matcha TTS atual no backend freds-cml-stress-1000, entregue ao navegador como WAV; speechSynthesis do navegador fica apenas como fallback se o Matcha estiver indisponível ou não autorizado. Conversar por ligação recebe um telefone com DDD, solicita uma chamada pelo bridge Vero dedicado sem caller ID fixo e usa agente SIP próprio: Matcha TTS freds-cml-stress-1000, normalização de pontuação, filtros de backchannel e respostas governadas pelo RAG/MCP. Durante a orientação telefônica, a aba que iniciou a chamada abre um stream exclusivo da sessão; o portal só executa rolagem e destaque visual quando recebe um evento explícito com um item permitido citado na conversa.
"""),
        new(
            "agent.rag_policy",
            "RAG, MCP e política do agente",
            ["rag", "mcp", "tts", "matcha", "politica", "policy", "controle", "permitido", "bloqueado", "llm"],
            """
O SupportAgent.Api usa LLM local em GPU, mas não responde livremente. Antes do LLM, a pergunta passa por política de bloqueio contra prompt injection, segredos, tokens, arquivos sensíveis e comandos destrutivos. Depois passa por recuperação RAG sobre documentos curados do portal. Se a pergunta pedir números atuais da tela, o agente consulta o MCP readonly do portal e adiciona esse snapshot ao prompt junto com o RAG. Se não houver evidência suficiente, o chat recusa e o modo de voz local ignora silenciosamente. O prompt enviado ao LLM contém apenas o contexto autorizado recuperado e, quando aplicável, o snapshot MCP readonly. A API retorna fontes internas em campo separado para auditoria e interface. O modo de voz local usa ASR local, a mesma política do chat e Matcha TTS freds-cml-stress-1000 para fala principal. O modo de ligação solicita chamada pelo bridge Vero dedicado e mantém a mesma política de resposta do agente, sem executar ações financeiras.
"""),
        new(
            "security.enterprise",
            "Segurança implementada",
            ["seguranca", "segurança", "enterprise", "protecao", "proteção"],
            """
A segurança visível começa no login com senha e Google reCAPTCHA v2 validado no BFF. A sessão autenticada mostra o usuário no topo. A arquitetura usa headers de tenant e usuário, containers com usuário não root, filesystem read-only, cap_drop ALL, no-new-privileges e health checks. O agente possui RAG governado para evitar resposta sem contexto autorizado e recusar segredos ou comandos destrutivos.
"""),
        new(
            "environments.urls",
            "Endereços públicos",
            ["producao", "produção", "uat", "url", "endereco", "dominio"],
            """
Produção pública: https://vertx.dwilon.com/. UAT reservada: https://uat.vertx.dwilon.com/. Dentro do portal, o ambiente aparece no topo esquerdo e também na marca do menu lateral.
""")
    ];
}
