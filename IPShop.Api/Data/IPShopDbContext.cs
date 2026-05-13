using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IPShop.Api.Data;

public class IPShopDbContext(DbContextOptions<IPShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderNotification> OrderNotifications => Set<OrderNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.ArticleNumber).IsUnique();
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        // Ensure CartItems are deleted if their parent Cart is deleted
        modelBuilder.Entity<CartItem>(entity =>
        {
            // Define the Primary Key
            entity.HasKey(e => e.Id);

            // Map CartItem -> Cart (One-to-Many)
            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.Items)           // Points to your 'Items' list in Cart.cs
                  .HasForeignKey(e => e.CartId)     // The Guid Foreign Key
                  .OnDelete(DeleteBehavior.Cascade); // Deleting a Cart deletes its Items

            // Map CartItem -> Product (One-to-Many)
            entity.HasOne(e => e.Product)
                  .WithMany()                       // Empty because Product.cs doesn't have a list of CartItems
                  .HasForeignKey(e => e.ProductId)  // The int Foreign Key
                  .OnDelete(DeleteBehavior.Restrict); // Prevents deleting a Product if it's in someone's cart
        });
    }
}
