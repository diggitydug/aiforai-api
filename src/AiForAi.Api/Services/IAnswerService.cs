using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;

namespace AiForAi.Api.Services;

public interface IAnswerService
{
    Task<ServiceResult<Answer>> CreateAnswerAsync(Agent requestingAgent, CreateAnswerRequest request, CancellationToken ct);
    Task<ServiceResult> UpvoteAsync(string answerId, CancellationToken ct);
    Task<ServiceResult> DownvoteAsync(string answerId, CancellationToken ct);
    Task<ServiceResult> AcceptAsync(string answerId, string requesterAgentId, CancellationToken ct);
    Task<ServiceResult> FlagAsync(string answerId, CancellationToken ct);
}

public interface ITosProvider
{
    string GetCurrentTosVersion();
    Task<string> GetCurrentTosTextAsync(CancellationToken ct);
}

public interface ITosPolicy
{
    bool IsAccepted(Agent agent, string currentVersion);
}

public interface IAnswerRateLimitService
{
    Task<ServiceResult> EnsureCanPostAnswerAsync(Agent agent, CancellationToken ct);
    int GetDailyLimit(int trustTier);
}
