using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

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
    private readonly IWebHostEnvironment _environment;

    public RoomsController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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
            CreatedAt = room.CreatedAt,
            RoomPicture = room.RoomPicture
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
                CreatedAt = r.CreatedAt,
                RoomPicture = r.RoomPicture
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

    /// <summary>
    /// Upload room picture for a specific room
    /// </summary>
    /// <param name="roomId">Room ID</param>
    /// <param name="file">Image file</param>
    /// <returns>Uploaded image URL</returns>
    [HttpPost("{roomId}/upload-picture")]
    public async Task<IActionResult> UploadRoomPicture(int roomId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
        }

        // Get room and verify ownership
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        if (room.CreatedByUserId != userId)
        {
            return Forbid();
        }

        // Create uploads folder if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "rooms");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate unique filename
        var fileName = $"room_{roomId}_{DateTime.UtcNow.Ticks}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update room picture URL
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var imageUrl = $"{baseUrl}/uploads/rooms/{fileName}";

        room.RoomPicture = imageUrl;
        await _context.SaveChangesAsync();

        return Ok(new { imageUrl });
    }

    #endregion
}
