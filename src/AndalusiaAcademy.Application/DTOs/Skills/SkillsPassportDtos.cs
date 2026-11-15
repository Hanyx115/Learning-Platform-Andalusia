namespace AndalusiaAcademy.Application.DTOs.Skills;

public sealed record SkillsPassportDto(
    int AssessedSkills,
    int DevelopingSkills,
    int DemonstratedSkills,
    IReadOnlyList<PassportSkillDto> Skills);

public sealed record PassportSkillDto(
    Guid SkillId,
    string Name,
    string Category,
    string Description,
    int TargetLevel,
    decimal? MasteryPercentage,
    string Status,
    int GradedEvidenceCount,
    IReadOnlyList<SkillEvidenceDto> Evidence);

public sealed record SkillEvidenceDto(
    string LearningTitle,
    string OfferingType,
    Guid OfferingId,
    int GradedAssignments,
    decimal AverageScorePercentage,
    DateTimeOffset? LastGradedAtUtc);
