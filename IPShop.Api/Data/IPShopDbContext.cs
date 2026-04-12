using IPShop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IPShop.Api.Data;

public class IPShopDbContext(DbContextOptions<IPShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.ArticleNumber).IsUnique();
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
