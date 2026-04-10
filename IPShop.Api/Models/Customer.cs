namespace IPShop.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } // Maybe rename this as CompanyName, "Name" feels closer to what we would use during login.
    public string Company { get; set; } // Pretty sure this is unneeded, as we can just use the name as the company, and the rest is handled in Account.cs.
    public string Email { get; set; }
}