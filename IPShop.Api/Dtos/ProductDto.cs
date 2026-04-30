using System.ComponentModel.DataAnnotations;

namespace IPShop.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class CreateProductDto
{
    [Required]
    [MaxLength(50)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    [Url]
    public string? ImageUrl { get; set; }
}
public class UpdateProductDto
{
    [Required]
    [MaxLength(50)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    [Url]
    public string? ImageUrl { get; set; }
}
// DTOs for Patch operations
public class PatchProductDto
{
    [MaxLength(50)]
    public string? ArticleNumber { get; set; }

    [MaxLength(150)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    [Url]
    public string? ImageUrl { get; set; }
}