namespace SugoBackend.DTOs;

public class SendGiftRequestDto
{
    public int GiftId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? RoomId { get; set; }
    public int? TargetUserId { get; set; }
}
