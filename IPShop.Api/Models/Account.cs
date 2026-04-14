using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Models;

public class Account
{
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}