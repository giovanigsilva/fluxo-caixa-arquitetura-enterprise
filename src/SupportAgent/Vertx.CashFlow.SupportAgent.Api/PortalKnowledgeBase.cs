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

        var wantsOverview = queryTokens.Any(token => token is "ajuda" or "mapa" or "portal" or "tela" or "tudo" or "onde" or "posicao" or "localizacao");
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
O portal autenticado possui uma coluna lateral esquerda, um topo operacional e o conteúdo principal. A ordem visual do conteúdo principal é: topo com Centro de comando financeiro, cards do Dashboard, grade de gráficos, painel Novo lançamento à esquerda, painel Teste de carga à direita, tabela Lançamentos, seção Clientes, seção Monitoramento e seção Alertas. O Agente Vertx fica fixo no canto inferior direito. Swagger fica fora do menu lateral, em /swagger.
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
O menu lateral fica na coluna esquerda depois do login. No topo aparecem a marca Fluxo de Caixa e o ambiente. Abaixo ficam os atalhos nesta ordem: Dashboard, Monitoramento, Alertas, Teste de carga, Lançamentos, Clientes e Manual. No rodapé do menu há o status Sistema ready, com req/s atual e o cenário selecionado.
"""),
        new(
            "layout.topbar",
            "Topo operacional",
            ["topo", "cabecalho", "usuario", "sair", "badges"],
            """
O topo operacional fica acima do conteúdo principal, à direita do menu lateral. À esquerda mostra o breadcrumb de ambiente e organização e o título Centro de comando financeiro. À direita ficam os badges Banco req/s, Alertas, usuário logado e o botão Sair.
"""),
        new(
            "dashboard.metrics",
            "Dashboard e cards executivos",
            ["dashboard", "metricas", "saldo", "credito", "debito"],
            """
Dashboard é a primeira seção abaixo do topo e também o primeiro item do menu lateral. Ele tem seis cards: Créditos, Débitos, Saldo projetado, Banco total req/s, p95 API e Alertas ativos. Serve para enxergar rapidamente posição financeira, carga do banco, latência e quantidade de alertas.
"""),
        new(
            "dashboard.charts",
            "Gráficos do dashboard",
            ["graficos", "grafico", "fluxo", "latencia", "filas", "projecao"],
            """
Os gráficos ficam logo abaixo dos cards do Dashboard. A grade mostra Fluxo diário, Banco req/s, Latência e Filas e projeção. Fluxo diário compara créditos e débitos por data. Banco req/s mostra leitura, escrita e total. Latência mostra p50, p95 e p99. Filas e projeção mostra outbox, Rabbit, projetados e duplicados.
"""),
        new(
            "entries.form",
            "Novo lançamento",
            ["lancamento", "lancamentos", "registrar", "credito", "debito", "valor", "data"],
            """
Novo lançamento fica abaixo dos gráficos, no painel da esquerda. Primeiro o usuário escolhe Crédito ou Débito. Depois preenche Conta, Valor, Data, Descrição e Cliente. O botão Registrar lançamento grava a movimentação. O agente apenas orienta o uso da tela; ele não cria lançamento automaticamente.
"""),
        new(
            "entries.list",
            "Tabela de lançamentos",
            ["tabela", "lancamentos", "historico", "valor"],
            """
A seção Lançamentos fica abaixo do bloco de Novo lançamento e do Teste de carga. Ela lista o histórico confirmado em tabela com Data, Descrição, Tipo e Valor. Tipo aparece como Crédito ou Débito.
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
Monitoramento fica no menu lateral e a seção aparece depois de Clientes quando a página é rolada. Ele mostra RPS leitura, RPS escrita, Outbox pendente e Error budget. Logo abaixo há uma grade de saúde com Entries API, Read DB, RabbitMQ, Redis, Observability e AI boundary.
"""),
        new(
            "observability.alerts",
            "Controle de alertas",
            ["alertas", "alerta", "regras", "incidente", "silenciado"],
            """
Alertas fica no menu lateral e a seção aparece no fim do portal. O título Controle de alertas mostra badge Normal ou Incidente simulado. A lista de regras possui checkbox para habilitar ou silenciar cada alerta, métrica atual, limite e status. Abaixo fica o feed de eventos ativos.
"""),
        new(
            "observability.loadtest",
            "Teste de carga",
            ["teste", "carga", "k6", "rps", "cenario", "spike", "recovery"],
            """
Teste de carga fica abaixo dos gráficos, no painel da direita, e também tem atalho no menu lateral. Ele tem botões de cenário como Normal, Carga 50, Carga 100, Pico 200 e Recuperação. Abaixo dos botões aparecem Leitura banco, Escrita banco, Total banco e Erros 5xx. O benchmark k6 real de 50 RPS por 10 minutos está documentado em docs/testing.
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
            ["agente", "robo", "chat", "conversar", "ligacao", "telefone", "local", "microfone", "voz", "asr"],
            """
O Agente Vertx fica fixo no canto inferior direito da tela autenticada. Ao clicar no ícone, aparecem três opções acima dele: Conversar por chat, Conversar local e Conversar por ligação. Conversar por chat chama o SupportAgent.Api, que usa RAG governado e LLM Qwen local em GPU. Conversar local usa o microfone do computador ou celular, grava WAV na taxa nativa do navegador, transcreve com Qwen3-ASR local, ignora áudio vazio, sem nexo ou sem contexto autorizado, consulta o mesmo RAG/LLM governado e reproduz resposta curta em texto plano com a voz nativa do navegador em velocidade 1.8. Conversar por ligação está preparado visualmente; discagem real e provedor telefônico serão definidos depois.
"""),
        new(
            "agent.rag_policy",
            "RAG e política do agente",
            ["rag", "politica", "policy", "controle", "permitido", "bloqueado", "llm"],
            """
O SupportAgent.Api usa LLM local em GPU, mas não responde livremente. Antes do LLM, a pergunta passa por política de bloqueio contra prompt injection, segredos, tokens, arquivos sensíveis e comandos destrutivos. Depois passa por recuperação RAG sobre documentos curados do portal. Se não houver evidência suficiente, o chat recusa e o modo de voz local ignora silenciosamente. O prompt enviado ao LLM contém apenas o contexto autorizado recuperado. A API retorna fontes internas em campo separado para auditoria e interface. O modo de voz local usa ASR local e a mesma política do chat. O agente não executa ações financeiras nem realiza ligações.
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
