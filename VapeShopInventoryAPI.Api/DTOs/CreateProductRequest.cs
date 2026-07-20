public class CreateProductRequest
{
    public required string Name {get; set;}
    public required string Sku {get; set;}
    public decimal Price {get; set;}
    public int StockQuantity {get; set;}
    public required string Category {get; set;}
}