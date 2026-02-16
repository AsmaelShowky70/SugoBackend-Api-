using Microsoft.EntityFrameworkCore;
using SugoBackend.Models;

namespace SugoBackend.Data;

/// <summary>
/// Entity Framework Core DbContext for Sugo application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<Gift> Gifts { get; set; } = null!;
    public DbSet<GiftTransaction> GiftTransactions { get; set; } = null!;
    public DbSet<Report> Reports { get; set; } = null!;
    #endregion

    #region Model Configuration
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            entity.HasMany(e => e.CreatedRooms)
                .WithOne(r => r.CreatedByUser)
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedRooms)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).IsRequired();
            entity.HasOne(e => e.User)
                .WithOne(u => u.Wallet)
                .HasForeignKey<Wallet>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Gift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasData(
                new Gift { Id = 1, Name = "Rose", Price = 10, IconUrl = "rose.png", IsActive = true },
                new Gift { Id = 2, Name = "Lion", Price = 1000, IconUrl = "lion.png", IsActive = true },
                new Gift { Id = 3, Name = "Crown", Price = 5000, IconUrl = "crown.png", IsActive = true }
            );
        });

        modelBuilder.Entity<GiftTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.TotalPrice).IsRequired();

            entity.HasOne(e => e.SenderUser)
                .WithMany(u => u.SentGifts)
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetUser)
                .WithMany(u => u.ReceivedGifts)
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.GiftTransactions)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Gift)
                .WithMany(g => g.GiftTransactions)
                .HasForeignKey(e => e.GiftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasOne(e => e.ReporterUser)
                .WithMany(u => u.ReportsCreated)
                .HasForeignKey(e => e.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetUser)
                .WithMany(u => u.ReportsReceived)
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    #endregion
}
