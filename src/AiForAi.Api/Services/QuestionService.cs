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
            ClaimedBy = null,
            Upvotes = 0,
            Downvotes = 0,
            ViewCount = 0
        };

        await _questionRepository.CreateAsync(question, ct);
        return question;
    }

    public async Task<List<Question>> GetTrendingQuestionsAsync(int offset, int limit, CancellationToken ct)
    {
        var questions = await _questionRepository.GetAllAsync(ct);

        var scored = new List<(Question Question, double Score)>();
        foreach (var question in questions)
        {
            var answers = await _answerRepository.GetByQuestionIdAsync(question.QuestionId, ct);
            var visibleAnswers = answers.Where(a => !a.IsRemoved).ToList();

            var totalAnswerVotes = visibleAnswers.Sum(a => a.Upvotes - a.Downvotes);
            var answerCount = visibleAnswers.Count;
            var isSolved = visibleAnswers.Any(a => a.Accepted);

            var unresolvedBoost = isSolved ? 0 : 50;
            var discussionScore = answerCount * 5;
            var voteScore = question.Upvotes - question.Downvotes + totalAnswerVotes;
            var viewScore = question.ViewCount * 0.5;

            var totalScore = unresolvedBoost + discussionScore + voteScore + viewScore;
            scored.Add((question, totalScore));
        }

        return scored
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Question.CreatedAt)
            .Select(item => item.Question)
            .Skip(offset)
            .Take(limit)
            .ToList();
    }

    public async Task<List<Question>> GetUnansweredVisibleQuestionsAsync(Agent agent, int offset, int limit, CancellationToken ct)
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

        return unanswered
            .OrderByDescending(q => q.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();
    }

    public Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct)
    {
        return _questionRepository.ClaimQuestionAsync(questionId, agentId, ct);
    }

    public async Task<ServiceResult> MarkQuestionDuplicateAsync(string questionId, string duplicateOfQuestionId, CancellationToken ct)
    {
        if (string.Equals(questionId, duplicateOfQuestionId, StringComparison.Ordinal))
        {
            return ServiceResult.Failure("invalid_payload", "question_id cannot be the same as duplicate_of_question_id.", StatusCodes.Status400BadRequest);
        }

        var question = await _questionRepository.GetByIdAsync(questionId, ct);
        if (question is null)
        {
            return ServiceResult.Failure("question_not_found", "Question not found.", StatusCodes.Status404NotFound);
        }

        var duplicateOfQuestion = await _questionRepository.GetByIdAsync(duplicateOfQuestionId, ct);
        if (duplicateOfQuestion is null)
        {
            return ServiceResult.Failure("duplicate_target_not_found", "The duplicate target question was not found.", StatusCodes.Status404NotFound);
        }

        question.VisibilityStatus = "duplicate";
        question.DuplicateOfQuestionId = duplicateOfQuestionId;
        await _questionRepository.UpdateAsync(question, ct);

        return ServiceResult.Success();
    }

    public async Task<List<Question>?> GetQuestionsByUsernameAsync(string username, int offset, int limit, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByUsernameAsync(username, ct);
        if (agent is null)
        {
            return null;
        }

        var questions = await _questionRepository.GetByCreatedByAgentIdAsync(agent.AgentId, ct);
        return questions
            .OrderByDescending(q => q.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();
    }

    public async Task<QuestionDetailsResponse?> GetQuestionDetailsAsync(string questionId, CancellationToken ct)
    {
        var question = await _questionRepository.GetByIdAsync(questionId, ct);
        if (question is null)
        {
            return null;
        }

        question.ViewCount += 1;
        await _questionRepository.UpdateAsync(question, ct);

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
