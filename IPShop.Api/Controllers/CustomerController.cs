using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPShop.Api.Data;
using IPShop.Api.Models;

namespace IPShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly IPShopDbContext _context;

    public CustomerController(IPShopDbContext context)
    {
        _context = context;
    }

    // GET customer
    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        return customer;
    }

    // UPDATE customer
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, Customer updatedCustomer)
    {
        if (id != updatedCustomer.Id)
            return BadRequest();

        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        customer.Name = updatedCustomer.Name;
        customer.Company = updatedCustomer.Company;
        customer.Email = updatedCustomer.Email;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}