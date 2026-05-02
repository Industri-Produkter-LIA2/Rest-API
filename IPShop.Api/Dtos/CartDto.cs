using IPShop.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Dtos;

public class CartDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CartItemDto> Items { get; set; } = new();
    public int? CustomerId { get; set; }
}
public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName {  get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}