using AiForAi.Api.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace AiForAi.Api.Repositories;

public sealed class DynamoQuestionRepository : IQuestionRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AppOptions _options;

    public DynamoQuestionRepository(IAmazonDynamoDB dynamoDb, IOptions<AppOptions> options)
    {
        _context = new DynamoDBContext(dynamoDb);
        _dynamoDb = dynamoDb;
        _options = options.Value;
    }

    public Task CreateAsync(Question question, CancellationToken ct)
    {
        return _context.SaveAsync(question, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.QuestionsTable
        }, ct);
    }

    public async Task<Question?> GetByIdAsync(string questionId, CancellationToken ct)
    {
        var question = await _context.LoadAsync<Question>(questionId, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.QuestionsTable
        }, ct);

        return question;
    }

    public async Task<List<Question>> GetAllAsync(CancellationToken ct)
    {
        var scan = _context.ScanAsync<Question>([], new DynamoDBOperationConfig
        {
            OverrideTableName = _options.QuestionsTable
        });

        var all = new List<Question>();
        do
        {
            all.AddRange(await scan.GetNextSetAsync(ct));
        }
        while (!scan.IsDone);

        return all;
    }

    public async Task<List<Question>> GetByCreatedByAgentIdAsync(string agentId, CancellationToken ct)
    {
        var scan = _context.ScanAsync<Question>(
        [
            new ScanCondition("created_by_agent_id", ScanOperator.Equal, agentId)
        ],
        new DynamoDBOperationConfig
        {
            OverrideTableName = _options.QuestionsTable
        });

        var questions = new List<Question>();
        do
        {
            questions.AddRange(await scan.GetNextSetAsync(ct));
        }
        while (!scan.IsDone);

        return questions;
    }

    public async Task<bool> ClaimQuestionAsync(string questionId, string agentId, CancellationToken ct)
    {
        try
        {
            await _dynamoDb.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _options.QuestionsTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["question_id"] = new AttributeValue { S = questionId }
                },
                UpdateExpression = "SET claimed_by = :agentId",
                ConditionExpression = "attribute_not_exists(claimed_by)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":agentId"] = new AttributeValue { S = agentId }
                }
            }, ct);

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public Task UpdateAsync(Question question, CancellationToken ct)
    {
        return _context.SaveAsync(question, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.QuestionsTable
        }, ct);
    }
}
