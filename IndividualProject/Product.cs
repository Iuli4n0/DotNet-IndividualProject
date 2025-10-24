namespace Week4;

public class Product
{
    public string Name { get; set; }
    public string Brand { get; set; }
    public string SKU { get; set; }
    public ProductCategory Category { get; set; }
    decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; } = 0;
}