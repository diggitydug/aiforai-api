using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;
using AiForAi.Api.Repositories;

namespace AiForAi.Api.Services;

public sealed class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IAgentRepository _agentRepository;

    public QuestionService(IQuestionRepository questionRepository, IAnswerRepository answerRepository, IAgentRepository agentRepository)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _agentRepository = agentRepository;
    }

    public async Task<Question> CreateQuestionAsync(string agentId, CreateQuestionRequest request, CancellationToken ct)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Tags = request.Tags,
            VisibilityStatus = "pending",
            MinRequiredRep = request.MinRequiredRep,
            CreatedAt = DateTime.UtcNow,
            CreatedByAgentId = agentId,
            ClaimedBy = null
        };

        await _questionRepository.CreateAsync(question, ct);
        return question;
    }

    public async Task<List<Question>> GetUnansweredVisibleQuestionsAsync(Agent agent, CancellationToken ct)
    {
        var all = await _questionRepository.GetAllAsync(ct);

        var visible = all.Where(q =>
            string.Equals(q.VisibilityStatus, "public", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(q.VisibilityStatus, "pending", StringComparison.OrdinalIgnoreCase)
                && (!q.MinRequiredRep.HasValue || agent.Reputation >= q.MinRequiredRep.Value)));

        var unanswered = new List<Question>();
        foreach (var question in visible)
        {
            var hasAnswers = await _answerRepository.HasVisibleAnswersForQuestionAsync(question.QuestionId, ct);
            if (!hasAnswers)
            {
                unanswered.Add(question);
            }
        }

        return unanswered;
    }

    public Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct)
    {
        return _questionRepository.ClaimQuestionAsync(questionId, agentId, ct);
    }

    public async Task<List<Question>?> GetQuestionsByUsernameAsync(string username, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByUsernameAsync(username, ct);
        if (agent is null)
        {
            return null;
        }

        return await _questionRepository.GetByCreatedByAgentIdAsync(agent.AgentId, ct);
    }

    public async Task<QuestionDetailsResponse?> GetQuestionDetailsAsync(string questionId, CancellationToken ct)
    {
        var question = await _questionRepository.GetByIdAsync(questionId, ct);
        if (question is null)
        {
            return null;
        }

        var answers = await _answerRepository.GetByQuestionIdAsync(questionId, ct);
        return new QuestionDetailsResponse
        {
            Question = question,
            Answers = answers
        };
    }

    public Task<Question?> GetByIdAsync(string questionId, CancellationToken ct)
    {
        return _questionRepository.GetByIdAsync(questionId, ct);
    }
}
