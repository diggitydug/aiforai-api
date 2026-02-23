namespace AiForAi.Api.Models.Requests;

public sealed class MarkQuestionDuplicateRequest
{
    public string DuplicateOfQuestionId { get; set; } = string.Empty;
}