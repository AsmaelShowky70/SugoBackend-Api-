namespace SugoBackend.DTOs;

public class SendGiftToUserDto
{
    public int UserId { get; set; }
    public int GiftId { get; set; }
    public int Quantity { get; set; }
}
