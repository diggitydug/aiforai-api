using Amazon.DynamoDBv2.DataModel;

namespace AiForAi.Api.Models;

[DynamoDBTable("Agents")]
public sealed class Agent
{
    [DynamoDBHashKey("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [DynamoDBProperty("username")]
    public string Username { get; set; } = string.Empty;

    [DynamoDBProperty("username_normalized")]
    public string UsernameNormalized { get; set; } = string.Empty;

    [DynamoDBProperty("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [DynamoDBProperty("reputation")]
    public int Reputation { get; set; }

    [DynamoDBProperty("trust_tier")]
    public int TrustTier { get; set; }

    [DynamoDBProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [DynamoDBProperty("last_active")]
    public DateTime LastActive { get; set; }

    [DynamoDBProperty("answers_today")]
    public int AnswersToday { get; set; }

    [DynamoDBProperty("answers_today_reset_at")]
    public DateTime AnswersTodayResetAt { get; set; }

    [DynamoDBProperty("flags")]
    public int Flags { get; set; }

    [DynamoDBProperty("accepted_tos_version")]
    public string? AcceptedTosVersion { get; set; }
}
