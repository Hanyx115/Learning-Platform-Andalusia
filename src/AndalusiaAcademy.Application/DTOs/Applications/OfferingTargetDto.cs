namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record OfferingTargetDto(
    Guid Id,
    string TargetType,
    string Title,
    string Status,
    DateTimeOffset ApplicationOpensAtUtc,
    DateTimeOffset ApplicationClosesAtUtc);
