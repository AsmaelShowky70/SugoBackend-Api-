namespace SugoBackend.Models;

/// <summary>
/// Represents a user in the Sugo application
/// </summary>
public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; } = false;

    #region Navigation Properties
    public ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
    public Wallet? Wallet { get; set; }
    public ICollection<GiftTransaction> SentGifts { get; set; } = new List<GiftTransaction>();
    public ICollection<GiftTransaction> ReceivedGifts { get; set; } = new List<GiftTransaction>();
    public ICollection<Report> ReportsCreated { get; set; } = new List<Report>();
    public ICollection<Report> ReportsReceived { get; set; } = new List<Report>();
    #endregion
}
