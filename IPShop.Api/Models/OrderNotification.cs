using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Models;

public class OrderNotification
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
