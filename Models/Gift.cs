namespace SugoBackend.Models;

public class Gift
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public long Price { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<GiftTransaction> GiftTransactions { get; set; } = new List<GiftTransaction>();
}
