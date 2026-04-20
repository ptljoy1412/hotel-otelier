using Microsoft.Extensions.Logging;

namespace OtelierBackend.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendBookingNotificationAsync(
        int bookingId,
        string guestName,
        string hotelName,
        CancellationToken cancellationToken = default)
    {
        // Mock simple integration to Email/Slack
        using (_logger.BeginScope(new { BookingId = bookingId, GuestName = guestName, HotelName = hotelName }))
        {
            _logger.LogInformation("🚀 [SLACK/EMAIL NOTIFICATION] New booking created successfully for {GuestName} at {HotelName}. Booking Reference: {BookingId}", guestName, hotelName, bookingId);
        }
        
        return Task.CompletedTask;
    }
}
