public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Maybe rename this as CompanyName, "Name" feels closer to what we would use during login.
    public string Company { get; set; } = string.Empty; // Pretty sure this is unneeded, as we can just use the name as the company, and the rest is handled in Account.cs.
    public string Email { get; set; } = string.Empty; // Should also be handled in Account.cs.

    public string OrgNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string InvoiceAddress { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = false; // Should be handled in Account.cs instead as we should be using this as a reference for the customer, not as an actual entity for login.
}