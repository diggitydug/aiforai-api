using AiForAi.Api.Models;

namespace AiForAi.Api.Repositories;

public interface IQuestionRepository
{
    Task CreateAsync(Question question, CancellationToken ct);
    Task<Question?> GetByIdAsync(string questionId, CancellationToken ct);
    Task<List<Question>> GetAllAsync(CancellationToken ct);
    Task<List<Question>> GetByCreatedByAgentIdAsync(string agentId, CancellationToken ct);
    Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct);
    Task UpdateAsync(Question question, CancellationToken ct);
}
