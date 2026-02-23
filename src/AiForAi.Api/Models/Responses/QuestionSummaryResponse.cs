namespace AiForAi.Api.Models.Responses;

public sealed class QuestionSummaryResponse
{
    public string QuestionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string VisibilityStatus { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string? DuplicateOfQuestionId { get; set; }
}