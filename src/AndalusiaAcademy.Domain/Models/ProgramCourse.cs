namespace AndalusiaAcademy.Domain.Models;

public class ProgramCourse
{
    public Guid ProgramId { get; set; }

    public AcademyProgram? Program { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public int SequenceOrder { get; set; }
}
