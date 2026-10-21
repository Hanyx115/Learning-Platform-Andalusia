namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public sealed record ManageOfferingRequest(
    Guid? LocationId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset ApplicationOpensAtUtc,
    DateTimeOffset ApplicationClosesAtUtc,
    decimal Price,
    string Currency,
    int Capacity,
    string Status);
