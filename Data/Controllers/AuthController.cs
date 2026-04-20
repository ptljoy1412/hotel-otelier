using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtelierBackend.DTOs.Auth;
using OtelierBackend.DTOs.Common;
using OtelierBackend.Services.Auth;

namespace OtelierBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Code = result.Error!.Code,
                Message = result.Error.Message
            });
        }

        var authenticatedUser = result.Value!;
        return Ok(new AuthResponseDto
        {
            AccessToken = _jwtTokenService.GenerateToken(authenticatedUser),
            ExpiresIn = _jwtTokenService.GetExpiryInSeconds(),
            Role = authenticatedUser.Role
        });
    }

    // Temp debug endpoint - shows all claims in the token
    [HttpGet("claims")]
    [Authorize]
    public IActionResult GetClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }
}
