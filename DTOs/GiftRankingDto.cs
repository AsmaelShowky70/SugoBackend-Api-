namespace SugoBackend.DTOs;

public class GiftRankingDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long TotalSpent { get; set; }
}
