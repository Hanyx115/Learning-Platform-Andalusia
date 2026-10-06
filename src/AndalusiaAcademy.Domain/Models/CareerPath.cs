namespace AndalusiaAcademy.Domain.Models;

public class CareerPath
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RecommendedSkills { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
