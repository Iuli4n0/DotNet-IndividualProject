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

            //TODO
            /*
            RuleFor(x => x).MustAsync(PassBusinessRules)
                .WithMessage("Business rules validation failed.");
                */
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
    
    
}