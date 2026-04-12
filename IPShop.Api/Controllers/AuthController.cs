using Microsoft.AspNetCore.Mvc;
using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;

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
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == request.Email && a.Password == request.Password); // We need to encrypt passwords, but for now I'm leaving them plain text just whilst we're setting up and testing.

        if (account == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (!account.IsApproved)
            return BadRequest(new { message = "Account not approved yet" });

        return Ok(new
        {
            email = account.Email,
            role = account.Role
        });
    }
}