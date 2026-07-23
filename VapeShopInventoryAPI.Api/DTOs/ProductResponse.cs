public record ProductResponse
{
    public int Id {get; init;}
    public string Name {get; init;} = string.Empty;
    public string Sku {get; init;} = string.Empty;
    public decimal Price {get; init;}
    public int StockQuantity {get; init;}
    public string Category {get; init;} = string.Empty;
    public DateTime CreatedAt {get; init;}

    public static ProductResponse FromProduct(Product product) => new()
    {
        Id = product.Id, 
        Name = product.Name, 
        Sku = product.Sku, 
        Price = product.Price, 
        StockQuantity = product.StockQuantity, 
        Category = product.Category, 
        CreatedAt = product.CreatedAt
    };
}