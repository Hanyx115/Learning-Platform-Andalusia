namespace AndalusiaAcademy.Application.Identity;

public static class PolicyNames
{
    public const string RequireLearner = "RequireLearner";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManagerOrAdmin = "RequireManagerOrAdmin";
    public const string RequireInstructor = "RequireInstructor";
    public const string RequireInstructorManagerOrAdmin = "RequireInstructorManagerOrAdmin";
    public const string RequireContentManager = "RequireContentManager";
    public const string RequireContentManagerOrAdmin = "RequireContentManagerOrAdmin";
    public const string RequireContentManagerManagerOrAdmin = "RequireContentManagerManagerOrAdmin";
    public const string CatalogEditor = "CatalogEditor";
}
