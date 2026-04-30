namespace IPShop.Api.Models;

public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string OrgNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string InvoiceAddress { get; set; } = string.Empty;
}