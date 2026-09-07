namespace VapeShopInventoryAPI.Api.DTOs;
public record ProductResponse
{
    public required int Id {get; init;}
    public required string Name {get; init;} = string.Empty;
    public required string Sku {get; init;} = string.Empty;
    public required decimal Price {get; init;}
    public required int StockQuantity {get; init;}
    public required int LowStockLevel {get; init;}
    public required bool IsLowStock {get; init;}
    public required string Category {get; init;} = string.Empty;
    public required DateTime CreatedAt {get; init;}

    public static ProductResponse FromProduct(Product product) => new()
    {
        Id = product.Id, 
        Name = product.Name, 
        Sku = product.Sku, 
        Price = product.Price, 
        StockQuantity = product.StockQuantity, 
        LowStockLevel = product.LowStockLevel,
        IsLowStock = product.IsLowStock,
        Category = product.Category, 
        CreatedAt = product.CreatedAt
    };
}