using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(AgentConfiguration.FromEnvironment());
builder.Services.AddSingleton<PortalRagIndex>();
builder.Services.AddHttpClient<VllmChatClient>((services, client) =>
{
    var configuration = services.GetRequiredService<AgentConfiguration>();
    client.BaseAddress = new Uri(configuration.LlmBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
});

var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { name = "Vertx Support Agent", status = "ready" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", (AgentConfiguration configuration, PortalRagIndex rag) => Results.Ok(new
{
    status = "ready",
    mode = "rag-grounded-llm",
    model = configuration.Model,
    ragDocuments = rag.DocumentCount
}));

var api = app.MapGroup("/api/v1").WithTags("Support agent");
api.MapGet("/health/ready", (AgentConfiguration configuration, PortalRagIndex rag) => Results.Ok(new
{
    status = "ready",
    mode = "rag-grounded-llm",
    model = configuration.Model,
    ragDocuments = rag.DocumentCount
}));

api.MapPost("/chat", async (
    AgentChatRequest request,
    AgentConfiguration configuration,
    PortalRagIndex rag,
    VllmChatClient llm,
    CancellationToken ct) =>
{
    var validationErrors = AgentPolicy.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var channel = AgentPolicy.NormalizeChannel(request.Channel);
    var messages = request.Messages ?? [];
    var latestUserMessage = AgentPolicy.LatestUserMessage(messages);
    var denied = AgentPolicy.TryDeny(latestUserMessage);
    if (denied is not null)
    {
        return Results.Ok(AgentChatResponse.Refused(configuration.Model, denied));
    }

    var retrieval = rag.Search(latestUserMessage, configuration.RagMinScore);
    if (!retrieval.HasEvidence)
    {
        return Results.Ok(AgentChatResponse.Refused(
            configuration.Model,
            "Não tenho contexto autorizado no RAG do Vertx para responder isso. Posso ajudar com mapa do portal, lançamentos, dashboard, monitoramento, alertas, teste de carga, manual, Swagger, acesso e agente."));
    }

    var prompt = AgentPromptBuilder.Build(channel, messages, retrieval);
    string reply;
    try
    {
        reply = await llm.CompleteAsync(prompt, configuration, ct).ConfigureAwait(false);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("LLM local do subagente indisponível.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Tempo esgotado ao consultar o LLM local do subagente.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (JsonException)
    {
        return Results.Problem("Resposta inválida do LLM local do subagente.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var safeReply = AgentPolicy.SanitizeModelReply(reply, retrieval);
    return Results.Ok(new AgentChatResponse(
        safeReply,
        configuration.Model,
        "rag-grounded-llm",
        retrieval.Citations,
        null));
})
.WithSummary("Conversa com o subagente real usando LLM local em GPU e RAG governado.");

app.Run();
