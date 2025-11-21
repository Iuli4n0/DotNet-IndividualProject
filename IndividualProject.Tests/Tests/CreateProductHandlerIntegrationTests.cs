namespace IndividualProject.Tests.Tests;

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Week4.Features.Products;
using Week4.Persistence;
using Week4.Common.Mapping;
using Week4.Common.Logging;
using Week4;
using NUnit.Framework;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase("ProductDb_" + Guid.NewGuid())
            .Options;

        _context = new ApplicationContext(dbOptions);
        _cache = new MemoryCache(new MemoryCacheOptions());

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CreateProductHandler>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AdvancedProductMappingProfile());
        }, loggerFactory);

        _mapper = mapperConfig.CreateMapper();

        _handler = new CreateProductHandler(_context, _mapper, _logger, _cache);
    }
    
    [Test]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "Smart Tech Camera",
            Brand = "Sony Electronics",
            SKU = "SONY-12345",
            Category = ProductCategory.Electronics,
            Price = 299.99m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            ImageUrl = "https://cdn/test.jpg",
            StockQuantity = 10
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.That(result.CategoryDisplayName, Is.EqualTo("Electronics & Technology"));
        Assert.That(result.BrandInitials, Is.EqualTo("SE"));
        Assert.That(result.FormattedPrice, Does.StartWith("$"));
        Assert.That(result.AvailabilityStatus, Is.EqualTo("In Stock"));
        Assert.That(result.ProductAge, Does.Contain("months old"));
    }
    
    [Test]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        // Arrange: create existing product
        _context.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Brand = "BrandX",
            SKU = "DUP-123",
            Category = ProductCategory.Books,
            Price = 25m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1),
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Request with duplicate SKU
        var request = new CreateProductProfileRequest
        {
            Name = "Another Product",
            Brand = "BrandY",
            SKU = "DUP-123", // duplicate
            Category = ProductCategory.Books,
            Price = 20m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            StockQuantity = 3
        };

        // Act + Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(request, default));

        Assert.That(ex!.Message, Does.Contain("already exists"));
    }
    
    [Test]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "Garden Chair",
            Brand = "HomeBrand",
            SKU = "HOME-001",
            Category = ProductCategory.Home,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-1),
            ImageUrl = "https://fakeimg.com/chair.jpg",
            StockQuantity = 3
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.That(result.CategoryDisplayName, Is.EqualTo("Home & Garden"));

        // Price with 10% discount
        Assert.That(result.Price, Is.EqualTo(90m));

        // ImageUrl must be null for Home category
        Assert.That(result.ImageUrl, Is.Null);
    }
    

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
    
    
}
