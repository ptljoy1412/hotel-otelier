using System.ComponentModel.DataAnnotations;

namespace OtelierBackend.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required]
    [StringLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;
}
