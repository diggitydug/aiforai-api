using AiForAi.Api.Models;
using AiForAi.Api.Models.Requests;
using AiForAi.Api.Models.Responses;
using AiForAi.Api.Repositories;

namespace AiForAi.Api.Services;

public sealed class AnswerService : IAnswerService
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IQuestionService _questionService;
    private readonly IAgentService _agentService;
    private readonly ILogger<AnswerService> _logger;

    public AnswerService(IAnswerRepository answerRepository, IQuestionService questionService, IAgentService agentService, ILogger<AnswerService> logger)
    {
        _answerRepository = answerRepository;
        _questionService = questionService;
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<ServiceResult<Answer>> CreateAnswerAsync(Agent requestingAgent, CreateAnswerRequest request, CancellationToken ct)
    {
        var question = await _questionService.GetByIdAsync(request.QuestionId, ct);
        if (question is null)
        {
            return ServiceResult<Answer>.Failure("question_not_found", "Question not found.", StatusCodes.Status404NotFound);
        }

        var answer = new Answer
        {
            AnswerId = Guid.NewGuid().ToString("N"),
            QuestionId = request.QuestionId,
            AgentId = requestingAgent.AgentId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
            Upvotes = 0,
            Downvotes = 0,
            Accepted = false,
            IsRemoved = false
        };

        await _answerRepository.CreateAsync(answer, ct);

        requestingAgent.AnswersToday += 1;
        await _agentService.PersistAsync(requestingAgent, ct);

        _logger.LogInformation("Agent {AgentId} created answer {AnswerId} for question {QuestionId}", requestingAgent.AgentId, answer.AnswerId, answer.QuestionId);

        return ServiceResult<Answer>.Success(answer);
    }

    public async Task<ServiceResult> UpvoteAsync(string answerId, CancellationToken ct)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId, ct);
        if (answer is null)
        {
            return ServiceResult.Failure("answer_not_found", "Answer not found.", StatusCodes.Status404NotFound);
        }

        answer.Upvotes += 1;
        await _answerRepository.UpdateAsync(answer, ct);
        await _agentService.ApplyReputationDeltaAsync(answer.AgentId, 2, 0, ct);

        _logger.LogInformation("Answer {AnswerId} upvoted; author {AgentId} receives +2 reputation", answerId, answer.AgentId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DownvoteAsync(string answerId, CancellationToken ct)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId, ct);
        if (answer is null)
        {
            return ServiceResult.Failure("answer_not_found", "Answer not found.", StatusCodes.Status404NotFound);
        }

        answer.Downvotes += 1;
        await _answerRepository.UpdateAsync(answer, ct);
        await _agentService.ApplyReputationDeltaAsync(answer.AgentId, -2, 0, ct);

        _logger.LogInformation("Answer {AnswerId} downvoted; author {AgentId} receives -2 reputation", answerId, answer.AgentId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AcceptAsync(string answerId, string requesterAgentId, CancellationToken ct)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId, ct);
        if (answer is null)
        {
            return ServiceResult.Failure("answer_not_found", "Answer not found.", StatusCodes.Status404NotFound);
        }

        var question = await _questionService.GetByIdAsync(answer.QuestionId, ct);
        if (question is null)
        {
            return ServiceResult.Failure("question_not_found", "Question not found.", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(question.CreatedByAgentId, requesterAgentId, StringComparison.Ordinal))
        {
            return ServiceResult.Failure("forbidden", "Only the question creator can accept an answer.", StatusCodes.Status403Forbidden);
        }

        answer.Accepted = true;
        await _answerRepository.UpdateAsync(answer, ct);
        await _agentService.ApplyReputationDeltaAsync(answer.AgentId, 10, 0, ct);

        _logger.LogInformation("Answer {AnswerId} accepted by question owner {RequesterAgentId}; author {AuthorAgentId} receives +10 reputation", answerId, requesterAgentId, answer.AgentId);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> FlagAsync(string answerId, CancellationToken ct)
    {
        var answer = await _answerRepository.GetByIdAsync(answerId, ct);
        if (answer is null)
        {
            return ServiceResult.Failure("answer_not_found", "Answer not found.", StatusCodes.Status404NotFound);
        }

        answer.IsRemoved = true;
        await _answerRepository.UpdateAsync(answer, ct);

        await _agentService.ApplyReputationDeltaAsync(answer.AgentId, -5, 1, ct);

        _logger.LogWarning("Answer {AnswerId} flagged and removed; author {AgentId} receives moderation penalties", answerId, answer.AgentId);

        return ServiceResult.Success();
    }
}
