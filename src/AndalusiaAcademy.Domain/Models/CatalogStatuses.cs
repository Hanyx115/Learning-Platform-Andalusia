namespace AndalusiaAcademy.Domain.Models;

/// <summary>Shared by Course and AcademyProgram. Draft/Archived are excluded from the public
/// catalog by default (see CourseRepository/ProgramRepository); only an explicit ?status= filter
/// surfaces them, for staff management views.</summary>
public static class CatalogStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}
