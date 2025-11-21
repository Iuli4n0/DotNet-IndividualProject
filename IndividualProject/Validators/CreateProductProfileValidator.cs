using System.Data;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Week4.Persistence;

namespace Week4.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    public CreateProductProfileValidator(ApplicationContext context, ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;
        
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name cannot be empty.")
                .Length(1, 200)
                .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
                .MustAsync(BeUniqueName).WithMessage("Product name already exists for this brand.");
            
            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("Brand cannot be empty.")
                .Length(2, 100)
                .Must(BeValidBrandName).WithMessage("Brand name contains invalid characters.");
            
            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU cannot be empty.")
                .Must(BeValidSKU).WithMessage("Invalid SKU format.")
                .MustAsync(BeUniqueSKU).WithMessage("SKU already exists in the system.");
            
            RuleFor(x => x.Category)
                .IsInEnum().WithMessage("Invalid product category.");
            
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .LessThan(10000);
            
            RuleFor(x => x.ReleaseDate)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Release date cannot be in the future.")
                .GreaterThan(new DateTime(1900, 1, 1)).WithMessage("Release date must be after 1900.");
            
            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(100000);
            
            When(x => !string.IsNullOrEmpty(x.ImageUrl), () =>
            {
                RuleFor(x => x.ImageUrl!)
                    .Must(BeValidImageUrl).WithMessage("Invalid image URL format.");
            });

            RuleFor(x => x)
                .MustAsync(PassBusinessRules)
                .WithMessage("Business rules validation failed.");
            
            
            // CONDITIONAL VALIDATION
            // ELECTRONICS
            When(x => x.Category == ProductCategory.Electronics, () =>
            {
                RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(50m)
                    .WithMessage("Electronics must have a minimum price of $50.");

                RuleFor(x => x.Name)
                    .Must(ContainTechnologyKeywords)
                    .WithMessage("Electronics product names must include technology-related words.");

                RuleFor(x => x.ReleaseDate)
                    .GreaterThan(DateTime.UtcNow.AddYears(-5))
                    .WithMessage("Electronics must be released within the last 5 years.");
            });

            // HOME
            When(x => x.Category == ProductCategory.Home, () =>
            {
                RuleFor(x => x.Price)
                    .LessThanOrEqualTo(200m)
                    .WithMessage("Home products must not exceed a price of $200.");

                RuleFor(x => x.Name)
                    .Must(BeAppropriateForHome)
                    .WithMessage("Home product name contains inappropriate or restricted terms.");
            });

            // CLOTHING
            When(x => x.Category == ProductCategory.Clothing, () =>
            {
                RuleFor(x => x.Brand)
                    .MinimumLength(3)
                    .WithMessage("Clothing brand names must be at least 3 characters long.");
            });

            // CROSS-FIELD: Expensive products must have limited stock
            RuleFor(x => x)
                .Must(x => x.Price <= 100m || x.StockQuantity <= 20)
                .WithMessage("Products costing more than $100 must not exceed 20 units in stock.");

    }
    private bool BeValidName(string name)
    {
        string[] bannedWords = { "fake", "test", "invalid" };
        return !bannedWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest req, string name, CancellationToken token)
    {
        bool exists = await _context.Products.AnyAsync(
            p => p.Name == name && p.Brand == req.Brand, token);

        if (exists)
            _logger.LogWarning("Duplicate name '{Name}' found for brand '{Brand}'", name, req.Brand);

        return !exists;
    }

    private bool BeValidBrandName(string brand)
    {
        return Regex.IsMatch(brand, @"^[A-Za-z0-9\s\-\.'’]+$");
    }

    private bool BeValidSKU(string sku)
    {
        return Regex.IsMatch(sku, @"^[0-9]+$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken token)
    {
        bool exists = await _context.Products.AnyAsync(p => p.SKU == sku, token);

        if (exists)
            _logger.LogWarning("Duplicate SKU detected: {SKU}", sku);

        return !exists;
    }

    private bool BeValidImageUrl(string url)
    {
        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
               allowedExt.Any(e => uri.AbsolutePath.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }
    
    private async Task<bool> PassBusinessRules(CreateProductProfileRequest req, CancellationToken token)
    {
        // RULE 1 — Daily limit
        int countToday = await _context.Products
            .CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date, token);

        if (countToday >= 500)
        {
            _logger.LogWarning("Business Rule Failed: Daily creation limit reached (500).");
            return false;
        }

        // RULE 2 — Electronics must be at least $50
        if (req.Category == ProductCategory.Electronics && req.Price < 50m)
        {
            _logger.LogWarning(
                "Business Rule Failed: Electronics price too low. Price={Price}", 
                req.Price
            );
            return false;
        }

        // RULE 3 — Home category restricted words
        if (req.Category == ProductCategory.Home)
        {
            string[] restricted = { "weapon", "explosive", "illegal", "restricted" };
            if (restricted.Any(w => req.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Business Rule Failed: Home product contains restricted word. Name={Name}",
                    req.Name
                );
                return false;
            }
        }

        // RULE 4 — High-value product stock limit
        if (req.Price > 500m && req.StockQuantity > 10)
        {
            _logger.LogWarning(
                "Business Rule Failed: High-value product cannot exceed 10 items in stock. Price={Price}, Stock={Stock}",
                req.Price, req.StockQuantity
            );
            return false;
        }

        return true;
    }

    
    private bool ContainTechnologyKeywords(string name)
    {
        string[] keywords = { "tech", "smart", "digital", "AI", "gadget", "electronic", "device" };
        return keywords.Any(k => 
            name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAppropriateForHome(string name)
    {
        string[] banned = { "weapon", "explosive", "restricted", "dangerous" };
        return !banned.Any(b =>
            name.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    
    
}