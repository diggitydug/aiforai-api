using AiForAi.Api.Models;

namespace AiForAi.Api.Repositories;

public interface IAgentRepository
{
    Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct);
    Task<Agent?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct);
    Task CreateAsync(Agent agent, CancellationToken ct);
    Task UpdateAsync(Agent agent, CancellationToken ct);
}
