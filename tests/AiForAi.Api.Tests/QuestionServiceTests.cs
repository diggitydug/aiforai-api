using AiForAi.Api.Models;
using AiForAi.Api.Repositories;
using AiForAi.Api.Services;
using Xunit;

namespace AiForAi.Api.Tests;

public sealed class QuestionServiceTests
{
    [Fact]
    public async Task GetQuestionDetails_IncrementsViewCount()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new InMemoryAnswerRepository();
        var agentRepo = new NoopAgentRepository();

        await questionRepo.CreateAsync(new Question { QuestionId = "q-views", ViewCount = 0 }, CancellationToken.None);

        var service = new QuestionService(questionRepo, answerRepo, agentRepo);
        var details = await service.GetQuestionDetailsAsync("q-views", CancellationToken.None);

        Assert.NotNull(details);

        var stored = await questionRepo.GetByIdAsync("q-views", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(1, stored!.ViewCount);
    }

    [Fact]
    public async Task GetTrendingQuestions_PrioritizesUnsolvedDiscussionAndViews()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new InMemoryAnswerRepository();
        var agentRepo = new NoopAgentRepository();

        var solved = new Question { QuestionId = "q-solved", ViewCount = 50, Upvotes = 1, Downvotes = 0, CreatedAt = DateTime.UtcNow.AddHours(-3) };
        var unsolved = new Question { QuestionId = "q-unsolved", ViewCount = 20, Upvotes = 2, Downvotes = 0, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        await questionRepo.CreateAsync(solved, CancellationToken.None);
        await questionRepo.CreateAsync(unsolved, CancellationToken.None);

        answerRepo.AddAnswer(new Answer
        {
            AnswerId = "a-1",
            QuestionId = "q-solved",
            Upvotes = 15,
            Downvotes = 1,
            Accepted = true,
            IsRemoved = false
        });

        answerRepo.AddAnswer(new Answer
        {
            AnswerId = "a-2",
            QuestionId = "q-unsolved",
            Upvotes = 10,
            Downvotes = 1,
            Accepted = false,
            IsRemoved = false
        });

        answerRepo.AddAnswer(new Answer
        {
            AnswerId = "a-3",
            QuestionId = "q-unsolved",
            Upvotes = 8,
            Downvotes = 0,
            Accepted = false,
            IsRemoved = false
        });

        var service = new QuestionService(questionRepo, answerRepo, agentRepo);
        var trending = await service.GetTrendingQuestionsAsync(0, 25, CancellationToken.None);

        Assert.NotEmpty(trending);
        Assert.Equal("q-unsolved", trending[0].QuestionId);
    }

    [Fact]
    public async Task GetTrendingQuestions_RespectsOffsetAndLimit()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new InMemoryAnswerRepository();
        var agentRepo = new NoopAgentRepository();

        await questionRepo.CreateAsync(new Question { QuestionId = "q-1", ViewCount = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-3) }, CancellationToken.None);
        await questionRepo.CreateAsync(new Question { QuestionId = "q-2", ViewCount = 90, CreatedAt = DateTime.UtcNow.AddMinutes(-2) }, CancellationToken.None);
        await questionRepo.CreateAsync(new Question { QuestionId = "q-3", ViewCount = 80, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }, CancellationToken.None);

        var service = new QuestionService(questionRepo, answerRepo, agentRepo);
        var page = await service.GetTrendingQuestionsAsync(1, 1, CancellationToken.None);

        Assert.Single(page);
        Assert.Equal("q-2", page[0].QuestionId);
    }

    [Fact]
    public async Task MarkQuestionDuplicate_SetsDuplicateLinkAndVisibility()
    {
        var questionRepo = new InMemoryQuestionRepository();
        var answerRepo = new InMemoryAnswerRepository();
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
        var answerRepo = new InMemoryAnswerRepository();
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

    private sealed class InMemoryAnswerRepository : IAnswerRepository
    {
        private readonly Dictionary<string, Answer> _answers = new();

        public void AddAnswer(Answer answer)
        {
            _answers[answer.AnswerId] = answer;
        }

        public Task CreateAsync(Answer answer, CancellationToken ct)
        {
            _answers[answer.AnswerId] = answer;
            return Task.CompletedTask;
        }

        public Task<Answer?> GetByIdAsync(string answerId, CancellationToken ct)
        {
            _answers.TryGetValue(answerId, out var answer);
            return Task.FromResult(answer);
        }

        public Task<List<Answer>> GetByQuestionIdAsync(string questionId, CancellationToken ct)
        {
            return Task.FromResult(_answers.Values.Where(a => a.QuestionId == questionId).ToList());
        }

        public Task<bool> HasVisibleAnswersForQuestionAsync(string questionId, CancellationToken ct)
        {
            return Task.FromResult(_answers.Values.Any(a => a.QuestionId == questionId && !a.IsRemoved));
        }

        public Task UpdateAsync(Answer answer, CancellationToken ct)
        {
            _answers[answer.AnswerId] = answer;
            return Task.CompletedTask;
        }
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