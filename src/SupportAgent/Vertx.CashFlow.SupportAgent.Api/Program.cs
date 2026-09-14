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
builder.Services.AddHttpClient<QwenAsrClient>((services, client) =>
{
    var configuration = services.GetRequiredService<AgentConfiguration>();
    client.BaseAddress = new Uri(configuration.AsrBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(configuration.AsrTimeoutSeconds);
});

var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { name = "Vertx Support Agent", status = "ready" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", (AgentConfiguration configuration, PortalRagIndex rag) => Results.Ok(new
{
    status = "ready",
    mode = "rag-grounded-llm",
    model = configuration.Model,
    voice = "qwen-asr-browser-speech",
    ragDocuments = rag.DocumentCount
}));

var api = app.MapGroup("/api/v1").WithTags("Support agent");
api.MapGet("/health/ready", (AgentConfiguration configuration, PortalRagIndex rag) => Results.Ok(new
{
    status = "ready",
    mode = "rag-grounded-llm",
    model = configuration.Model,
    voice = "qwen-asr-browser-speech",
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

api.MapPost("/voice/turn", async (
    HttpRequest httpRequest,
    AgentConfiguration configuration,
    PortalRagIndex rag,
    VllmChatClient llm,
    QwenAsrClient asr,
    CancellationToken ct) =>
{
    if (!httpRequest.HasFormContentType)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["contentType"] = ["Envie multipart/form-data com o arquivo WAV no campo file."]
        });
    }

    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["Envie um áudio WAV no campo file."]
        });
    }

    if (file.Length > 24 * 1024 * 1024)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["O áudio precisa ter no máximo 24 MB."]
        });
    }

    if (!IsSupportedAudioContentType(file.ContentType))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["Use áudio WAV."]
        });
    }

    var inputSampleRate = int.TryParse(form["sampleRate"].FirstOrDefault(), out var parsedSampleRate) && parsedSampleRate > 0
        ? parsedSampleRate
        : (int?)null;
    AsrTranscription transcription;
    try
    {
        await using var audio = file.OpenReadStream();
        transcription = await asr.TranscribeAsync(audio, file.FileName, file.ContentType, ct).ConfigureAwait(false);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("ASR local do subagente indisponível.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Tempo esgotado ao consultar o ASR local do subagente.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (JsonException)
    {
        return Results.Problem("Resposta inválida do ASR local do subagente.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var transcript = transcription.Text?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(transcript))
    {
        return Results.Ok(new AgentVoiceTurnResponse(
            "",
            "",
            configuration.Model,
            "asr-empty",
            [],
            null,
            transcription.Model,
            transcription.Language,
            transcription.TotalLatencyMs,
            inputSampleRate));
    }

    if (AgentPolicy.ShouldIgnoreVoiceTranscript(transcript))
    {
        return Results.Ok(new AgentVoiceTurnResponse(
            transcript,
            "",
            configuration.Model,
            "voice-ignored",
            [],
            "Transcrição curta ou sem sinal suficiente para resposta por voz.",
            transcription.Model,
            transcription.Language,
            transcription.TotalLatencyMs,
            inputSampleRate));
    }

    var channel = AgentPolicy.NormalizeChannel(form["channel"].FirstOrDefault());
    if (string.IsNullOrWhiteSpace(form["channel"].FirstOrDefault()))
    {
        channel = "portal-voice";
    }

    var request = new AgentChatRequest(
        form["sessionId"].FirstOrDefault(),
        channel,
        [new AgentChatMessage("user", transcript)]);
    var validationErrors = AgentPolicy.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var messages = request.Messages ?? [];
    if (AgentPolicy.TryDeny(transcript) is not null)
    {
        return Results.Ok(new AgentVoiceTurnResponse(
            transcript,
            "",
            configuration.Model,
            "voice-ignored",
            [],
            "Transcrição bloqueada pela política do agente.",
            transcription.Model,
            transcription.Language,
            transcription.TotalLatencyMs,
            inputSampleRate));
    }

    var retrieval = rag.Search(transcript, configuration.RagMinScore);
    if (!retrieval.HasEvidence)
    {
        return Results.Ok(new AgentVoiceTurnResponse(
            transcript,
            "",
            configuration.Model,
            "voice-ignored",
            [],
            "Transcrição sem contexto autorizado para resposta por voz.",
            transcription.Model,
            transcription.Language,
            transcription.TotalLatencyMs,
            inputSampleRate));
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

    return Results.Ok(new AgentVoiceTurnResponse(
        transcript,
        AgentPolicy.SanitizeModelReply(reply, retrieval),
        configuration.Model,
        "voice-rag-grounded-llm",
        retrieval.Citations,
        null,
        transcription.Model,
        transcription.Language,
        transcription.TotalLatencyMs,
        inputSampleRate));
})
.WithSummary("Recebe áudio do navegador, transcreve com ASR local e responde com RAG/LLM governado.");

app.Run();

static bool IsSupportedAudioContentType(string? contentType)
{
    if (string.IsNullOrWhiteSpace(contentType))
    {
        return true;
    }

    return (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            && (contentType.Contains("wav", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("wave", StringComparison.OrdinalIgnoreCase)))
        || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);
}
