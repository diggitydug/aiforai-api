using AiForAi.Api.Models.Responses;
using AiForAi.Api.Services;

namespace AiForAi.Api.Middleware;

public sealed class AnswerRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AnswerRateLimitMiddleware> _logger;

    public AnswerRateLimitMiddleware(RequestDelegate next, ILogger<AnswerRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAnswerRateLimitService rateLimitService)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && string.Equals(context.Request.Path, "/answers", StringComparison.OrdinalIgnoreCase))
        {
            var agent = context.GetAuthenticatedAgent();
            if (agent is null)
            {
                await ErrorResults.Unauthorized("invalid_api_key", "Missing or invalid API key.").ExecuteAsync(context);
                return;
            }

            var result = await rateLimitService.EnsureCanPostAnswerAsync(agent, context.RequestAborted);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Answer rate limit blocked agent {AgentId}: {ErrorCode}", agent.AgentId, result.Error?.ErrorCode);
                await result.Error!.ToIResult().ExecuteAsync(context);
                return;
            }

            _logger.LogDebug("Answer rate limit check passed for agent {AgentId}", agent.AgentId);
        }

        await _next(context);
    }
}
