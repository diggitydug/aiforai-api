namespace AiForAi.Api.Models.Responses;

public sealed class RegisterAgentResponse
{
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Tos { get; set; } = string.Empty;
    public string TosVersion { get; set; } = string.Empty;
}
