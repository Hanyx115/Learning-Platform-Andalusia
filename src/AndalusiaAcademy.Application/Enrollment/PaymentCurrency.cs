namespace AndalusiaAcademy.Application.Enrollment;

public static class PaymentCurrency
{
    /// <summary>
    /// The catalog has no per-course currency today and Paymob (the chosen gateway) primarily
    /// serves Egypt, so this is the single supported currency until that changes.
    /// </summary>
    public const string Default = "EGP";
}
