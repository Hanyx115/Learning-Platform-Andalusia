namespace AndalusiaAcademy.Infrastructure.Lms.Assistant;

public sealed class CourseAssistantOptions
{
    public const string SectionName = "CourseAssistant";

    public string Provider { get; set; } = "Demo";
    public OpenAiOptions OpenAI { get; set; } = new();
}

public sealed class OpenAiOptions
{
    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string Model { get; set; } = "gpt-4.1-mini";
    public string? ApiKey { get; set; }
}
