using Microsoft.AspNetCore.Mvc;
using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using IPShop.Api.Models.Constants;

namespace IPShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IPShopDbContext _context;

    public AuthController(IPShopDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email && a.Password == request.Password); // We need to encrypt passwords, but for now I'm leaving them plain text just whilst we're setting up and testing.

        if (account == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (!account.IsApproved)
            return BadRequest(new { message = "Account not approved yet" });

        return Ok(new
        {
            id = account.Id,
            customerId = account.CustomerId,
            username = account.Username,
            email = account.Email,
            role = account.Role
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var emailExists = await _context.Accounts.AnyAsync(a => a.Email == email);

        if (emailExists) return Conflict(new { message = "Email already registered" });

        var orgExists = await _context.Customers.AnyAsync(c => c.OrgNumber == request.OrgNumber);

        if (orgExists) return Conflict(new { message = "Organization number already registered" });

        var customer = new Customer // Address/Invoice address are not being saved on registration, they should be added in the customer details page after approval to avoid unnecessary complexity.
        {
            Name = request.CompanyName,
            OrgNumber = request.OrgNumber,

            Email = email, // Email is currently being duplicated in customer and account, this one should realistically be removed, but I'm keeping it for now.
            IsApproved = false // Also currently duplicated, is again just here temporarily so the project doesn't break.
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var account = new Account
        {
            Email = email,
            Username = request.Username,
            Password = request.Password,
            Role = Roles.Customer,
            IsApproved = false,
            CustomerId = customer.Id
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration successful, pending approval" });
    }

    [HttpPatch("approve/{id}")]
    public async Task<IActionResult> ApproveAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account == null)
            return NotFound(new { message = "Account not found" });

        account.IsApproved = true;

        if (account.Customer != null) account.Customer.IsApproved = true; // Temporary measure to ensure customer is also approved until we remove IsApproved from customer.

        await _context.SaveChangesAsync();

        return Ok(new { message = "Account approved successfully" });
    }

    [HttpPatch("reject/{id}")]
    public async Task<IActionResult> RejectAccount(int id)
    {
        var account = await _context.Accounts.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
            return NotFound(new { message = "Account not found" });

        if (account.Customer != null)
        {
            _context.Customers.Remove(account.Customer);
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Account rejected and removed successfully" });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _context.Accounts
            .Select(a => new AccountAdminDto
            {
                Id = a.Id,
                Email = a.Email,
                Username = a.Username,
                Role = a.Role,
                IsApproved = a.IsApproved,
                CompanyName = a.Customer!.Name,
                OrgNumber = a.Customer!.OrgNumber
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingAccounts()
    {
        var accounts = await _context.Accounts
            .Where(a => !a.IsApproved)
            .Select(a => new AccountAdminDto
            {
                Id = a.Id,
                Email = a.Email,
                Username = a.Username,
                Role = a.Role,
                IsApproved = a.IsApproved,
                CompanyName = a.Customer!.Name,
                OrgNumber = a.Customer!.OrgNumber
            })
            .ToListAsync();

        return Ok(accounts);
    }
}