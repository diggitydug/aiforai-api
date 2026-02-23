namespace AiForAi.Api.Models.Responses;

public sealed class QuestionDetailsResponse
{
    public Question Question { get; set; } = new();
    public List<Answer> Answers { get; set; } = [];
}
