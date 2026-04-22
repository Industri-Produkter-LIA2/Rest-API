using IPShop.Api.Data;
using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Rewrite;
using IPShop.Api.Dtos;
using IPShop.Api.Models.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IPShop API", Version = "v1" });
});

builder.Services.AddDbContext<IPShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IFileService, FileService>();

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

app.UseCors("AllowFrontend");

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
        // Changed it into a capture group to avoid repeating the code. If you add a new page remember to include it in the list.
        .AddRewrite(@"^(products|login|register|admin)/?$", "src/pages/$1.html", skipRemainingRules: true)
        .AddRewrite(@"^product-details(?:/(\d+))?/?$", "src/pages/product-details.html?id=$1", skipRemainingRules: true);
    app.UseRewriter(rewriteOptions);

    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });
}


//app.UseHttpsRedirection();

app.MapControllers();

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

// Hardcoded admin for now, VERY OBVIOUSLY DO NOT KEEP when this enters production,
// this is entirely just for testing purposes, so that everyone can log in as an admin 
// and try out the admin features without having to manually seed an admin.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IPShopDbContext>();

    if (!db.Accounts.Any(a => a.Role == Roles.Admin))
    {
        var admin = new Account
        {
            Email = "admin@test.com",
            Username = "admin",
            Password = "admin123", // Again, shouldn't be plain text, but this is still testing.
            Role = Roles.Admin,
            IsApproved = true
        };

        db.Accounts.Add(admin);
        db.SaveChanges();
    }
}

app.Run();