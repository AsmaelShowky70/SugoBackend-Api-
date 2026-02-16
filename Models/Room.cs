namespace SugoBackend.Models;

/// <summary>
/// Represents a chat room in the Sugo application
/// </summary>
public class Room
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #region Navigation Properties
    public User? CreatedByUser { get; set; }
    public ICollection<GiftTransaction> GiftTransactions { get; set; } = new List<GiftTransaction>();
    #endregion
}
