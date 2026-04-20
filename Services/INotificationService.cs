namespace OtelierBackend.Services;

public interface INotificationService
{
    Task SendBookingNotificationAsync(
        int bookingId,
        string guestName,
        string hotelName,
        CancellationToken cancellationToken = default);
}
