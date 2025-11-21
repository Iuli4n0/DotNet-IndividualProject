namespace Week4;

/// <summary>
/// Data Transfer Object representing a product profile returned to clients.
/// Contains both stored values and derived/display properties.
/// </summary>
public class ProductProfileDto
{
    /// <summary>
    /// Product unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Brand or manufacturer name.
    /// </summary>
    public string Brand { get; set; }

    /// <summary>
    /// Stock keeping unit (SKU).
    /// </summary>
    public string SKU { get; set; }

    /// <summary>
    /// Friendly display name for the product category (e.g. "Electronics & Technology").
    /// </summary>
    public string CategoryDisplayName { get; set; }

    /// <summary>
    /// Numeric price value stored for the product (after any applied discounts).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Formatted price string suitable for display (includes currency symbol and formatting).
    /// </summary>
    public string FormattedPrice { get; set; }

    /// <summary>
    /// UTC timestamp when the product was created in the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Product release date (UTC).
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Optional URL referencing the product image. Can be null if images are not provided for the category.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Availability flag indicating if the product can currently be purchased.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Quantity available in stock.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Human readable product age (e.g. "2 months old", "1 year old").
    /// </summary>
    public string ProductAge { get; set; }

    /// <summary>
    /// Combined initials of the brand for display purposes (e.g. "SE" for "Sony Electronics").
    /// </summary>
    public string BrandInitials { get; set; }

    /// <summary>
    /// User-friendly availability status string (e.g. "In Stock", "Out of Stock").
    /// </summary>
    public string AvailabilityStatus { get; set; }
}

/// <summary>
/// Request object used to create a new product profile.
/// Contains only the inputs required to construct a <see cref="Product"/>.
/// </summary>
public class CreateProductProfileRequest
{
    /// <summary>
    /// Desired product name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Brand or manufacturer name.
    /// </summary>
    public string Brand { get; set; }

    /// <summary>
    /// Stock keeping unit (SKU). Must be unique; uniqueness is validated by the handler.
    /// </summary>
    public string SKU { get; set; }

    /// <summary>
    /// Product category enum value.
    /// </summary>
    public ProductCategory Category { get; set; }

    /// <summary>
    /// Price for the product (pre-discount). Business rules/handlers may modify this value.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Release date (UTC) of the product.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Optional product image URL. Some categories may explicitly drop images during mapping.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Initial stock quantity for the product. Defaults to 1.
    /// </summary>
    public int StockQuantity { get; set; } = 1;
}