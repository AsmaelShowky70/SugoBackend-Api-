using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.DTOs;
using SugoBackend.Services;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;

    public MatchingController(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpGet("recommend-room")]
    public async Task<IActionResult> RecommendRoom(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var room = await _matchingService.GetRecommendedRoomAsync(userId.Value, cancellationToken);

        if (room == null)
        {
            return NotFound(new { message = "No rooms available" });
        }

        var dto = new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            CreatedByUserId = room.CreatedByUserId,
            CreatedAt = room.CreatedAt
        };

        return Ok(dto);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
        {
            return null;
        }

        return id;
    }
}
