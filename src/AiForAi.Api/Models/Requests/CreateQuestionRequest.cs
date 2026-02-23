namespace AiForAi.Api.Models.Requests;

public sealed class CreateQuestionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public int? MinRequiredRep { get; set; }
}
