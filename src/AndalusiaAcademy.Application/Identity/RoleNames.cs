namespace AndalusiaAcademy.Application.Identity;

public static class RoleNames
{
    public const string Learner = "Learner";
    public const string Instructor = "Instructor";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
    public const string ContentManager = "ContentManager";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        Learner,
        Instructor,
        Manager,
        Admin,
        ContentManager,
    ]);
}
