public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; }              
    public string Company { get; set; }           
    public string Email { get; set; }

    public string OrgNumber { get; set; }         
    public string Address { get; set; }           
    public string InvoiceAddress { get; set; }    

    public bool IsApproved { get; set; } = false;
}