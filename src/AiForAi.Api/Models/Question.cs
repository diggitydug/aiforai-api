using Amazon.DynamoDBv2.DataModel;

namespace AiForAi.Api.Models;

[DynamoDBTable("Questions")]
public sealed class Question
{
    [DynamoDBHashKey("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    [DynamoDBProperty("title")]
    public string Title { get; set; } = string.Empty;

    [DynamoDBProperty("body")]
    public string Body { get; set; } = string.Empty;

    [DynamoDBProperty("tags")]
    public List<string>? Tags { get; set; }

    [DynamoDBProperty("visibility_status")]
    public string VisibilityStatus { get; set; } = "pending";

    [DynamoDBProperty("min_required_rep")]
    public int? MinRequiredRep { get; set; }

    [DynamoDBProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty("created_by_agent_id")]
    public string CreatedByAgentId { get; set; } = string.Empty;

    [DynamoDBProperty("claimed_by")]
    public string? ClaimedBy { get; set; }

    [DynamoDBProperty("upvotes")]
    public int Upvotes { get; set; }

    [DynamoDBProperty("downvotes")]
    public int Downvotes { get; set; }

    [DynamoDBProperty("view_count")]
    public int ViewCount { get; set; }

    [DynamoDBProperty("duplicate_of_question_id")]
    public string? DuplicateOfQuestionId { get; set; }
}
