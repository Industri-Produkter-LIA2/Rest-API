namespace IPShop.Api.Models;

public class Account
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }

    public bool IsApproved { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}