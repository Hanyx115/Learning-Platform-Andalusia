using System.ComponentModel.DataAnnotations;
using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CatalogListQueryTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var query = new CatalogListQuery();

        var validationResults = Validate(query);

        Assert.Empty(validationResults);
        Assert.Equal(1, query.Page);
        Assert.Equal(10, query.PageSize);
        Assert.Equal(CatalogListQuery.TitleSort, query.Sort);
    }

    [Theory]
    [InlineData(CatalogListQuery.TitleSort)]
    [InlineData(CatalogListQuery.TitleDescendingSort)]
    public void SupportedSorts_AreValid(string sort)
    {
        Assert.Empty(Validate(new CatalogListQuery { Sort = sort }));
    }

    [Fact]
    public void UnsupportedSort_IsRejected()
    {
        Assert.NotEmpty(Validate(new CatalogListQuery { Sort = "newest" }));
    }

    private static IReadOnlyList<ValidationResult> Validate(CatalogListQuery query)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), results, true);
        return results;
    }
}
