using System.ComponentModel.DataAnnotations;

namespace Week4.Features.Products;

/// <summary>
/// Represents a product stored in the application database.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier for the product.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The UTC timestamp when the product was created. Set to <see cref="DateTime.UtcNow"/> by default.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // (opțional, dar recomandat)

    /// <summary>
    /// The UTC timestamp when the product was last updated. Null if the product has never been updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Human readable product name. Maximum length and validation are enforced by higher layers/validators.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Manufacturer or brand name of the product.
    /// </summary>
    public required string Brand { get; set; }

    /// <summary>
    /// Stock keeping unit (SKU). Expected to be unique across products.
    /// </summary>
    public required string SKU { get; set; }

    /// <summary>
    /// The product category.
    /// </summary>
    public ProductCategory Category { get; set; }

    /// <summary>
    /// The product price in the application's currency. Use decimal for financial values.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The product release date (UTC). Used for age calculations and display.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Optional URL pointing to a product image. Can be null for categories that do not expose images.
    /// </summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Indicates whether the product is currently available for purchase.
    /// This may be derived from <see cref="StockQuantity"/> or business logic.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// The number of items available in stock.
    /// </summary>
    public int StockQuantity { get; set; }
}