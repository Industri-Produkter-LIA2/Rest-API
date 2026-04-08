// Controllers/ProductsController.cs (Extended Version)
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
    private readonly IFileService _fileService;

    public ProductsController(IPShopDbContext dbContext, IFileService fileService)
    {
        _dbContext = dbContext;
        _fileService = fileService;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
        [FromQuery] string? articleNumber,
        [FromQuery] string? name,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            return BadRequest(new { message = "minPrice cannot be greater than maxPrice." });
        }

        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(articleNumber))
        {
            var value = articleNumber.Trim();
            query = query.Where(p => p.ArticleNumber.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var value = name.Trim();
            query = query.Where(p => p.Name.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var value = category.Trim();
            query = query.Where(p => p.Category.Contains(value));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

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

    // GET: api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromForm] CreateProductDto createDto)
    {
        var articleExists = await _dbContext.Products
            .AnyAsync(p => p.ArticleNumber == createDto.ArticleNumber);

        if (articleExists)
        {
            return Conflict(new { message = "ArticleNumber already exists." });
        }

        var product = new Product
        {
            ArticleNumber = createDto.ArticleNumber,
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Category = createDto.Category
        };

        // Handle image upload if file is provided
        if (createDto.ImageFile != null)
        {
            try
            {
                product.ImageUrl = await _fileService.UploadFileAsync(createDto.ImageFile);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        else
        {
            product.ImageUrl = "https://via.placeholder.com/500x500?text=No+Image";
        }

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // POST: api/products (JSON version without file upload)
    [HttpPost("json")]
    public async Task<ActionResult<Product>> CreateProductJson([FromBody] Product product)
    {
        var articleExists = await _dbContext.Products
            .AnyAsync(p => p.ArticleNumber == product.ArticleNumber);

        if (articleExists)
        {
            return Conflict(new { message = "ArticleNumber already exists." });
        }

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // PUT: api/products/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto updateDto)
    {
        var existingProduct = await _dbContext.Products.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        // Check if article number is changed and if new one already exists
        if (existingProduct.ArticleNumber != updateDto.ArticleNumber)
        {
            var articleExists = await _dbContext.Products
                .AnyAsync(p => p.ArticleNumber == updateDto.ArticleNumber && p.Id != id);

            if (articleExists)
            {
                return Conflict(new { message = "ArticleNumber already exists." });
            }
        }

        var oldImageUrl = existingProduct.ImageUrl;

        // Update properties
        existingProduct.ArticleNumber = updateDto.ArticleNumber;
        existingProduct.Name = updateDto.Name;
        existingProduct.Description = updateDto.Description;
        existingProduct.Price = updateDto.Price;
        existingProduct.Category = updateDto.Category;

        // Handle image upload if new file is provided
        if (updateDto.ImageFile != null)
        {
            try
            {
                existingProduct.ImageUrl = await _fileService.UploadFileAsync(updateDto.ImageFile);

                // Delete old image if it's not the default placeholder
                if (!string.IsNullOrEmpty(oldImageUrl) && !oldImageUrl.Contains("placeholder"))
                {
                    await _fileService.DeleteFileAsync(oldImageUrl);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PUT: api/products/{id}/json (JSON version without file upload)
    [HttpPut("{id:int}/json")]
    public async Task<IActionResult> UpdateProductJson(int id, [FromBody] Product product)
    {
        if (id != product.Id)
        {
            return BadRequest(new { message = "Product ID mismatch." });
        }

        var existingProduct = await _dbContext.Products.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound();
        }

        // Check if article number is changed and if new one already exists
        if (existingProduct.ArticleNumber != product.ArticleNumber)
        {
            var articleExists = await _dbContext.Products
                .AnyAsync(p => p.ArticleNumber == product.ArticleNumber && p.Id != id);

            if (articleExists)
            {
                return Conflict(new { message = "ArticleNumber already exists." });
            }
        }

        // Update properties
        existingProduct.ArticleNumber = product.ArticleNumber;
        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Category = product.Category;
        existingProduct.ImageUrl = product.ImageUrl;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PATCH: api/products/{id}
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> PatchProduct(int id, [FromBody] PatchProductDto patchDto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        // Update only provided fields
        if (patchDto.ArticleNumber != null)
        {
            if (product.ArticleNumber != patchDto.ArticleNumber)
            {
                var articleExists = await _dbContext.Products
                    .AnyAsync(p => p.ArticleNumber == patchDto.ArticleNumber && p.Id != id);

                if (articleExists)
                {
                    return Conflict(new { message = "ArticleNumber already exists." });
                }
            }
            product.ArticleNumber = patchDto.ArticleNumber;
        }

        if (patchDto.Name != null)
            product.Name = patchDto.Name;

        if (patchDto.Description != null)
            product.Description = patchDto.Description;

        if (patchDto.Price.HasValue)
            product.Price = patchDto.Price.Value;

        if (patchDto.Category != null)
            product.Category = patchDto.Category;

        if (patchDto.ImageUrl != null)
            product.ImageUrl = patchDto.ImageUrl;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        // Delete associated image if not using placeholder
        if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.Contains("placeholder"))
        {
            await _fileService.DeleteFileAsync(product.ImageUrl);
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/products/categories
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _dbContext.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/products/articlenumber/{articleNumber}
    [HttpGet("articlenumber/{articleNumber}")]
    public async Task<ActionResult<Product>> GetProductByArticleNumber(string articleNumber)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ArticleNumber == articleNumber);

        if (product == null)
        {
            return NotFound(new { message = $"Product with article number {articleNumber} not found." });
        }

        return Ok(product);
    }

    private bool ProductExists(int id)
    {
        return _dbContext.Products.Any(e => e.Id == id);
    }
}
