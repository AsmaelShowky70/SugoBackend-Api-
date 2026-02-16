using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.DTOs;
using SugoBackend.Models;

namespace SugoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModerationController : ControllerBase
{
    private readonly AppDbContext _context;

    public ModerationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("report")]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { message = "Reason is required" });
        }

        if (dto.TargetUserId == null && dto.RoomId == null)
        {
            return BadRequest(new { message = "TargetUserId or RoomId must be provided" });
        }

        if (dto.TargetUserId != null)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == dto.TargetUserId.Value, cancellationToken);
            if (!exists)
            {
                return NotFound(new { message = "Target user not found" });
            }
        }

        if (dto.RoomId != null)
        {
            var exists = await _context.Rooms.AnyAsync(r => r.Id == dto.RoomId.Value, cancellationToken);
            if (!exists)
            {
                return NotFound(new { message = "Room not found" });
            }
        }

        var report = new Report
        {
            ReporterUserId = userId.Value,
            TargetUserId = dto.TargetUserId,
            RoomId = dto.RoomId,
            Reason = dto.Reason.Trim(),
            Status = ReportStatus.Pending
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Report created", id = report.Id });
    }

    [HttpGet("my-reports")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetMyReports(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var reports = await _context.Reports
            .Where(r => r.ReporterUserId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
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
