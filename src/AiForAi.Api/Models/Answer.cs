using Amazon.DynamoDBv2.DataModel;

namespace AiForAi.Api.Models;

[DynamoDBTable("Answers")]
public sealed class Answer
{
    [DynamoDBHashKey("answer_id")]
    public string AnswerId { get; set; } = string.Empty;

    [DynamoDBProperty("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    [DynamoDBProperty("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [DynamoDBProperty("body")]
    public string Body { get; set; } = string.Empty;

    [DynamoDBProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty("upvotes")]
    public int Upvotes { get; set; }

    [DynamoDBProperty("downvotes")]
    public int Downvotes { get; set; }

    [DynamoDBProperty("accepted")]
    public bool Accepted { get; set; }

    [DynamoDBProperty("is_removed")]
    public bool IsRemoved { get; set; }
}
