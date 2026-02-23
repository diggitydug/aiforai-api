using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;

namespace AiForAi.Api.Middleware;

public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAgentService agentService)
    {
        if (string.Equals(context.Request.Path, "/agents/register", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Authentication failed due to missing/invalid Authorization header for {Path}", context.Request.Path);
            await ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.").ExecuteAsync(context);
            return;
        }

        var apiKey = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Authentication failed due to empty API key for {Path}", context.Request.Path);
            await ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.").ExecuteAsync(context);
            return;
        }

        var agent = await agentService.GetByApiKeyAsync(apiKey, context.RequestAborted);
        if (agent is null)
        {
            _logger.LogWarning("Authentication failed: no agent matched API key for {Path}", context.Request.Path);
            await ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.").ExecuteAsync(context);
            return;
        }

        context.SetAuthenticatedAgent(agent);
        _logger.LogInformation("Authenticated agent {AgentId} for {Method} {Path}", agent.AgentId, context.Request.Method, context.Request.Path);
        await agentService.UpdateLastActiveAsync(agent, context.RequestAborted);

        await _next(context);
    }
}
