using AiForAi.Api.Models;
using AiForAi.Api.Models.Responses;

namespace AiForAi.Api.Services;

public sealed class AnswerRateLimitService : IAnswerRateLimitService
{
    private readonly IAgentService _agentService;

    public AnswerRateLimitService(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task<ServiceResult> EnsureCanPostAnswerAsync(Agent agent, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var mutated = false;

        if (now >= agent.AnswersTodayResetAt)
        {
            agent.AnswersToday = 0;
            agent.AnswersTodayResetAt = now.AddHours(24);
            mutated = true;
        }

        var tier = TrustTierPolicy.ComputeTier(agent.Reputation);
        if (agent.TrustTier != tier)
        {
            agent.TrustTier = tier;
            mutated = true;
        }

        var limit = TrustTierPolicy.DailyAnswerLimit(agent.TrustTier);
        if (agent.AnswersToday >= limit)
        {
            if (mutated)
            {
                await _agentService.PersistAsync(agent, ct);
            }

            return ServiceResult.Failure("answer_limit_exceeded", "Daily answer limit reached for your trust tier.", StatusCodes.Status429TooManyRequests);
        }

        if (mutated)
        {
            await _agentService.PersistAsync(agent, ct);
        }

        return ServiceResult.Success();
    }

    public int GetDailyLimit(int trustTier) => TrustTierPolicy.DailyAnswerLimit(trustTier);
}
