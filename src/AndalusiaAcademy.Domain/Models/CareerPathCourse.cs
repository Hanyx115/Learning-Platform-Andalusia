namespace AndalusiaAcademy.Domain.Models;

public class CareerPathCourse
{
    public Guid CareerPathId { get; set; }

    public CareerPath? CareerPath { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }
}
