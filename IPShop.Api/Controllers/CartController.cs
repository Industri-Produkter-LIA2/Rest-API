// Controllers/CartController.cs
using IPShop.Api.Data;
using IPShop.Api.Dtos;
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
        var customers = await _dbContext.Customers.ToListAsync();

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

        var updatedCart = await _dbContext.Carts
         .Include(c => c.Items)
         .ThenInclude(i => i.Product) // This loads the product details
         .Select(x => new CartDto
         {
             CustomerId = x.CustomerId,
             CreatedAt = x.CreatedAt,
             Id = x.Id,
             Items = x.Items
             .Select(a => new CartItemDto
             {
                 Id = a.Id,
                 CartId = a.CartId,
                 ProductId = a.ProductId,
                 Quantity = a.Quantity,
                 // YOU MUST ADD THESE TWO LINES FOR THE FRONTEND:
                 ProductName = a.Product.Name,
                 Price = a.Product.Price
             }).ToList(),
         })
         .FirstOrDefaultAsync(c => c.Id == cartId);

        if (updatedCart == null)
            return NotFound();

        return Ok(updatedCart);
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
            .Select(x => new CartDto
            {
                CustomerId = x.CustomerId,
                CreatedAt = x.CreatedAt,
                Id = x.Id,
                Items = x.Items
                .Select(a => new CartItemDto { Id = a.Id,CartId = a.CartId , ProductId = a.ProductId,Quantity = a.Quantity}).ToList(),
            })
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

    // 1. CHANGED: Renamed itemId to productId in the route
    [HttpPatch("{cartId:guid}/items/{productId:int}")]
    public async Task<IActionResult> UpdateCartItemQuantity(
        Guid cartId,
        int productId, // 2. CHANGED variable name
        [FromBody] UpdateCartItemQuantityRequest request)
    {
        // 3. CHANGED: Search by ProductId instead of the cart item's Primary Key (i.Id)
        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.CartId == cartId);

        if (item == null)
            return NotFound(new { message = "Cart item not found" });

        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0" });

        item.Quantity = request.Quantity;
        
        await _dbContext.SaveChangesAsync();

        return NoContent(); // 204 Success!
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

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserCart(int id)
    {
        // 1. Verify customer exists (AnyAsync is much faster than FindAsync if we only need to check existence)
        var customerExists = await _dbContext.Customers.AnyAsync(c => c.Id == id);
        if (!customerExists)
            return NotFound(new { message = $"Customer {id} not found" });

        // 2. Query the database and map directly to the DTO in one step
        var cartDto = await _dbContext.Carts
            .Include(c => c.Items)
            .ThenInclude(c => c.Product)
            .Where(x => x.CustomerId == id)
            .Select(x => new CartDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CreatedAt = x.CreatedAt,
                Items = x.Items.Select(a => new CartItemDto
                {
                    Id = a.Id,
                    CartId = a.CartId,
                    ProductId = a.ProductId,
                    ProductName = a.Product.Name,
                    Price = a.Product.Price, // MUST INCLUDE PRICE FOR FRONTEND MATH!
                    Quantity = a.Quantity
                }).ToList()
            })
            .FirstOrDefaultAsync();

        // 3. If the cart exists, return it immediately
        if (cartDto != null)
            return Ok(cartDto);

        // 4. If it does NOT exist, create it
        var newCart = new Cart
        {
            CustomerId = id,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Carts.Add(newCart);
        await _dbContext.SaveChangesAsync();

        // 5. Instantly return a new DTO (No need to query the database again since it's empty!)
        return Ok(new CartDto
        {
            Id = newCart.Id,
            CustomerId = newCart.CustomerId,
            CreatedAt = newCart.CreatedAt,
            Items = new List<CartItemDto>()
        });
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