namespace SugoBackend.DTOs;

public class GiftDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public long Price { get; set; }
    public string? IconUrl { get; set; }
}
