namespace AndalusiaAcademy.API.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration, string environmentName)
    {
        if (!string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var errors = new List<string>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:DefaultConnection must be supplied by the deployment secret store.");
        }

        var jwtKey = configuration["Jwt:Key"];
        if (!TryGetSecureKey(jwtKey, out var keyLength) || keyLength < 32)
        {
            errors.Add("Jwt:Key must be a Base64-encoded secret containing at least 32 random bytes.");
        }

        var allowedHosts = configuration["AllowedHosts"]?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        if (allowedHosts.Length == 0 || allowedHosts.Contains("*", StringComparer.Ordinal))
        {
            errors.Add("AllowedHosts must explicitly list the production host names.");
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Cors:AllowedOrigins must contain one or more absolute HTTPS origins.");
        }

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            errors.Add("Database:MigrateOnStartup must remain false in Production; run the reviewed migration artifact before deployment.");
        }

        if (configuration.GetValue<bool>("CatalogSeed:Enabled") ||
            configuration.GetValue<bool>("IdentitySeed:Enabled"))
        {
            errors.Add("Development demo seeding must be disabled in Production.");
        }

        if (!configuration.GetValue("HttpsRedirection:Enabled", true))
        {
            errors.Add("HTTPS redirection must be enabled in Production.");
        }

        var assistantProvider = configuration["CourseAssistant:Provider"];
        if (string.Equals(assistantProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration["CourseAssistant:OpenAI:ApiKey"]))
            {
                errors.Add("CourseAssistant:OpenAI:ApiKey must be supplied by the deployment secret store when the OpenAI provider is enabled.");
            }

            var endpoint = configuration["CourseAssistant:OpenAI:Endpoint"];
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)
            {
                errors.Add("CourseAssistant:OpenAI:Endpoint must be an absolute HTTPS URL.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Production configuration is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static bool TryGetSecureKey(string? value, out int keyLength)
    {
        keyLength = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            keyLength = Convert.FromBase64String(value).Length;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
