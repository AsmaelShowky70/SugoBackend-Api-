using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Services;
using Microsoft.AspNetCore.SignalR;
using SugoBackend.Hubs;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GiftsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGiftService _giftService;
    private readonly IWalletService _walletService;
    private readonly IHubContext<RoomHub> _roomHub;

    public GiftsController(AppDbContext context, IGiftService giftService, IWalletService walletService, IHubContext<RoomHub> roomHub)
    {
        _context = context;
        _giftService = giftService;
        _walletService = walletService;
        _roomHub = roomHub;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GiftDto>>> GetGifts(CancellationToken cancellationToken)
    {
        var gifts = await _giftService.GetActiveGiftsAsync(cancellationToken);

        var result = gifts.Select(g => new GiftDto
        {
            Id = g.Id,
            Name = g.Name,
            Price = g.Price,
            IconUrl = g.IconUrl
        }).ToList();

        return Ok(result);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendGift([FromBody] SendGiftRequestDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        if (dto.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be greater than zero" });
        }

        if (dto.RoomId == null && dto.TargetUserId == null)
        {
            return BadRequest(new { message = "Either roomId or targetUserId must be provided" });
        }

        if (dto.RoomId != null)
        {
            var roomExists = await _context.Rooms.AnyAsync(r => r.Id == dto.RoomId, cancellationToken);
            if (!roomExists)
            {
                return NotFound(new { message = "Room not found" });
            }
        }

        if (dto.TargetUserId != null)
        {
            var targetExists = await _context.Users.AnyAsync(u => u.Id == dto.TargetUserId, cancellationToken);
            if (!targetExists)
            {
                return NotFound(new { message = "Target user not found" });
            }
        }

        try
        {
            var transaction = await _giftService.SendGiftAsync(
                userId.Value,
                dto.GiftId,
                dto.Quantity,
                dto.RoomId,
                dto.TargetUserId,
                cancellationToken);

            var remainingBalance = await _walletService.GetBalanceAsync(userId.Value, cancellationToken);

            if (transaction.RoomId != null)
            {
                await _roomHub.Clients
                    .Group(transaction.RoomId.Value.ToString())
                    .SendAsync("GiftReceived", new
                    {
                        roomId = transaction.RoomId,
                        giftId = transaction.GiftId,
                        quantity = transaction.Quantity,
                        senderUserId = transaction.SenderUserId,
                        targetUserId = transaction.TargetUserId,
                        createdAt = transaction.CreatedAt
                    }, cancellationToken);
            }

            return Ok(new
            {
                transactionId = transaction.Id,
                giftId = transaction.GiftId,
                quantity = transaction.Quantity,
                totalPrice = transaction.TotalPrice,
                roomId = transaction.RoomId,
                targetUserId = transaction.TargetUserId,
                createdAt = transaction.CreatedAt,
                remainingBalance
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { message = "Invalid quantity or amount" });
        }
    }

    [HttpGet("ranking/room/{roomId}")]
    public async Task<ActionResult<IEnumerable<GiftRankingDto>>> GetRoomRanking(int roomId, CancellationToken cancellationToken)
    {
        var roomExists = await _context.Rooms.AnyAsync(r => r.Id == roomId, cancellationToken);
        if (!roomExists)
        {
            return NotFound(new { message = "Room not found" });
        }

        var rankings = await _context.GiftTransactions
            .Where(gt => gt.RoomId == roomId)
            .GroupBy(gt => gt.SenderUserId)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Total)
            .Take(50)
            .ToListAsync(cancellationToken);

        var userIds = rankings.Select(r => r.UserId).ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = rankings
            .Select(r => new GiftRankingDto
            {
                UserId = r.UserId,
                Username = users.TryGetValue(r.UserId, out var user) ? user.Username : string.Empty,
                TotalSpent = r.Total
            })
            .ToList();

        return Ok(result);
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
