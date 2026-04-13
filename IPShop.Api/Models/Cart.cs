namespace IPShop.Api.Models;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual List<CartItem> Items { get; set; } = new();

    public int? CustomerId { get; set; }

    public virtual Customer? Customer { get; set; }
}
