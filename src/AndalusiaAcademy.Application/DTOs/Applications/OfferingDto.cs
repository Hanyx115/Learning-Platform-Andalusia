using AndalusiaAcademy.Application.DTOs.Scheduling;

namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record OfferingDto(
    Guid Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset ApplicationOpensAtUtc,
    DateTimeOffset ApplicationClosesAtUtc,
    decimal Price,
    string Currency,
    int Capacity,
    string Status,
    TrainingLocationDto? Location);
