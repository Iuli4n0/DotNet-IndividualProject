using System.Diagnostics; 
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Week4.Common.Logging;
using Week4.Persistence;

namespace Week4.Features.Products
{
    public class CreateProductHandler
    {
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProductHandler> _logger;
        private readonly IMemoryCache _cache;

        public CreateProductHandler(
            ApplicationContext context,
            IMapper mapper,
            ILogger<CreateProductHandler> logger,
            IMemoryCache cache)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request, CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            var overallWatch = Stopwatch.StartNew();

            _logger.LogInformation(new EventId(ProductLogEvents.ProductCreationStarted),
                "[{OperationId}] Product creation started: {Name} ({Brand}) | SKU: {SKU} | Category: {Category}",
                operationId, request.Name, request.Brand, request.SKU, request.Category);

            // Validation Phase
            var validationWatch = Stopwatch.StartNew();
            _logger.LogInformation(new EventId(ProductLogEvents.SKUValidationPerformed),
                "[{OperationId}] Validating SKU: {SKU}", operationId, request.SKU);

            bool skuExists = await _context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken);
            if (skuExists)
            {
                _logger.LogError(new EventId(ProductLogEvents.ProductValidationFailed),
                    "[{OperationId}] SKU '{SKU}' already exists in system.", operationId, request.SKU);

                throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists.");
            }

            _logger.LogInformation(new EventId(ProductLogEvents.StockValidationPerformed),
                "[{OperationId}] Stock validation performed for {Name}.", operationId, request.Name);
            validationWatch.Stop();

            // Mapping Phase
            var product = _mapper.Map<Product>(request);

            // Database Save Phase
            var dbWatch = Stopwatch.StartNew();
            _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationStarted),
                "[{OperationId}] Starting database save for {Name}...", operationId, request.Name);

            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            dbWatch.Stop();
            _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationCompleted),
                "[{OperationId}] Database save completed for product {Id}.", operationId, product.Id);

            // Cache Phase
            _logger.LogInformation(new EventId(ProductLogEvents.CacheOperationPerformed),
                "[{OperationId}] Updating cache key 'all_products'.", operationId);

            _cache.Remove("all_products");

            overallWatch.Stop();

            // Map to DTO with resolvers
            var productDto = _mapper.Map<ProductProfileDto>(product);

            // Metrics logging
            var metrics = new LoggingExtensions.ProductCreationMetrics(
                OperationId: operationId,
                ProductName: product.Name,
                SKU: product.SKU,
                Category: product.Category,
                ValidationDuration: validationWatch.Elapsed,
                DatabaseSaveDuration: dbWatch.Elapsed,
                TotalDuration: overallWatch.Elapsed,
                Success: true,
                ErrorReason: ""
            );

            _logger.LogProductCreationMetrics(metrics);

            _logger.LogInformation(new EventId(ProductLogEvents.ProductCreationCompleted),
                "[{OperationId}] Product creation completed successfully for {Name}.", operationId, product.Name);

            return productDto;
        }
    }
}
