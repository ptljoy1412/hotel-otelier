namespace OtelierBackend.DTOs;

public class BookingResponseDto
{
    public int BookingId { get; set; }
    public int HotelId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
