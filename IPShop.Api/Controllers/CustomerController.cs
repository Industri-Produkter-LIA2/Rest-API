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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        if (!IsBusinessCustomer(customer))
            return BadRequest(new { message = "Endast företag är tillåtna." });

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }
    
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return NotFound(new { message = $"Customer with ID {id} was not found." });

        return Ok(customer);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(int id, Customer updatedCustomer)
    {
        if (id != updatedCustomer.Id)
            return BadRequest(new { message = "URL ID and Customer ID mismatch." });

        if (!IsBusinessCustomer(updatedCustomer))
            return BadRequest(new { message = "Endast företag är tillåtna." });

        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound(new { message = $"Customer with ID {id} was not found." });

        // Map updated properties
        customer.CompanyName = updatedCustomer.CompanyName;
        customer.OrgNumber = updatedCustomer.OrgNumber;
        customer.Address = updatedCustomer.Address;
        customer.InvoiceAddress = updatedCustomer.InvoiceAddress;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Helper method to validate that the customer has the required business fields.
    /// </summary>
    private static bool IsBusinessCustomer(Customer customer)
    {
        return !string.IsNullOrWhiteSpace(customer.CompanyName) &&
               !string.IsNullOrWhiteSpace(customer.OrgNumber);
    }
}