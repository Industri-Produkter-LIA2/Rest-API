using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<IPShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.MapGet("/", (HttpContext http) =>
{
    if (frontendAvailable)
    {
        return Results.Redirect("/index.html");
    }

    return Results.Text("IPShop API is running");
})
.WithName("Health")
.WithOpenApi();

app.MapGet("/products", async (IPShopDbContext dbContext) =>
{
    var products = await dbContext.Products
        .OrderBy(p => p.Name)
        .ToListAsync();

    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/products/{id:int}", async (int id, IPShopDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProductById")
.WithOpenApi();

app.MapPost("/products", async (Product product, IPShopDbContext dbContext) =>
{
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

app.Run();
