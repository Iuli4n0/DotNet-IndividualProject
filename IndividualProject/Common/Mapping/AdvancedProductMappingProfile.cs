using AutoMapper;
using Week4.Features.Products;

namespace Week4.Common.Mapping;

/// <summary>
/// AutoMapper profile that defines advanced mapping rules between <see cref="CreateProductProfileRequest"/>,
/// <see cref="Product"/> and <see cref="ProductProfileDto"/> including conditional mappings and value resolvers.
/// </summary>
public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        // Map CreateProductProfileRequest to Product
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        
        
        CreateMap<Product, ProductProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge, opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials, opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>())
        
            // Conditional ImageUrl: null for Home category
            .ForMember(dest => dest.ImageUrl,
                opt => opt.Condition(src => src.Category != ProductCategory.Home))

            // Conditional Price: 10% discount for Home category
            .ForMember(dest => dest.Price,
                opt => opt.MapFrom<ConditionalPriceResolver>());
    }

}
/// <summary>
/// Resolves a friendly display name for a product category.
/// </summary>
public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
    /// <summary>
    /// Returns a human-friendly category name for display (e.g. "Electronics & Technology").
    /// </summary>
    /// <param name="src">Source <see cref="Product"/> instance.</param>
    /// <param name="dest">Destination DTO (may be null during mapping).</param>
    /// <param name="destMember">Destination member (ignored).</param>
    /// <param name="context">Resolution context from AutoMapper.</param>
    /// <returns>A display name for the product category.</returns>
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        return src.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing => "Clothing & Fashion",
            ProductCategory.Books => "Books & Media",
            ProductCategory.Home => "Home & Garden",
            _ => "Uncategorized"
        };
    }
}

/// <summary>
/// Formats a numeric product price into a display string including currency symbol.
/// </summary>
public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    /// <summary>
    /// Formats the product price as a string (e.g. "$199.99").
    /// </summary>
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        return $"${src.Price:F2}"; // Example: $199.99
    }
}

/// <summary>
/// Resolves a human readable age string for a product based on its release date.
/// </summary>
public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    /// <summary>
    /// Returns a friendly age description such as "New Release", "2 months old", or "Vintage".
    /// </summary>
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        var days = (DateTime.UtcNow - src.ReleaseDate).TotalDays;

        return days switch
        {
            < 30    => "New Release",
            < 365   => $"{Math.Floor(days / 30)} months old",
            < 1825  => $"{Math.Floor(days / 365)} years old",
            1825    => "Classic",
            _       => "Vintage"
        };
    }
}

/// <summary>
/// Produces initials from the brand name (e.g. "Sony Electronics" -> "SE").
/// </summary>
public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    /// <summary>
    /// Extracts initials from the brand name, returning "?" when brand is empty.
    /// </summary>
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(src.Brand))
            return "?";

        var words = src.Brand.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
            return words[0][0].ToString().ToUpper();

        return $"{words[0][0]}{words[^1][0]}".ToUpper();
    }
}

/// <summary>
/// Returns a user-friendly availability status based on stock and availability flags.
/// </summary>
public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    /// <summary>
    /// Resolves availability to strings such as "In Stock", "Limited Stock", or "Out of Stock".
    /// </summary>
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        if (!src.IsAvailable)
            return "Out of Stock";

        return src.StockQuantity switch
        {
            <= 0 => "Unavailable",
            1 => "Last Item",
            <= 5 => "Limited Stock",
            _ => "In Stock"
        };
    }
}

/// <summary>
/// Applies conditional pricing rules (e.g. discounts) based on product category.
/// </summary>
public class ConditionalPriceResolver : IValueResolver<Product, ProductProfileDto, decimal>
{
    /// <summary>
    /// Returns the adjusted price. Currently applies a 10% discount for <see cref="ProductCategory.Home"/>.
    /// </summary>
    public decimal Resolve(Product src, ProductProfileDto dest, decimal destMember, ResolutionContext context)
    {
        // Home category gets 10% discount
        return src.Category == ProductCategory.Home
            ? Math.Round(src.Price * 0.9m, 2)
            : src.Price;
    }
}
