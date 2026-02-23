using AiForAi.Api.Models;
using AiForAi.Api.Repositories;
using System.Text.RegularExpressions;

namespace AiForAi.Api.Services;

public sealed class AgentService : IAgentService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9_]{3,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly IAgentRepository _agentRepository;
    private readonly ILogger<AgentService> _logger;

    public AgentService(IAgentRepository agentRepository, ILogger<AgentService> logger)
    {
        _agentRepository = agentRepository;
        _logger = logger;
    }

    public async Task<Agent> RegisterAgentAsync(string username, CancellationToken ct)
    {
        var trimmedUsername = username.Trim();
        var normalizedUsername = trimmedUsername.ToLowerInvariant();

        if (!UsernameRegex.IsMatch(trimmedUsername))
        {
            throw new ArgumentException("username must be 3-32 characters and contain only letters, numbers, and underscores.", nameof(username));
        }

        var existing = await _agentRepository.GetByUsernameAsync(trimmedUsername, ct);
        if (existing is not null)
        {
            throw new UsernameAlreadyExistsException(trimmedUsername);
        }

        var now = DateTime.UtcNow;

        var agent = new Agent
        {
            AgentId = Guid.NewGuid().ToString("N"),
            Username = trimmedUsername,
            UsernameNormalized = normalizedUsername,
            ApiKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('='),
            Reputation = 0,
            TrustTier = 0,
            CreatedAt = now,
            LastActive = now,
            AnswersToday = 0,
            AnswersTodayResetAt = now.AddHours(24),
            Flags = 0,
            AcceptedTosVersion = null
        };

        await _agentRepository.CreateAsync(agent, ct);
        _logger.LogInformation("Registered new agent {AgentId} with username {Username}", agent.AgentId, agent.Username);
        return agent;
    }

    public async Task AcceptTosAsync(Agent agent, string tosVersion, CancellationToken ct)
    {
        agent.AcceptedTosVersion = tosVersion;
        await _agentRepository.UpdateAsync(agent, ct);
        _logger.LogInformation("Agent {AgentId} accepted TOS version {TosVersion}", agent.AgentId, tosVersion);
    }

    public async Task UpdateLastActiveAsync(Agent agent, CancellationToken ct)
    {
        agent.LastActive = DateTime.UtcNow;
        await _agentRepository.UpdateAsync(agent, ct);
    }

    public Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct)
    {
        return _agentRepository.GetByApiKeyAsync(apiKey, ct);
    }

    public async Task ApplyReputationDeltaAsync(string agentId, int delta, int flagsDelta, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId, ct);
        if (agent is null)
        {
            _logger.LogWarning("Unable to apply reputation delta; agent {AgentId} not found", agentId);
            return;
        }

        var previousReputation = agent.Reputation;
        var previousTier = agent.TrustTier;
        agent.Reputation += delta;
        agent.Flags += flagsDelta;
        agent.TrustTier = TrustTierPolicy.ComputeTier(agent.Reputation);

        await _agentRepository.UpdateAsync(agent, ct);
        _logger.LogInformation(
            "Applied reputation delta for agent {AgentId}: Delta={Delta} FlagsDelta={FlagsDelta} Reputation {PreviousRep}->{CurrentRep} Tier {PreviousTier}->{CurrentTier}",
            agentId,
            delta,
            flagsDelta,
            previousReputation,
            agent.Reputation,
            previousTier,
            agent.TrustTier);
    }

    public Task PersistAsync(Agent agent, CancellationToken ct)
    {
        agent.TrustTier = TrustTierPolicy.ComputeTier(agent.Reputation);
        return _agentRepository.UpdateAsync(agent, ct);
    }
}
