using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;

namespace SugoBackend.Controllers;

/// <summary>
/// Chat room management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RoomsController(AppDbContext context)
    {
        _context = context;
    }

    #region Public Methods

    /// <summary>
    /// Create a new chat room
    /// </summary>
    /// <param name="roomDto">Room creation information</param>
    /// <returns>Created room information</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateRoom([FromBody] RoomDto roomDto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(roomDto.Name))
        {
            return BadRequest(new { message = "Room name is required" });
        }

        // Get current user ID from JWT claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userId == null || !int.TryParse(userId.Value, out var currentUserId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        // Create room
        var room = new Room
        {
            Name = roomDto.Name,
            CreatedByUserId = currentUserId
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var createdRoomDto = new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            CreatedByUserId = room.CreatedByUserId,
            CreatedAt = room.CreatedAt
        };

        return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, createdRoomDto);
    }

    /// <summary>
    /// List all available chat rooms
    /// </summary>
    /// <returns>List of all rooms</returns>
    [HttpGet("list")]
    public async Task<IActionResult> ListRooms()
    {
        var rooms = _context.Rooms
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                CreatedByUserId = r.CreatedByUserId,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        return Ok(rooms);
    }

    /// <summary>
    /// Get a specific room by ID
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Room information</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        var roomDto = new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            CreatedByUserId = room.CreatedByUserId,
            CreatedAt = room.CreatedAt
        };

        return Ok(roomDto);
    }

    #endregion
}
