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
            // Generate OperationId
            var operationId = Guid.NewGuid().ToString("N")[..8];

            // Track total duration
            var overallWatch = Stopwatch.StartNew();

            // LOGGING SCOPE
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationId"] = operationId,
                ["SKU"] = request.SKU,
                ["Category"] = request.Category.ToString()
            });

            try
            {
                _logger.LogInformation(
                    new EventId(ProductLogEvents.ProductCreationStarted),
                    "[{OperationId}] Product creation started: {Name} ({Brand}) | SKU: {SKU} | Category: {Category}",
                    operationId, request.Name, request.Brand, request.SKU, request.Category);

                // VALIDATION 
                var validationWatch = Stopwatch.StartNew();

                _logger.LogInformation(new EventId(ProductLogEvents.SKUValidationPerformed),
                    "[{OperationId}] Validating SKU: {SKU}", operationId, request.SKU);

                bool skuExists = await _context.Products.AnyAsync(
                    p => p.SKU == request.SKU, cancellationToken);

                if (skuExists)
                {
                    _logger.LogError(new EventId(ProductLogEvents.ProductValidationFailed),
                        "[{OperationId}] SKU '{SKU}' already exists in system.",
                        operationId, request.SKU);

                    // ERROR METRICS
                    var errMetrics = new LoggingExtensions.ProductCreationMetrics(
                        OperationId: operationId,
                        ProductName: request.Name,
                        SKU: request.SKU,
                        Category: request.Category,
                        ValidationDuration: validationWatch.Elapsed,
                        DatabaseSaveDuration: TimeSpan.Zero,
                        TotalDuration: overallWatch.Elapsed,
                        Success: false,
                        ErrorReason: "Duplicate SKU");

                    _logger.LogProductCreationMetrics(errMetrics);

                    throw new InvalidOperationException(
                        $"Product with SKU '{request.SKU}' already exists.");
                }

                _logger.LogInformation(new EventId(ProductLogEvents.StockValidationPerformed),
                    "[{OperationId}] Stock validation performed for {Name}.",
                    operationId, request.Name);

                validationWatch.Stop();

                // MAPPING 
                var product = _mapper.Map<Product>(request);

                // DATABASE SAVE
                var dbWatch = Stopwatch.StartNew();

                _logger.LogInformation(
                    new EventId(ProductLogEvents.DatabaseOperationStarted),
                    "[{OperationId}] Starting database save for {Name}...",
                    operationId, request.Name);

                await _context.Products.AddAsync(product, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                dbWatch.Stop();

                _logger.LogInformation(
                    new EventId(ProductLogEvents.DatabaseOperationCompleted),
                    "[{OperationId}] Database save completed for product {Id}.",
                    operationId, product.Id);

                // CACHE INVALIDATION 
                _logger.LogInformation(
                    new EventId(ProductLogEvents.CacheOperationPerformed),
                    "[{OperationId}] Updating cache key 'all_products'.",
                    operationId);

                _cache.Remove("all_products");

                overallWatch.Stop();

                // DTO MAPPING 
                var productDto = _mapper.Map<ProductProfileDto>(product);

                // SUCCESS METRICS 
                var metrics = new LoggingExtensions.ProductCreationMetrics(
                    OperationId: operationId,
                    ProductName: product.Name,
                    SKU: product.SKU,
                    Category: product.Category,
                    ValidationDuration: validationWatch.Elapsed,
                    DatabaseSaveDuration: dbWatch.Elapsed,
                    TotalDuration: overallWatch.Elapsed,
                    Success: true,
                    ErrorReason: null
                );

                _logger.LogProductCreationMetrics(metrics);

                _logger.LogInformation(
                    new EventId(ProductLogEvents.ProductCreationCompleted),
                    "[{OperationId}] Product creation completed successfully for {Name}.",
                    operationId, product.Name);

                return productDto;
            }
            catch (Exception ex)
            {
                overallWatch.Stop();

                // ERROR METRICS
                var errorMetrics = new LoggingExtensions.ProductCreationMetrics(
                    OperationId: operationId,
                    ProductName: request.Name,
                    SKU: request.SKU,
                    Category: request.Category,
                    ValidationDuration: TimeSpan.Zero,
                    DatabaseSaveDuration: TimeSpan.Zero,
                    TotalDuration: overallWatch.Elapsed,
                    Success: false,
                    ErrorReason: ex.Message
                );

                _logger.LogProductCreationMetrics(errorMetrics);

                _logger.LogError(
                    new EventId(ProductLogEvents.ProductValidationFailed),
                    ex,
                    "[{OperationId}] Error during product creation. Name={Name}, Brand={Brand}, SKU={SKU}, Category={Category}",
                    operationId, request.Name, request.Brand, request.SKU, request.Category);

                // Re-throw for global exception handler 
                throw;
            }
        }
    }
}
