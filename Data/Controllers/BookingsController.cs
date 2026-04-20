using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtelierBackend.Authorization;
using OtelierBackend.DTOs;
using OtelierBackend.DTOs.Common;
using OtelierBackend.Services.Bookings;
using OtelierBackend.Services.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OtelierBackend.Controllers;

[ApiController]
[Route("api/hotels/{hotelId}/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // Any authenticated user can view bookings
    [HttpGet]
    [Authorize(Policy = "AnyRole")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetBookings(
        int hotelId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingsAsync(hotelId, startDate, endDate, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(result.Value);
    }

    // Only staff or reception can create bookings
    [HttpPost]
    [Authorize(Policy = "StaffOrReception")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        int hotelId,
        [FromBody] CreateBookingDto request,
        CancellationToken cancellationToken)
    {
        // Extract userId from the JWT sub claim (set during token generation)
        var createdBy =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.Identity?.Name ??
            "unknown";

        var result = await _bookingService.CreateBookingAsync(
            hotelId,
            request,
            createdBy,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToActionResult(result.Error!);
        }

        var response = result.Value!;

        return Created($"/api/hotels/{hotelId}/bookings/{response.BookingId}", response);
    }

    private ActionResult ToActionResult(ServiceError error)
    {
        var response = new ApiErrorResponse
        {
            Code = error.Code,
            Message = error.Message
        };

        return error.Type switch
        {
            ServiceErrorType.Validation => BadRequest(response),
            ServiceErrorType.NotFound => NotFound(response),
            ServiceErrorType.Conflict => Conflict(response),
            ServiceErrorType.Unauthorized => Unauthorized(response),
            _ => BadRequest(response)
        };
    }
}