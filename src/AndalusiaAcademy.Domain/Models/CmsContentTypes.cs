namespace AndalusiaAcademy.Domain.Models;

public static class CmsContentTypes
{
    public const string HomepageSection = "HomepageSection";
    public const string PublicPage = "PublicPage";
    public const string Faq = "Faq";
    public const string Partner = "Partner";
    public const string Testimonial = "Testimonial";
    public const string InstructorProfile = "InstructorProfile";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        HomepageSection,
        PublicPage,
        Faq,
        Partner,
        Testimonial,
        InstructorProfile,
    };
}
