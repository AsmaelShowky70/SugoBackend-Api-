using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.Data;
using SugoBackend.DTOs;

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

    public UsersController(AppDbContext context)
    {
        _context = context;
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
            CreatedAt = user.CreatedAt
        };

        return Ok(profile);
    }

    #endregion
}
