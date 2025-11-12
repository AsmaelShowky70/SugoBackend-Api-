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
    }
    #endregion
}
