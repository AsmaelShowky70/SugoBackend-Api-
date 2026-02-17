using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;
using SugoBackend.Services;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGiftService _giftService;
    private readonly IWalletService _walletService;

    public AdminController(AppDbContext context, IGiftService giftService, IWalletService walletService)
    {
        _context = context;
        _giftService = giftService;
        _walletService = walletService;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports([FromQuery] ReportStatus? status, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var query = _context.Reports.AsQueryable();

        if (status != null)
        {
            query = query.Where(r => r.Status == status);
        }

        var reports = await query
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = reports.Select(r => new ReportDto
        {
            Id = r.Id,
            ReporterUserId = r.ReporterUserId,
            TargetUserId = r.TargetUserId,
            RoomId = r.RoomId,
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt
        }).ToList();

        return Ok(result);
    }

    [HttpPost("reports/{id}/update-status")]
    public async Task<IActionResult> UpdateReportStatus(int id, [FromQuery] ReportStatus status, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report == null)
        {
            return NotFound(new { message = "Report not found" });
        }

        report.Status = status;
        report.ResolvedAt = status is ReportStatus.Resolved or ReportStatus.Rejected ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Report status updated" });
    }

    #region Users Management

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            IsAdmin = u.IsAdmin,
            IsBanned = u.IsBanned
        }).ToList();

        return Ok(result);
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(int id, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.IsAdmin)
        {
            return BadRequest(new { message = "Cannot ban admin users" });
        }

        user.IsBanned = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "User banned successfully" });
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(int id, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.IsBanned = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "User unbanned successfully" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.IsAdmin)
        {
            return BadRequest(new { message = "Cannot delete admin users" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "User deleted successfully" });
    }

    #endregion

    #region Rooms Management

    [HttpGet("rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAllRooms(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var rooms = await _context.Rooms
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = rooms.Select(r => new RoomDto
        {
            Id = r.Id,
            Name = r.Name,
            CreatedByUserId = r.CreatedByUserId,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpPut("rooms/{id}")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomDto roomDto, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        if (string.IsNullOrWhiteSpace(roomDto.Name))
        {
            return BadRequest(new { message = "Room name is required" });
        }

        room.Name = roomDto.Name;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            CreatedByUserId = room.CreatedByUserId,
            CreatedAt = room.CreatedAt
        });
    }

    [HttpDelete("rooms/{id}")]
    public async Task<IActionResult> DeleteRoom(int id, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null)
        {
            return NotFound(new { message = "Room not found" });
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Room deleted successfully" });
    }

    #endregion

    #region Gifts Management

    [HttpGet("gifts")]
    public async Task<ActionResult<IEnumerable<GiftDto>>> GetAllGifts(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

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

    [HttpPost("gifts/send-to-user")]
    public async Task<IActionResult> SendGiftToUser([FromBody] SendGiftToUserDto dto, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        if (dto.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be greater than zero" });
        }

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, cancellationToken);
        if (targetUser == null)
        {
            return NotFound(new { message = "Target user not found" });
        }

        try
        {
            // Admin sends gift for free (no wallet deduction)
            var transaction = await _giftService.SendGiftAsync(
                0, // Admin sender (0 = system/admin)
                dto.GiftId,
                dto.Quantity,
                null, // No room
                dto.UserId,
                cancellationToken);

            return Ok(new
            {
                transactionId = transaction.Id,
                giftId = transaction.GiftId,
                quantity = transaction.Quantity,
                targetUserId = transaction.TargetUserId,
                createdAt = transaction.CreatedAt,
                message = "Gift sent successfully"
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

    [HttpDelete("gifts/{id}")]
    public async Task<IActionResult> DeleteGift(int id, CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var gift = await _context.Gifts.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (gift == null)
        {
            return NotFound(new { message = "Gift not found" });
        }

        gift.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Gift deleted successfully" });
    }

    #endregion

    private async Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
        {
            return false;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user != null && user.IsAdmin;
    }
}
