using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;

namespace AiForAi.Api.Services;

public interface IQuestionService
{
    Task<Question> CreateQuestionAsync(string agentId, CreateQuestionRequest request, CancellationToken ct);
    Task<List<Question>> GetUnansweredVisibleQuestionsAsync(Agent agent, CancellationToken ct);
    Task<List<Question>?> GetQuestionsByUsernameAsync(string username, CancellationToken ct);
    Task<ServiceResult> MarkQuestionDuplicateAsync(string questionId, string duplicateOfQuestionId, CancellationToken ct);
    Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct);
    Task<QuestionDetailsResponse?> GetQuestionDetailsAsync(string questionId, CancellationToken ct);
    Task<Question?> GetByIdAsync(string questionId, CancellationToken ct);
}
