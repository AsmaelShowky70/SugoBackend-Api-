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

    #region Navigation Properties
    public ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
    #endregion
}
