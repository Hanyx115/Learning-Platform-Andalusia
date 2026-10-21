using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Validators.Scheduling;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class SchedulingValidatorsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://maps.example.com/location/1")]
    public void CreateLocation_AllowsOptionalOrAbsoluteMapUrl(string? mapUrl)
    {
        var request = new CreateTrainingLocationRequest(
            "Alexandria Campus",
            "Training Room A",
            "Alexandria",
            "39 Khalil El-Khayat Basha, Sidi Gaber",
            mapUrl,
            30);

        var result = new CreateTrainingLocationRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateLocation_RejectsRelativeMapUrl()
    {
        var request = new CreateTrainingLocationRequest(
            "Alexandria Campus",
            "Training Room A",
            "Alexandria",
            "39 Khalil El-Khayat Basha, Sidi Gaber",
            "/locations/alexandria",
            30);

        var result = new CreateTrainingLocationRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.MapUrl));
    }

    [Fact]
    public void ScheduleSession_RequiresEndAfterStart()
    {
        var startsAtUtc = DateTimeOffset.UtcNow.AddDays(1);
        var request = new ScheduleCourseSessionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Session 1",
            startsAtUtc,
            startsAtUtc);

        var result = new ScheduleCourseSessionRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.EndsAtUtc));
    }

    [Fact]
    public void RescheduleSession_RejectsNoteLongerThanMaximum()
    {
        var startsAtUtc = DateTimeOffset.UtcNow.AddDays(1);
        var request = new RescheduleCourseSessionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Session 1",
            startsAtUtc,
            startsAtUtc.AddHours(1),
            new string('x', 501));

        var result = new RescheduleCourseSessionRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Note));
    }
}
