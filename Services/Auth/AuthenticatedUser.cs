namespace OtelierBackend.Services.Auth;

public sealed class AuthenticatedUser
{
    public int Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
