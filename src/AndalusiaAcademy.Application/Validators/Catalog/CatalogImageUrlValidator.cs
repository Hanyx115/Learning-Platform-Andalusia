namespace AndalusiaAcademy.Application.Validators.Catalog;

internal static class CatalogImageUrlValidator
{
    public static bool IsValid(string? imageUrl)
        => string.IsNullOrWhiteSpace(imageUrl) ||
           (imageUrl.Length <= 500 && imageUrl.StartsWith("/api/v1/catalog-images/", StringComparison.Ordinal));
}
