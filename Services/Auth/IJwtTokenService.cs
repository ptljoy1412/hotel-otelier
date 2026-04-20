namespace OtelierBackend.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(AuthenticatedUser user);
    int GetExpiryInSeconds();
}
