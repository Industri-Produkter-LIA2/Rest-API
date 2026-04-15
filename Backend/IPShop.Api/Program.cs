using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<IPShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.MapGet("/api/orders", async (int? customerId, IPShopDbContext dbContext) =>
{
    var query = dbContext.Orders
        .Include(o => o.Items)
        .AsQueryable();
    if (customerId.HasValue)
    {
        query = query.Where(o => o.CustomerId == customerId.Value);
    }

    var orders = await query
        .OrderByDescending(o => o.CreatedAtUtc)
        .ToListAsync();

    return Results.Ok(orders);
})
.WithName("GetOrders")
.WithOpenApi();

app.MapPost("/api/orders", async (CreateOrderRequest request, IPShopDbContext dbContext) =>
{
    if (request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "Order must contain at least one item." });
    }

    var customerExists = await dbContext.Customers.AnyAsync(c => c.Id == request.CustomerId);
    if (!customerExists)
    {
        return Results.BadRequest(new { message = "Customer does not exist." });
    }

    var requestedProductIds = request.Items
        .Select(i => i.ProductId)
        .Distinct()
        .ToList();

    var products = await dbContext.Products
        .Where(p => requestedProductIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id);

    var missingProductIds = requestedProductIds
        .Where(id => !products.ContainsKey(id))
        .ToList();

    if (missingProductIds.Count > 0)
    {
        return Results.BadRequest(new { message = "One or more products do not exist.", missingProductIds });
    }

    if (request.Items.Any(i => i.Quantity <= 0))
    {
        return Results.BadRequest(new { message = "All order item quantities must be greater than zero." });
    }

    string orderNumber;
    do
    {
        orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
    while (await dbContext.Orders.AnyAsync(o => o.OrderNumber == orderNumber));

    var order = new Order
    {
        CustomerId = request.CustomerId,
        OrderNumber = orderNumber,
        Status = "Pending",
        CreatedAtUtc = DateTime.UtcNow
    };

    var orderItems = request.Items.Select(itemRequest =>
    {
        var product = products[itemRequest.ProductId];
        var lineTotal = product.Price * itemRequest.Quantity;

        return new OrderItem
        {
            Order = order,
            ProductId = itemRequest.ProductId,
            Quantity = itemRequest.Quantity,
            UnitPrice = product.Price,
            LineTotal = lineTotal
        };
    }).ToList();

    order.TotalAmount = orderItems.Sum(i => i.LineTotal);

    dbContext.Orders.Add(order);
    dbContext.OrderItems.AddRange(orderItems);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/orders/{order.Id}", order);
})
.WithName("CreateOrder")
.WithOpenApi();

app.Run();
