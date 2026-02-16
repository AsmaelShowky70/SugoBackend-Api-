using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Services;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWalletService _walletService;

    public WalletController(AppDbContext context, IWalletService walletService)
    {
        _context = context;
        _walletService = walletService;
    }

    [HttpGet]
    public async Task<ActionResult<WalletDto>> GetCurrentWallet(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var exists = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
        if (exists == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var wallet = await _walletService.GetOrCreateWalletAsync(userId.Value, cancellationToken);

        return Ok(new WalletDto
        {
            UserId = wallet.UserId,
            Balance = wallet.Balance
        });
    }

    [HttpPost("topup")]
    public async Task<ActionResult<WalletDto>> TopUp([FromBody] TopUpWalletDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero" });
        }

        var exists = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
        if (exists == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var balance = await _walletService.TopUpAsync(userId.Value, dto.Amount, cancellationToken);

        return Ok(new WalletDto
        {
            UserId = userId.Value,
            Balance = balance
        });
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
