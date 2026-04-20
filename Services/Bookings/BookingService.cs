using Microsoft.EntityFrameworkCore;
using OtelierBackend.Data;
using OtelierBackend.DTOs;
using OtelierBackend.Models;
using OtelierBackend.Services.Common;

namespace OtelierBackend.Services.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly AppDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public BookingService(AppDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<IReadOnlyCollection<BookingResponseDto>>> GetBookingsAsync(
        int hotelId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var hotelExists = await _dbContext.Hotels.AnyAsync(h => h.Id == hotelId, cancellationToken);
        if (!hotelExists)
        {
            return ServiceResult<IReadOnlyCollection<BookingResponseDto>>.Failure(
                new ServiceError(ServiceErrorType.NotFound, "hotel_not_found", "Hotel not found."));
        }

        var query = _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == hotelId);

        if (startDate.HasValue)
        {
            query = query.Where(b => b.CheckInDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(b => b.CheckOutDate <= endDate.Value);
        }

        var bookings = await query
            .OrderBy(b => b.CheckInDate)
            .Select(b => new BookingResponseDto
            {
                BookingId = b.BookingId,
                HotelId = b.HotelId,
                GuestName = b.GuestName,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                CreatedBy = b.CreatedBy
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<BookingResponseDto>>.Success(bookings);
    }

    public async Task<ServiceResult<BookingResponseDto>> CreateBookingAsync(
        int hotelId,
        CreateBookingDto request,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (request.CheckOutDate <= request.CheckInDate)
        {
            return ServiceResult<BookingResponseDto>.Failure(
                new ServiceError(ServiceErrorType.Validation, "invalid_date_range", "Check-out date must be after check-in date."));
        }

        var hotel = await _dbContext.Hotels
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.Id == hotelId, cancellationToken);

        if (hotel is null)
        {
            return ServiceResult<BookingResponseDto>.Failure(
                new ServiceError(ServiceErrorType.NotFound, "hotel_not_found", "Hotel not found."));
        }

        var hasConflict = await _dbContext.Bookings.AnyAsync(
            b => b.HotelId == hotelId
                && request.CheckInDate < b.CheckOutDate
                && request.CheckOutDate > b.CheckInDate,
            cancellationToken);

        if (hasConflict)
        {
            return ServiceResult<BookingResponseDto>.Failure(
                new ServiceError(ServiceErrorType.Conflict, "booking_conflict", "The requested dates conflict with an existing booking."));
        }

        var booking = new Booking
        {
            HotelId = hotelId,
            GuestName = request.GuestName.Trim(),
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            CreatedBy = createdBy
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _notificationService.SendBookingNotificationAsync(booking.BookingId, booking.GuestName, hotel.Name, cancellationToken);

        return ServiceResult<BookingResponseDto>.Success(new BookingResponseDto
        {
            BookingId = booking.BookingId,
            HotelId = booking.HotelId,
            GuestName = booking.GuestName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            CreatedBy = booking.CreatedBy
        });
    }
}
