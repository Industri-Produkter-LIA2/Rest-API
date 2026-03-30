using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<IPShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()      // Allow any origin
              .AllowAnyMethod()      // Allow any HTTP method (GET, POST, etc.)
              .AllowAnyHeader();     // Allow any headers
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IPShopDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Frontend"));
var frontendAvailable = false;
if (Directory.Exists(frontendPath))
{
    frontendAvailable = true;
    var frontendProvider = new PhysicalFileProvider(frontendPath);

    // Sets new url to be just localhost:5088/products instead of localhost:5088/src/pages/products.html, because I changed the file structure in frontend and this looks cleaner
    var rewriteOptions = new RewriteOptions()
        .AddRewrite(@"^products/?$", "src/pages/products.html", skipRemainingRules: true);
    app.UseRewriter(rewriteOptions);

    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });

}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.MapGet("/", (HttpContext http) =>
{
    if (frontendAvailable)
    {
        return Results.Redirect("/products"); //Changed to redirect to /products instead of the old /index.html
    }

    return Results.Text("IPShop API is running");
})
.WithName("Health")
.WithOpenApi();

app.MapGet("/api/products", async ( // Changed to /api/products to avoid conflict with frontend route, the same applies to every product endpoint.
    string? articleNumber,
    string? name,
    string? category,
    decimal? minPrice,
    decimal? maxPrice,
    IPShopDbContext dbContext) =>
{
    if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
    {
        return Results.BadRequest(new { message = "minPrice cannot be greater than maxPrice." });
    }

    var query = dbContext.Products.AsQueryable();

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

    var products = await query
        .OrderBy(p => p.Name)
        .ToListAsync();

    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/products/{id:int}", async (int id, IPShopDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProductById")
.WithOpenApi();

app.MapPost("/api/products", async (Product product, IPShopDbContext dbContext) =>
{
    var articleExists = await dbContext.Products
        .AnyAsync(p => p.ArticleNumber == product.ArticleNumber);
    if (articleExists)
    {
        return Results.Conflict(new { message = "ArticleNumber already exists." });
    }

    dbContext.Products.Add(product);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

//shopping cart endpoints

app.MapPost("/cart", async (IPShopDbContext dbContext) =>
{
    var cart = new Cart();

    dbContext.Carts.Add(cart);
    await dbContext.SaveChangesAsync();

    return Results.Ok(cart);
})
.WithName("CreateCart")
.WithOpenApi();

app.MapGet("/cart/{cartId:guid}", async (Guid cartId, IPShopDbContext dbContext) =>
{
    var cart = await dbContext.Carts
        .Include(c => c.Items)
        .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(c => c.Id == cartId);

    return cart is null ? Results.NotFound() : Results.Ok(cart);
})
.WithName("GetCart")
.WithOpenApi();


app.MapPost("/cart/{cartId:guid}/items", async (Guid cartId, CartItem request, IPShopDbContext dbContext) =>
{
    var cart = await dbContext.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.Id == cartId);

    if (cart is null)
        return Results.NotFound();

    var existingItem = cart.Items
        .FirstOrDefault(i => i.ProductId == request.ProductId);

    if (existingItem != null)
    {
        existingItem.Quantity += request.Quantity;
    }
    else
    {
        cart.Items.Add(new CartItem
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity
        });
    }

    await dbContext.SaveChangesAsync();

    return Results.Ok(cart);
})
.WithName("AddCartItem")
.WithOpenApi();


app.MapDelete("/cart/{cartId:guid}/items/{itemId:int}", async (Guid cartId, int itemId, IPShopDbContext dbContext) =>
{
    var item = await dbContext.CartItems
        .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cartId);

    if (item is null)
        return Results.NotFound();

    dbContext.CartItems.Remove(item);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("RemoveCartItem")
.WithOpenApi();

app.MapPut("/api/products/{id:int}", async (int id, Product input, IPShopDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    var duplicateArticle = await dbContext.Products
        .AnyAsync(p => p.Id != id && p.ArticleNumber == input.ArticleNumber);
    if (duplicateArticle)
    {
        return Results.Conflict(new { message = "ArticleNumber already exists." });
    }

    product.ArticleNumber = input.ArticleNumber;
    product.Name = input.Name;
    product.Description = input.Description;
    product.Price = input.Price;
    product.Category = input.Category;

    await dbContext.SaveChangesAsync();
    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithOpenApi();

app.MapDelete("/api/products/{id:int}", async (int id, IPShopDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    dbContext.Products.Remove(product);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();

app.Run();
