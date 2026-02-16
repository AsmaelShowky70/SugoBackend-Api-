using Microsoft.EntityFrameworkCore;
using SugoBackend.Data;
using SugoBackend.Models;

namespace SugoBackend.Services;

public interface IWalletService
{
    Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default);
    Task<long> GetBalanceAsync(int userId, CancellationToken cancellationToken = default);
    Task<long> TopUpAsync(int userId, long amount, CancellationToken cancellationToken = default);
}

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;

    public WalletService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet != null)
        {
            return wallet;
        }

        wallet = new Wallet
        {
            UserId = userId,
            Balance = 0
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);

        return wallet;
    }

    public async Task<long> GetBalanceAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        return wallet?.Balance ?? 0;
    }

    public async Task<long> TopUpAsync(int userId, long amount, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return wallet.Balance;
    }
}
