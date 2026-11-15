namespace AndalusiaAcademy.Application.DTOs;

public sealed record CatalogImageDto(string ImageUrl);

public sealed record CatalogImageFile(Stream Content, string ContentType);

public sealed record CatalogImageUploadOutcome(CatalogImageDto? Value, string? Error)
{
    public bool Succeeded => Value is not null;

    public static CatalogImageUploadOutcome Success(CatalogImageDto value) => new(value, null);

    public static CatalogImageUploadOutcome Failure(string error) => new(null, error);
}
