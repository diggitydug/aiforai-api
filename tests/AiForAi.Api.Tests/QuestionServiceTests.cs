using AiForAi.Api.Models;
using AiForAi.Api.Repositories;
using AiForAi.Api.Services;
using Xunit;

namespace AiForAi.Api.Tests;

public sealed class QuestionServiceTests
{
    [Fact]
    public async Task MarkQuestionDuplicate_SetsDuplicateLinkAndVisibility()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new NoopAnswerRepository();
        var agentRepo = new NoopAgentRepository();

        var duplicateQuestion = new Question { QuestionId = "q-1", VisibilityStatus = "pending" };
        var canonicalQuestion = new Question { QuestionId = "q-2", VisibilityStatus = "public" };
        await questionRepo.CreateAsync(duplicateQuestion, CancellationToken.None);
        await questionRepo.CreateAsync(canonicalQuestion, CancellationToken.None);

        var service = new QuestionService(questionRepo, answerRepo, agentRepo);

        var result = await service.MarkQuestionDuplicateAsync("q-1", "q-2", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await questionRepo.GetByIdAsync("q-1", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("duplicate", updated!.VisibilityStatus);
        Assert.Equal("q-2", updated.DuplicateOfQuestionId);
    }

    [Fact]
    public async Task MarkQuestionDuplicate_ReturnsNotFound_WhenCanonicalQuestionMissing()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new NoopAnswerRepository();
        var agentRepo = new NoopAgentRepository();

        await questionRepo.CreateAsync(new Question { QuestionId = "q-1" }, CancellationToken.None);

        var service = new QuestionService(questionRepo, answerRepo, agentRepo);
        var result = await service.MarkQuestionDuplicateAsync("q-1", "q-2", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("duplicate_target_not_found", result.Error?.ErrorCode);
    }

    private sealed class InMemoryQuestionRepository : IQuestionRepository
    {
        private readonly Dictionary<string, Question> _questions = new();

        public Task CreateAsync(Question question, CancellationToken ct)
        {
            _questions[question.QuestionId] = question;
            return Task.CompletedTask;
        }

        public Task<Question?> GetByIdAsync(string questionId, CancellationToken ct)
        {
            _questions.TryGetValue(questionId, out var question);
            return Task.FromResult(question);
        }

        public Task<List<Question>> GetAllAsync(CancellationToken ct)
        {
            return Task.FromResult(_questions.Values.ToList());
        }

        public Task<List<Question>> GetByCreatedByAgentIdAsync(string agentId, CancellationToken ct)
        {
            return Task.FromResult(_questions.Values.Where(q => q.CreatedByAgentId == agentId).ToList());
        }

        public Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct)
        {
            if (!_questions.TryGetValue(questionId, out var question))
            {
                return Task.FromResult(false);
            }

            if (!string.IsNullOrWhiteSpace(question.ClaimedBy))
            {
                return Task.FromResult(false);
            }

            question.ClaimedBy = agentId;
            return Task.FromResult(true);
        }

        public Task UpdateAsync(Question question, CancellationToken ct)
        {
            _questions[question.QuestionId] = question;
            return Task.CompletedTask;
        }
    }

    private sealed class NoopAnswerRepository : IAnswerRepository
    {
        public Task CreateAsync(Answer answer, CancellationToken ct) => Task.CompletedTask;
        public Task<Answer?> GetByIdAsync(string answerId, CancellationToken ct) => Task.FromResult<Answer?>(null);
        public Task<List<Answer>> GetByQuestionIdAsync(string questionId, CancellationToken ct) => Task.FromResult(new List<Answer>());
        public Task<bool> HasVisibleAnswersForQuestionAsync(string questionId, CancellationToken ct) => Task.FromResult(false);
        public Task UpdateAsync(Answer answer, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoopAgentRepository : IAgentRepository
    {
        public Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct) => Task.FromResult<Agent?>(null);
        public Task<Agent?> GetByUsernameAsync(string username, CancellationToken ct) => Task.FromResult<Agent?>(null);
        public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct) => Task.FromResult<Agent?>(null);
        public Task CreateAsync(Agent agent, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Agent agent, CancellationToken ct) => Task.CompletedTask;
    }
}