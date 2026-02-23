using AiForAi.Api.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Options;

namespace AiForAi.Api.Repositories;

public sealed class DynamoAgentRepository : IAgentRepository
{
    private readonly IDynamoDBContext _context;
    private readonly AppOptions _options;

    public DynamoAgentRepository(IAmazonDynamoDB dynamoDb, IOptions<AppOptions> options)
    {
        _context = new DynamoDBContext(dynamoDb);
        _options = options.Value;
    }

    public async Task<Agent?> GetByApiKeyAsync(string apiKey, CancellationToken ct)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _options.AgentsTable };
        var scan = _context.ScanAsync<Agent>(
        [
            new ScanCondition("api_key", ScanOperator.Equal, apiKey)
        ],
        config);

        var results = await scan.GetNextSetAsync(ct);
        return results.FirstOrDefault();
    }

    public async Task<Agent?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var normalized = username.Trim().ToLowerInvariant();
        var config = new DynamoDBOperationConfig { OverrideTableName = _options.AgentsTable };

        var normalizedScan = _context.ScanAsync<Agent>(
        [
            new ScanCondition("username_normalized", ScanOperator.Equal, normalized)
        ],
        config);

        var normalizedResults = await normalizedScan.GetNextSetAsync(ct);
        if (normalizedResults.Count > 0)
        {
            return normalizedResults.First();
        }

        var scan = _context.ScanAsync<Agent>(
        [
            new ScanCondition("username", ScanOperator.Equal, username)
        ],
        config);

        var results = await scan.GetNextSetAsync(ct);
        return results.FirstOrDefault(agent => string.Equals(agent.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct)
    {
        var agent = await _context.LoadAsync<Agent>(agentId, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AgentsTable
        }, ct);

        return agent;
    }

    public Task CreateAsync(Agent agent, CancellationToken ct)
    {
        return _context.SaveAsync(agent, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AgentsTable
        }, ct);
    }

    public Task UpdateAsync(Agent agent, CancellationToken ct)
    {
        return _context.SaveAsync(agent, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AgentsTable
        }, ct);
    }
}
