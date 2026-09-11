namespace Vertx.CashFlow.Assistant.Contracts;

public static class AssistantFeature
{
    public const bool AssistantEnabled = false;
    public const string Boundary = "Fase futura: sem ASR, LLM, TTS, voz, GPU ou acesso direto a banco.";
}

public sealed record AssistantToolGrant(
    string TenantId,
    string UserId,
    string[] AllowedTools,
    DateTimeOffset ExpiresAt);
