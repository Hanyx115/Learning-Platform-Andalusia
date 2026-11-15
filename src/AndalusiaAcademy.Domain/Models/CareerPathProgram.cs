namespace AndalusiaAcademy.Domain.Models;

public class CareerPathProgram
{
    public Guid CareerPathId { get; set; }

    public CareerPath? CareerPath { get; set; }

    public Guid ProgramId { get; set; }

    public AcademyProgram? Program { get; set; }
}
