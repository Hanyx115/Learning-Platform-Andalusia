namespace AndalusiaAcademy.Domain.Models;

public static class NotificationTemplateCodes
{
    public const string AccountCreated = "AccountCreated";
    public const string ApplicationSubmitted = "ApplicationSubmitted";
    public const string ApplicationStatusUpdated = "ApplicationStatusUpdated";
    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string EnrollmentConfirmed = "EnrollmentConfirmed";
    public const string SessionReminder = "SessionReminder";
    public const string CourseAnnouncement = "CourseAnnouncement";
    public const string InstructorAssigned = "InstructorAssigned";
    public const string NewLearnerEnrollment = "NewLearnerEnrollment";
    public const string AssignmentSubmitted = "AssignmentSubmitted";
    public const string Operational = "Operational";
}

public static class NotificationChannels
{
    public const string InApp = "InApp";
}

public static class NotificationDeliveryStatuses
{
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";
}
