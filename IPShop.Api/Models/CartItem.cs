using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Models;

public class CartItem
{
    public int Id { get; set; }

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Range(1, 1000)]
    public int Quantity { get; set; }
}