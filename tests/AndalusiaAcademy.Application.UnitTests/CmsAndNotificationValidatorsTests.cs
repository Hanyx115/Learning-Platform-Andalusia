using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.DTOs.Notifications;
using AndalusiaAcademy.Application.Validators.Cms;
using AndalusiaAcademy.Application.Validators.Notifications;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CmsAndNotificationValidatorsTests
{
    [Fact]
    public void CmsValidator_AcceptsCompleteBilingualContent()
    {
        var request = new UpsertCmsContentItemRequest(
            CmsContentTypes.Faq,
            "payment-support",
            "Payment support",
            "دعم الدفع",
            "Contact support for payment help.",
            "تواصل مع الدعم للمساعدة في الدفع.",
            "https://example.com/image.png",
            "https://example.com/help",
            null,
            10,
            true);

        Assert.True(new UpsertCmsContentItemRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("Invalid Key")]
    [InlineData("UPPERCASE")]
    [InlineData("../unsafe")]
    public void CmsValidator_RejectsInvalidKeys(string key)
    {
        var request = new UpsertCmsContentItemRequest(
            CmsContentTypes.Faq, key, "Title", "عنوان", "Body", "محتوى", null, null, null, 0, false);

        Assert.False(new UpsertCmsContentItemRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("/profile/notifications", true)]
    [InlineData("https://example.com/news", true)]
    [InlineData("javascript:alert('unsafe')", false)]
    [InlineData("file:///C:/private/file.txt", false)]
    [InlineData("//malicious.example/path", false)]
    public void AnnouncementValidator_AcceptsInternalAndWebLinksOnly(string linkUrl, bool expectedValid)
    {
        var request = new CreateAnnouncementRequest(
            [Guid.NewGuid()], "Announcement", "إعلان", "Message", "رسالة", linkUrl);

        Assert.Equal(expectedValid, new CreateAnnouncementRequestValidator().Validate(request).IsValid);
    }
}
