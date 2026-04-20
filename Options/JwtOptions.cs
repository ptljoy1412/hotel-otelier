namespace OtelierBackend.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OtelierBackend";
    public string Audience { get; init; } = "OtelierBackendClients";
    public string Secret { get; init; } = "super-secret-development-key-change-me-12345";
    public int ExpiryMinutes { get; init; } = 60;
}
