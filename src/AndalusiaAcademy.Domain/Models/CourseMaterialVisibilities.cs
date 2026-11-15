namespace AndalusiaAcademy.Domain.Models;

public static class CourseMaterialVisibilities
{
    public const string Published = "Published";
    public const string Hidden = "Hidden";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        Published,
        Hidden,
    ]);

    public static bool IsSupported(string? value)
        => value is not null && All.Contains(value, StringComparer.Ordinal);
}
