namespace SugoBackend.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public long Balance { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
