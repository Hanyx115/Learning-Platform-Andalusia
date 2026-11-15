namespace AndalusiaAcademy.Domain.Models;

public static class CourseMaterialResourceTypes
{
    public const string Link = "Link";
    public const string File = "File";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        Link,
        File,
    ]);

    public static bool IsSupported(string? value)
        => value is not null && All.Contains(value, StringComparer.Ordinal);
}
