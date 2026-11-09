namespace AndalusiaAcademy.Domain.Models;

public sealed class LearningSkillMapping
{
    public Guid Id { get; set; }

    public Guid SkillId { get; set; }

    public Skill? Skill { get; set; }

    public Guid? CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid? ProgramId { get; set; }

    public AcademyProgram? Program { get; set; }

    public int TargetLevel { get; set; } = 1;
}
