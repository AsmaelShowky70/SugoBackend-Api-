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
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
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
