using System.ComponentModel.DataAnnotations;
using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CourseCatalogQueryTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var query = new CourseCatalogQuery();

        var validationResults = Validate(query);

        Assert.Empty(validationResults);
        Assert.Equal(1, query.Page);
        Assert.Equal(10, query.PageSize);
        Assert.Equal(CourseCatalogQuery.TitleSort, query.Sort);
    }

    [Theory]
    [InlineData(0, 10, "title")]
    [InlineData(1, 0, "title")]
    [InlineData(1, 101, "title")]
    [InlineData(1, 10, "unsupported")]
    public void InvalidBoundsOrSort_AreRejected(int page, int pageSize, string sort)
    {
        var query = new CourseCatalogQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
        };

        var validationResults = Validate(query);

        Assert.NotEmpty(validationResults);
    }

    [Theory]
    [InlineData(CourseCatalogQuery.TitleSort)]
    [InlineData(CourseCatalogQuery.TitleDescendingSort)]
    [InlineData(CourseCatalogQuery.NewestSort)]
    [InlineData(CourseCatalogQuery.OldestSort)]
    public void SupportedSorts_AreValid(string sort)
    {
        var query = new CourseCatalogQuery { Sort = sort };

        var validationResults = Validate(query);

        Assert.Empty(validationResults);
    }

    private static IReadOnlyList<ValidationResult> Validate(CourseCatalogQuery query)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), results, true);
        return results;
    }
}
