namespace IPShop.Api.Models;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CartItem> Items { get; set; } = new();

    //todo add user id
}
