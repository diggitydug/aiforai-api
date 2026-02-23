using AiForAi.Api.Models;

namespace AiForAi.Api.Services;

public sealed class UsernameAlreadyExistsException : Exception
{
    public UsernameAlreadyExistsException(string username)
        : base($"Username '{username}' is already taken.")
    {
    }
}

public interface IAgentService
{
    Task<Agent> RegisterAgentAsync(string username, CancellationToken ct);
    Task AcceptTosAsync(Agent agent, string tosVersion, CancellationToken ct);
    Task UpdateLastActiveAsync(Agent agent, CancellationToken ct);
    Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct);
    Task ApplyReputationDeltaAsync(string agentId, int delta, int flagsDelta, CancellationToken ct);
    Task PersistAsync(Agent agent, CancellationToken ct);
}
