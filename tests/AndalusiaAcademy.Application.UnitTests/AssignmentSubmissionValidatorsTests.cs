using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.Validators.Assignments;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class AssignmentSubmissionValidatorsTests
{
    [Theory]
    [InlineData("https://example.com/homework")]
    [InlineData("http://localhost:5173/document")]
    public void LinkValidator_AcceptsHttpAndHttpsLinks(string url)
    {
        var result = new SubmitAssignmentLinkRequestValidator()
            .Validate(new SubmitAssignmentLinkRequest(url));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("homework.example.com")]
    [InlineData("javascript:alert('unsafe')")]
    [InlineData("file:///C:/private/homework.pdf")]
    public void LinkValidator_RejectsMissingRelativeAndUnsafeLinks(string url)
    {
        var result = new SubmitAssignmentLinkRequestValidator()
            .Validate(new SubmitAssignmentLinkRequest(url));

        Assert.False(result.IsValid);
    }
}
