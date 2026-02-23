namespace AiForAi.Api.Models.Requests;

public sealed class CreateAnswerRequest
{
    public string QuestionId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
