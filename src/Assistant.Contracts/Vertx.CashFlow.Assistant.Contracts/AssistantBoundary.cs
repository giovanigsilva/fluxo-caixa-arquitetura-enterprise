namespace Vertx.CashFlow.Assistant.Contracts;

public static class AssistantFeature
{
    public const bool AssistantEnabled = true;
    public const string Boundary = "SupportAgent.Api com RAG governado, MCP readonly do portal, LLM local em GPU e voz local por Qwen3-ASR; sem discagem real, telefonia SIP ou acesso direto a banco nesta etapa.";
}

public sealed record AssistantToolGrant(
    string TenantId,
    string UserId,
    string[] AllowedTools,
    DateTimeOffset ExpiresAt);
