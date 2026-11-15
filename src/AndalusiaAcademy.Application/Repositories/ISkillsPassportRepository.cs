using AndalusiaAcademy.Application.DTOs.Skills;

namespace AndalusiaAcademy.Application.Repositories;

public interface ISkillsPassportRepository
{
    Task<SkillsPassportDto> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken);
}
