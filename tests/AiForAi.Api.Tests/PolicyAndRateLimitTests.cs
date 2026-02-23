using AiForAi.Api.Models;
using AiForAi.Api.Repositories;
using AiForAi.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiForAi.Api.Tests;

public sealed class PolicyAndRateLimitTests
{
    [Fact]
    public void TosPolicy_RequiresExactVersionMatch()
    {
        var policy = new TosPolicy();
        var agent = new Agent { AcceptedTosVersion = "2026-02-22" };

        Assert.True(policy.IsAccepted(agent, "2026-02-22"));
        Assert.False(policy.IsAccepted(agent, "2026-03-01"));
    }

    [Fact]
    public async Task AnswerRateLimit_ResetsDailyCounter_WhenWindowExpired()
    {
        var repo = new InMemoryAgentRepository();
        var agent = new Agent
        {
            AgentId = "agent-1",
            ApiKey = "key-1",
            Reputation = 0,
            TrustTier = 0,
            AnswersToday = 99,
            AnswersTodayResetAt = DateTime.UtcNow.AddMinutes(-1)
        };

        await repo.CreateAsync(agent, CancellationToken.None);
        var agentService = new AgentService(repo, NullLogger<AgentService>.Instance);
        var rateLimit = new AnswerRateLimitService(agentService);

        var result = await rateLimit.EnsureCanPostAnswerAsync(agent, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, agent.AnswersToday);
        Assert.True(agent.AnswersTodayResetAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task AnswerRateLimit_BlocksWhenTierCapReached()
    {
        var repo = new InMemoryAgentRepository();
        var agent = new Agent
        {
            AgentId = "agent-2",
            ApiKey = "key-2",
            Reputation = 0,
            TrustTier = 0,
            AnswersToday = 5,
            AnswersTodayResetAt = DateTime.UtcNow.AddHours(1)
        };

        await repo.CreateAsync(agent, CancellationToken.None);
        var agentService = new AgentService(repo, NullLogger<AgentService>.Instance);
        var rateLimit = new AnswerRateLimitService(agentService);

        var result = await rateLimit.EnsureCanPostAnswerAsync(agent, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("answer_limit_exceeded", result.Error?.ErrorCode);
    }

    [Fact]
    public async Task RegisterAgent_StoresUsernameAndNormalizedValue()
    {
        var repo = new InMemoryAgentRepository();
        var agentService = new AgentService(repo, NullLogger<AgentService>.Instance);

        var agent = await agentService.RegisterAgentAsync("Alice_123", CancellationToken.None);

        Assert.Equal("Alice_123", agent.Username);
        Assert.Equal("alice_123", agent.UsernameNormalized);
        Assert.False(string.IsNullOrWhiteSpace(agent.ApiKey));
    }

    [Fact]
    public async Task RegisterAgent_RejectsDuplicateUsername_IgnoringCase()
    {
        var repo = new InMemoryAgentRepository();
        var agentService = new AgentService(repo, NullLogger<AgentService>.Instance);

        await agentService.RegisterAgentAsync("Alice_123", CancellationToken.None);

        await Assert.ThrowsAsync<UsernameAlreadyExistsException>(
            () => agentService.RegisterAgentAsync("alice_123", CancellationToken.None));
    }

    private sealed class InMemoryAgentRepository : IAgentRepository
    {
        private readonly Dictionary<string, Agent> _agents = new();

        public Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct)
        {
            return Task.FromResult(_agents.Values.FirstOrDefault(a => a.ApiKey == apiKey));
        }

        public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct)
        {
            _agents.TryGetValue(agentId, out var agent);
            return Task.FromResult(agent);
        }

        public Task<Agent?> GetByUsernameAsync(string username, CancellationToken ct)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return Task.FromResult(_agents.Values.FirstOrDefault(a => a.UsernameNormalized == normalized));
        }

        public Task CreateAsync(Agent agent, CancellationToken ct)
        {
            _agents[agent.AgentId] = agent;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Agent agent, CancellationToken ct)
        {
            _agents[agent.AgentId] = agent;
            return Task.CompletedTask;
        }
    }
}
