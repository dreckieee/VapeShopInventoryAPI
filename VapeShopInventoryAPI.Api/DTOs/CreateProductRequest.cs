namespace VapeShopInventoryAPI.Api.DTOs;
public class CreateProductRequest
{
    public required string Name {get; set;}
    public required string Sku {get; set;}
    public required decimal Price {get; set;}
    public required int StockQuantity {get; set;}
    public required int LowStockLevel {get; set;}
    public required string Category {get; set;}
    
}