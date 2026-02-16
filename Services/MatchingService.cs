using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.Models;

namespace SugoBackend.Services;

public interface IMatchingService
{
    Task<Room?> GetRecommendedRoomAsync(int userId, CancellationToken cancellationToken = default);
}

public class MatchingService : IMatchingService
{
    private readonly AppDbContext _context;

    public MatchingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Room?> GetRecommendedRoomAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roomsWithScore = await _context.Rooms
            .Select(r => new
            {
                Room = r,
                TotalGifts = _context.GiftTransactions
                    .Where(gt => gt.RoomId == r.Id)
                    .Sum(gt => (long?)gt.TotalPrice) ?? 0,
                CreatedAt = r.CreatedAt
            })
            .OrderByDescending(x => x.TotalGifts)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return roomsWithScore.FirstOrDefault()?.Room;
    }
}
