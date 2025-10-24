namespace Week4.Logging;

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