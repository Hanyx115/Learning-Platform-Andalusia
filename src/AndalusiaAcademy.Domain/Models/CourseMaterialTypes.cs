namespace AndalusiaAcademy.Domain.Models;

public static class CourseMaterialTypes
{
    public const string Document = "Document";
    public const string Video = "Video";
    public const string Link = "Link";
    public const string Other = "Other";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        Document,
        Video,
        Link,
        Other,
    ]);

    public static bool IsSupported(string? value)
        => value is not null && All.Contains(value, StringComparer.Ordinal);
}
