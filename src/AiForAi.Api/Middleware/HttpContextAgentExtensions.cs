using AiForAi.Api.Models;

namespace AiForAi.Api.Middleware;

public static class HttpContextAgentExtensions
{
    private const string AgentContextKey = "AuthenticatedAgent";

    public static void SetAuthenticatedAgent(this HttpContext context, Agent agent)
    {
        context.Items[AgentContextKey] = agent;
    }

    public static Agent? GetAuthenticatedAgent(this HttpContext context)
    {
        return context.Items.TryGetValue(AgentContextKey, out var value)
            ? value as Agent
            : null;
    }
}
