using Microsoft.EntityFrameworkCore;
using OtelierBackend.Data;
using OtelierBackend.Services.Common;

namespace OtelierBackend.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<AuthenticatedUser>> AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.VerifyPassword(user.PasswordHash, password))
        {
            return ServiceResult<AuthenticatedUser>.Failure(
                new ServiceError(ServiceErrorType.Unauthorized, "invalid_credentials", "Invalid username or password."));
        }

        return ServiceResult<AuthenticatedUser>.Success(new AuthenticatedUser
        {
            Id = user.Id,
            UserName = user.UserName,
            Role = user.Role
        });
    }
}
