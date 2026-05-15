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

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Company) ||
            string.IsNullOrWhiteSpace(customer.OrgNumber))
        {
            return BadRequest("Endast företag är tillåtna.");
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetCustomerList()
    {
        var rows = await _context.Customers
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Company, c.Email, c.OrgNumber })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        return customer;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, Customer updatedCustomer)
    {
        if (id != updatedCustomer.Id)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(updatedCustomer.Company) ||
            string.IsNullOrWhiteSpace(updatedCustomer.OrgNumber))
        {
            return BadRequest("Endast företag är tillåtna.");
        }

        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        customer.Name = updatedCustomer.Name;
        customer.Company = updatedCustomer.Company;
        customer.Email = updatedCustomer.Email;
        customer.OrgNumber = updatedCustomer.OrgNumber;
        customer.Address = updatedCustomer.Address;
        customer.InvoiceAddress = updatedCustomer.InvoiceAddress;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}