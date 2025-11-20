using Week4.Common.Logging;

namespace Week4.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            new EventId(ProductLogEvents.ProductCreationCompleted, nameof(ProductLogEvents.ProductCreationCompleted)),
            "Product metrics | OperationId: {OperationId}, Name: {ProductName}, SKU: {SKU}, Category: {Category}, " +
            "Validation: {ValidationMs}ms, DB Save: {DbMs}ms, Total: {TotalMs}ms, Success: {Success}, Error: {Error}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "None"
        );
    }
    
    public record ProductCreationMetrics(
        string OperationId,
        string ProductName,
        string SKU,
        ProductCategory Category,
        TimeSpan ValidationDuration,
        TimeSpan DatabaseSaveDuration,
        TimeSpan TotalDuration,
        bool Success,
        string? ErrorReason
    );
}