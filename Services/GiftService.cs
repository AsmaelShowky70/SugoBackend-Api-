using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.Models;

namespace SugoBackend.Services;

public interface IGiftService
{
    Task<IReadOnlyList<Gift>> GetActiveGiftsAsync(CancellationToken cancellationToken = default);
    Task<GiftTransaction> SendGiftAsync(
        int senderUserId,
        int giftId,
        int quantity,
        int? roomId,
        int? targetUserId,
        CancellationToken cancellationToken = default);
}

public class GiftService : IGiftService
{
    private readonly AppDbContext _context;
    private readonly IWalletService _walletService;

    public GiftService(AppDbContext context, IWalletService walletService)
    {
        _context = context;
        _walletService = walletService;
    }

    public async Task<IReadOnlyList<Gift>> GetActiveGiftsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Gifts
            .Where(g => g.IsActive)
            .OrderBy(g => g.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<GiftTransaction> SendGiftAsync(
        int senderUserId,
        int giftId,
        int quantity,
        int? roomId,
        int? targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        var gift = await _context.Gifts.FirstOrDefaultAsync(
            g => g.Id == giftId && g.IsActive,
            cancellationToken);

        if (gift == null)
        {
            throw new InvalidOperationException("Gift not found or inactive.");
        }

        if (roomId == null && targetUserId == null)
        {
            throw new InvalidOperationException("Either roomId or targetUserId must be provided.");
        }

        var totalPrice = gift.Price * quantity;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await _walletService.GetOrCreateWalletAsync(senderUserId, cancellationToken);

        if (wallet.Balance < totalPrice)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        wallet.Balance -= totalPrice;
        wallet.UpdatedAt = DateTime.UtcNow;

        var giftTransaction = new GiftTransaction
        {
            SenderUserId = senderUserId,
            TargetUserId = targetUserId,
            RoomId = roomId,
            GiftId = giftId,
            Quantity = quantity,
            TotalPrice = totalPrice
        };

        _context.GiftTransactions.Add(giftTransaction);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return giftTransaction;
    }
}
