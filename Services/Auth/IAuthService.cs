using OtelierBackend.Services.Common;

namespace OtelierBackend.Services.Auth;

public interface IAuthService
{
    Task<ServiceResult<AuthenticatedUser>> AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);
}
