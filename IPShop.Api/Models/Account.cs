namespace IPShop.Api.Models;

public class Account
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}