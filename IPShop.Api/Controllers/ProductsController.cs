using IPShop.Api.Data;
using IPShop.Api.Dtos;
using IPShop.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IPShopDbContext _dbContext;

    public ProductsController(IPShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetProducts(
        [FromQuery] string? articleNumber,
        [FromQuery] string? name,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            return BadRequest(new { message = "minPrice cannot be greater than maxPrice." });

        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(articleNumber))
            query = query.Where(p => p.ArticleNumber.Contains(articleNumber.Trim()));

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name.Trim()));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.Contains(category.Trim()));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Items = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);

        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found." });

        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDto createDto)
    {
        if (!await IsArticleNumberUniqueAsync(createDto.ArticleNumber))
            return Conflict(new { message = "ArticleNumber already exists." });

        var product = new Product
        {
            ArticleNumber = createDto.ArticleNumber,
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Category = createDto.Category,
            ImageUrl = createDto.ImageUrl ?? string.Empty
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
    {
        var existingProduct = await _dbContext.Products.FindAsync(id);
        if (existingProduct == null)
            return NotFound(new { message = $"Product with ID {id} not found." });

        if (existingProduct.ArticleNumber != updateDto.ArticleNumber &&
            !await IsArticleNumberUniqueAsync(updateDto.ArticleNumber, id))
        {
            return Conflict(new { message = "ArticleNumber already exists." });
        }

        existingProduct.ArticleNumber = updateDto.ArticleNumber;
        existingProduct.Name = updateDto.Name;
        existingProduct.Description = updateDto.Description;
        existingProduct.Price = updateDto.Price;
        existingProduct.Category = updateDto.Category;

        if (!string.IsNullOrWhiteSpace(updateDto.ImageUrl))
            existingProduct.ImageUrl = updateDto.ImageUrl;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchProduct(int id, [FromBody] PatchProductDto patchDto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found." });

        if (patchDto.ArticleNumber != null)
        {
            if (product.ArticleNumber != patchDto.ArticleNumber &&
                !await IsArticleNumberUniqueAsync(patchDto.ArticleNumber, id))
            {
                return Conflict(new { message = "ArticleNumber already exists." });
            }
            product.ArticleNumber = patchDto.ArticleNumber;
        }

        if (patchDto.Name != null) product.Name = patchDto.Name;
        if (patchDto.Description != null) product.Description = patchDto.Description;
        if (patchDto.Price.HasValue) product.Price = patchDto.Price.Value;
        if (patchDto.Category != null) product.Category = patchDto.Category;
        if (patchDto.ImageUrl != null) product.ImageUrl = patchDto.ImageUrl;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = $"Product with ID {id} not found." });

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _dbContext.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("articlenumber/{articleNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetProductByArticleNumber(string articleNumber)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ArticleNumber == articleNumber);

        if (product == null)
            return NotFound(new { message = $"Product with article number {articleNumber} not found." });

        return Ok(product);
    }

    // --- Private Helpers ---

    private bool ProductExists(int id)
    {
        return _dbContext.Products.Any(e => e.Id == id);
    }

    /// <summary>
    /// Checks if an article number is unique across the database.
    /// Optionally excludes a specific Product ID from the check (useful for PUT/PATCH).
    /// </summary>
    private async Task<bool> IsArticleNumberUniqueAsync(string articleNumber, int? excludeId = null)
    {
        var query = _dbContext.Products.Where(p => p.ArticleNumber == articleNumber);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return !await query.AnyAsync();
    }
}