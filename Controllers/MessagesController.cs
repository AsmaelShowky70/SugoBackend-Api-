using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Send a message to a user
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var senderId = GetCurrentUserId();
        if (senderId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Message content is required" });

        if (dto.RecipientId == senderId)
            return BadRequest(new { message = "Cannot send message to yourself" });

        var recipient = await _context.Users.FindAsync(dto.RecipientId);
        if (recipient == null)
            return NotFound(new { message = "Recipient not found" });

        // Note: Message model doesn't exist yet - this is for reference
        // You need to create a Message model and DbSet in AppDbContext

        return Ok(new
        {
            id = 1,
            senderId = senderId,
            recipientId = dto.RecipientId,
            content = dto.Content,
            messageType = dto.MessageType ?? "text",
            mediaUrl = dto.MediaUrl,
            createdAt = DateTime.UtcNow,
            isRead = false
        });
    }

    /// <summary>
    /// Get conversation with a specific user
    /// </summary>
    [HttpGet("conversation/{userId}")]
    public async Task<IActionResult> GetConversation(int userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // This would fetch from database once Message model is created
        var messages = new List<object>(); // Placeholder

        return Ok(messages);
    }

    /// <summary>
    /// Get all conversations (list of users with messages)
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        // This would fetch all unique users the current user has messages with
        var conversations = new List<object>(); // Placeholder

        return Ok(conversations);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var id = GetCurrentUserId();
        if (id == null)
            return Unauthorized();

        // Update message read status
        return Ok(new { message = "Message marked as read" });
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Delete message logic
        return Ok(new { message = "Message deleted" });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
            return null;
        return id;
    }
}

/// <summary>
/// DTO for sending messages
/// </summary>
public class SendMessageDto
{
    public int RecipientId { get; set; }
    public required string Content { get; set; }
    public string? MessageType { get; set; } = "text"; // text, video, audio
    public string? MediaUrl { get; set; }
}
