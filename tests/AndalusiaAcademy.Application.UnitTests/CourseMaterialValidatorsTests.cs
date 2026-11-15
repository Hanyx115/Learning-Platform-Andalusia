using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Validators.Materials;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CourseMaterialValidatorsTests
{
    [Fact]
    public void CreateMaterial_AllowsValidPublishedResource()
    {
        var request = CreateRequest("https://learning.example.com/networking.pdf");

        var result = new CreateCourseMaterialRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateMaterial_RejectsRelativeResourceUrl()
    {
        var request = CreateRequest("/materials/networking.pdf");

        var result = new CreateCourseMaterialRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ResourceUrl));
    }

    [Fact]
    public void CreateMaterial_RejectsUnsupportedTypeAndVisibility()
    {
        var request = CreateRequest(
            "https://learning.example.com/networking.pdf",
            materialType: "Quiz",
            visibility: "Public");

        var result = new CreateCourseMaterialRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.MaterialType));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Visibility));
    }

    private static CreateCourseMaterialRequest CreateRequest(
        string resourceUrl,
        string materialType = CourseMaterialTypes.Document,
        string visibility = CourseMaterialVisibilities.Published)
        => new(
            CourseSessionId: null,
            Title: "Networking workbook",
            Description: "Reference material for the course.",
            ResourceUrl: resourceUrl,
            MaterialType: materialType,
            SortOrder: 0,
            Visibility: visibility);
}
