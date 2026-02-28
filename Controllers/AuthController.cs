using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;
using SugoBackend.Services;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

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
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AuthController(AppDbContext context, ITokenService tokenService, IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
        _httpClient = new HttpClient();
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

    /// <summary>
    /// Login or register user via social providers (Google, Facebook)
    /// </summary>
    /// <param name="socialLoginDto">Social login information</param>
    /// <returns>Login response with JWT token</returns>
    [HttpPost("social-login")]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto socialLoginDto)
    {
        string email = string.Empty;
        string username = string.Empty;
        string socialId = string.Empty;

        if (socialLoginDto.Provider.ToLower() == "google")
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                return StatusCode(500, new { message = "Google Client ID not configured." });
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(socialLoginDto.AccessToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
                email = payload.Email;
                username = payload.Name;
                socialId = payload.Subject; // This is the Google unique user ID
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google Access Token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Google login failed: {ex.Message}" });
            }
        }
        else if (socialLoginDto.Provider.ToLower() == "facebook")
        {
            var appAccessToken = $"{_configuration["Authentication:Facebook:AppId"]}|{_configuration["Authentication:Facebook:AppSecret"]}";
            if (string.IsNullOrEmpty(appAccessToken))
            {
                return StatusCode(500, new { message = "Facebook App ID or Secret not configured." });
            }

            try
            {
                // Validate user access token
                var debugTokenUrl = $"https://graph.facebook.com/debug_token?input_token={socialLoginDto.AccessToken}&access_token={appAccessToken}";
                var debugTokenResponse = await _httpClient.GetFromJsonAsync<FacebookDebugTokenResponse>(debugTokenUrl);

                if (debugTokenResponse == null || !debugTokenResponse.Data.IsValid)
                {
                    return Unauthorized(new { message = "Invalid Facebook Access Token." });
                }

                // Get user profile
                var userProfileUrl = $"https://graph.facebook.com/me?fields=id,name,email&access_token={socialLoginDto.AccessToken}";
                var userProfileResponse = await _httpClient.GetFromJsonAsync<FacebookUserProfileResponse>(userProfileUrl);

                if (userProfileResponse == null || string.IsNullOrEmpty(userProfileResponse.Email))
                {
                    return BadRequest(new { message = "Could not retrieve Facebook user email." });
                }

                email = userProfileResponse.Email;
                username = userProfileResponse.Name;
                socialId = userProfileResponse.Id; // This is the Facebook unique user ID
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Facebook login failed: {ex.Message}" });
            }
        }
        else
        {
            return BadRequest(new { message = "Unsupported social provider." });
        }

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { message = "Could not retrieve email from social provider." });
        }

        // Try to find user by Social ID first, then by Email
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            (u.SocialProvider == socialLoginDto.Provider.ToLower() && u.SocialId == socialId) ||
            u.Email == email);

        if (user == null)
        {
            // Register new user
            user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(Guid.NewGuid().ToString()), // Generate a random password for social users
                SocialProvider = socialLoginDto.Provider.ToLower(),
                SocialId = socialId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(user.SocialId))
        {
            // Link existing user to social account if they logged in with email before
            user.SocialProvider = socialLoginDto.Provider.ToLower();
            user.SocialId = socialId;
            await _context.SaveChangesAsync();
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
