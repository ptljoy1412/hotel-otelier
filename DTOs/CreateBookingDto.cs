using System.ComponentModel.DataAnnotations;

namespace OtelierBackend.DTOs;

public class CreateBookingDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string GuestName { get; set; } = string.Empty;

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    // Custom validation to ensure check-out is after check-in
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOutDate <= CheckInDate)
        {
            yield return new ValidationResult(
                "Check-out date must be after check-in date.",
                new[] { nameof(CheckOutDate) });
        }

        if (CheckInDate < DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "Check-in date cannot be in the past.",
                new[] { nameof(CheckInDate) });
        }
    }
}
