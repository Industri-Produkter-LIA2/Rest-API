// Controllers/CartController.cs
using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly IPShopDbContext _dbContext;

    public CartController(IPShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // POST: api/cart
    [HttpPost]
    public async Task<ActionResult<CartResponse>> CreateCart([FromQuery] int? customerId)
    {
        var cart = new Cart();

        if (customerId.HasValue)
        {
            var customer = await _dbContext.Customers.FindAsync(customerId.Value);
            if (customer == null)
                return NotFound(new { message = "Customer not found" });

            cart.CustomerId = customerId.Value;
        }

        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        return Ok(new CartResponse
        {
            Id = cart.Id,
            CreatedAt = cart.CreatedAt,
            CustomerId = cart.CustomerId,
            Items = cart.Items ?? new List<CartItem>()
        });
    }

    // GET: api/cart/{cartId}
    [HttpGet("{cartId:guid}")]
    public async Task<ActionResult<Cart>> GetCart(Guid cartId)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            return NotFound();

        return Ok(cart);
    }

    // POST: api/cart/{cartId}/items
    [HttpPost("{cartId:guid}/items")]
    public async Task<ActionResult<Cart>> AddCartItem(Guid cartId, [FromBody] AddToCartRequest request)
    {
        // Get cart with items
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            return NotFound(new { message = "Cart not found" });

        // Check if product exists
        var product = await _dbContext.Products.FindAsync(request.ProductId);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        // Check if quantity is valid
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0" });

        // Check if product already exists in cart
        var existingItem = cart.Items
            .FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            // Update quantity if product already in cart
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            // Add new item to cart
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CartId = cartId
            });
        }

        await _dbContext.SaveChangesAsync();

        // Return updated cart with product details
        var updatedCart = await _dbContext.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        return Ok(updatedCart);
    }

    // DELETE: api/cart/{cartId}/items/{itemId}
    [HttpDelete("{cartId:guid}/items/{itemId:int}")]
    public async Task<IActionResult> RemoveCartItem(Guid cartId, int itemId)
    {
        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);

        if (item == null)
            return NotFound();

        _dbContext.CartItems.Remove(item);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/cart/{cartId}/items/{itemId}
    [HttpPatch("{cartId:guid}/items/{itemId:int}")]
    public async Task<IActionResult> UpdateCartItemQuantity(
        Guid cartId,
        int itemId,
        [FromBody] UpdateCartItemQuantityRequest request)
    {
        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);

        if (item == null)
            return NotFound(new { message = "Cart item not found" });

        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0" });

        item.Quantity = request.Quantity;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/cart/{cartId}
    [HttpDelete("{cartId:guid}")]
    public async Task<IActionResult> ClearCart(Guid cartId)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            return NotFound(new { message = "Cart not found" });

        _dbContext.CartItems.RemoveRange(cart.Items);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

// Request/Response Models
public class CartResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateCartItemQuantityRequest
{
    public int Quantity { get; set; }
}