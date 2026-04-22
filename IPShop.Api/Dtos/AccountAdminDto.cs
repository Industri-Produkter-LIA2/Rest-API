public class AccountAdminDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public bool IsApproved { get; set; }

    public string? CompanyName { get; set; }
    public string? OrgNumber { get; set; }
}