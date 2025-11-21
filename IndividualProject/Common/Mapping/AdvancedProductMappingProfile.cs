using AutoMapper;

namespace Week4.Common.Mapping;

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
public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
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

public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product src, ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        return src.Price.ToString("C2"); // Example: $199.99
    }
}

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
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

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
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

public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
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

public class ConditionalPriceResolver : IValueResolver<Product, ProductProfileDto, decimal>
{
    public decimal Resolve(Product src, ProductProfileDto dest, decimal destMember, ResolutionContext context)
    {
        // Home category gets 10% discount
        return src.Category == ProductCategory.Home
            ? Math.Round(src.Price * 0.9m, 2)
            : src.Price;
    }
}
