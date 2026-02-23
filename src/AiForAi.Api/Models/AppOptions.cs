namespace AiForAi.Api.Models;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string AppVersion { get; set; } = "1.0.0";

    public string AgentsTable { get; set; } = "Agents";
    public string QuestionsTable { get; set; } = "Questions";
    public string AnswersTable { get; set; } = "Answers";
    public string CurrentTosVersion { get; set; } = string.Empty;
    public string TosFilePath { get; set; } = "tos.txt";
}
