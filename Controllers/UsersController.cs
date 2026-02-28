using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SugoBackend.Controllers;

/// <summary>
/// User management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public UsersController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    #region Public Methods

    /// <summary>
    /// Get user profile information
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile information</returns>
    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var profile = new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            ProfilePicture = user.ProfilePicture
        };

        return Ok(profile);
    }

    /// <summary>
    /// Get all users (Nearby/Discovery)
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Where(u => !u.IsBanned)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                ProfilePicture = u.ProfilePicture
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Upload profile picture for the current user
    /// </summary>
    /// <param name="file">Image file</param>
    /// <returns>Uploaded image URL</returns>
    [HttpPost("upload-picture")]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".heif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Invalid file type. Only JPG, PNG, GIF, HEIC/HEIF or WEBP are allowed." });
        }

        // Get current user
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Create uploads folder if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate unique filename
        var fileName = $"profile_{userId}_{DateTime.UtcNow.Ticks}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update user profile picture URL
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var imageUrl = $"{baseUrl}/uploads/profiles/{fileName}";

        user.ProfilePicture = imageUrl;
        await _context.SaveChangesAsync();

        return Ok(new { imageUrl });
    }

    #endregion
}
