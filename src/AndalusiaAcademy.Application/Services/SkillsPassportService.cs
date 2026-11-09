using AndalusiaAcademy.Application.DTOs.Skills;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class SkillsPassportService(ISkillsPassportRepository repository)
{
    public Task<SkillsPassportDto> GetMineAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetForLearnerAsync(userId, cancellationToken);
}
