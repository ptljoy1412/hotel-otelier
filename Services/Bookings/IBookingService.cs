using OtelierBackend.DTOs;
using OtelierBackend.Services.Common;

namespace OtelierBackend.Services.Bookings;

public interface IBookingService
{
    Task<ServiceResult<IReadOnlyCollection<BookingResponseDto>>> GetBookingsAsync(
        int hotelId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BookingResponseDto>> CreateBookingAsync(
        int hotelId,
        CreateBookingDto request,
        string createdBy,
        CancellationToken cancellationToken = default);
}
