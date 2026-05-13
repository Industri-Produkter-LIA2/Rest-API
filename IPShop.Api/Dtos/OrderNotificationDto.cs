using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Dtos;

public class OrderNotificationDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class SendOrderNotificationDto
{
    [MaxLength(500)]
    public string? Message { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Message { get; set; }
}
