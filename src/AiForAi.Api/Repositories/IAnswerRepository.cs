using AiForAi.Api.Models;

namespace AiForAi.Api.Repositories;

public interface IAnswerRepository
{
    Task CreateAsync(Answer answer, CancellationToken ct);
    Task<Answer?> GetByIdAsync(string answerId, CancellationToken ct);
    Task<List<Answer>> GetByQuestionIdAsync(string questionId, CancellationToken ct);
    Task<bool> HasVisibleAnswersForQuestionAsync(string questionId, CancellationToken ct);
    Task UpdateAsync(Answer answer, CancellationToken ct);
}
