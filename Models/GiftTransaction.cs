namespace SugoBackend.Models;

public class GiftTransaction
{
    public int Id { get; set; }
    public int SenderUserId { get; set; }
    public int? TargetUserId { get; set; }
    public int? RoomId { get; set; }
    public int GiftId { get; set; }
    public int Quantity { get; set; }
    public long TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? SenderUser { get; set; }
    public User? TargetUser { get; set; }
    public Room? Room { get; set; }
    public Gift? Gift { get; set; }
}
