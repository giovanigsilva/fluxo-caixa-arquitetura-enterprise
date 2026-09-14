namespace Vertx.CashFlow.Assistant.Contracts;

public static class AssistantFeature
{
    public const bool AssistantEnabled = true;
    public const string Boundary = "SupportAgent.Api com RAG governado e LLM local em GPU; sem ASR, TTS, discagem real ou acesso direto a banco nesta etapa.";
}

public sealed record AssistantToolGrant(
    string TenantId,
    string UserId,
    string[] AllowedTools,
    DateTimeOffset ExpiresAt);
