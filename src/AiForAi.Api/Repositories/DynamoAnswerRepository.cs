using AiForAi.Api.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Options;

namespace AiForAi.Api.Repositories;

public sealed class DynamoAnswerRepository : IAnswerRepository
{
    private readonly IDynamoDBContext _context;
    private readonly AppOptions _options;

    public DynamoAnswerRepository(IAmazonDynamoDB dynamoDb, IOptions<AppOptions> options)
    {
        _context = new DynamoDBContext(dynamoDb);
        _options = options.Value;
    }

    public Task CreateAsync(Answer answer, CancellationToken ct)
    {
        return _context.SaveAsync(answer, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AnswersTable
        }, ct);
    }

    public async Task<Answer?> GetByIdAsync(string answerId, CancellationToken ct)
    {
        var answer = await _context.LoadAsync<Answer>(answerId, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AnswersTable
        }, ct);

        return answer;
    }

    public async Task<List<Answer>> GetByQuestionIdAsync(string questionId, CancellationToken ct)
    {
        var scan = _context.ScanAsync<Answer>(
        [
            new ScanCondition("question_id", ScanOperator.Equal, questionId)
        ],
        new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AnswersTable
        });

        var results = new List<Answer>();
        do
        {
            results.AddRange(await scan.GetNextSetAsync(ct));
        }
        while (!scan.IsDone);

        return results;
    }

    public async Task<bool> HasVisibleAnswersForQuestionAsync(string questionId, CancellationToken ct)
    {
        var answers = await GetByQuestionIdAsync(questionId, ct);
        return answers.Any(a => !a.IsRemoved);
    }

    public Task UpdateAsync(Answer answer, CancellationToken ct)
    {
        return _context.SaveAsync(answer, new DynamoDBOperationConfig
        {
            OverrideTableName = _options.AnswersTable
        }, ct);
    }
}
