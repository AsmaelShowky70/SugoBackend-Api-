using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;
using SugoBackend.Services;

namespace SugoBackend.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    #region Public Methods

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="registerDto">Registration information</param>
    /// <returns>User registration result</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(registerDto.Username) ||
            string.IsNullOrWhiteSpace(registerDto.Email) ||
            string.IsNullOrWhiteSpace(registerDto.Password))
        {
            return BadRequest(new { message = "Username, Email, and Password are required" });
        }

        // Check if user already exists
        var existingUser = _context.Users.FirstOrDefault(u => u.Email == registerDto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already in use" });
        }

        // Create new user
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully", userId = user.Id });
    }

    /// <summary>
    /// Login user and return JWT token
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Login response with JWT token</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(loginDto.Email) ||
            string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return BadRequest(new { message = "Email and Password are required" });
        }

        // Find user
        var user = _context.Users.FirstOrDefault(u => u.Email == loginDto.Email);
        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Generate token
        var token = _tokenService.GenerateToken(user);

        var response = new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token
        };

        return Ok(response);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Hash password using PBKDF2
    /// </summary>
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBuffer = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBuffer);
        }
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    #endregion
}
