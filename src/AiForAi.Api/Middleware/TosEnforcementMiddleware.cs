using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;

namespace AiForAi.Api.Middleware;

public sealed class TosEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TosEnforcementMiddleware> _logger;

    public TosEnforcementMiddleware(RequestDelegate next, ILogger<TosEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITosProvider tosProvider, ITosPolicy tosPolicy)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method)
            && !HttpMethods.IsDelete(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (string.Equals(context.Request.Path, "/agents/register", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Path, "/agents/accept-tos", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var agent = context.GetAuthenticatedAgent();
        if (agent is null)
        {
            _logger.LogWarning("TOS check failed due to missing authenticated agent for {Method} {Path}", context.Request.Method, context.Request.Path);
            await ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.").ExecuteAsync(context);
            return;
        }

        if (!tosPolicy.IsAccepted(agent, tosProvider.GetCurrentTosVersion()))
        {
            _logger.LogWarning("TOS check failed for agent {AgentId} on {Method} {Path}. AcceptedVersion={AcceptedVersion} CurrentVersion={CurrentVersion}",
                agent.AgentId,
                context.Request.Method,
                context.Request.Path,
                agent.AcceptedTosVersion,
                tosProvider.GetCurrentTosVersion());
            await ErrorResults.Forbidden("tos_not_accepted", "You must accept the latest Terms of Service.").ExecuteAsync(context);
            return;
        }

        _logger.LogDebug("TOS check passed for agent {AgentId} on {Method} {Path}", agent.AgentId, context.Request.Method, context.Request.Path);

        await _next(context);
    }
}
