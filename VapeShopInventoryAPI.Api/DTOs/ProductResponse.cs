using System.Text.Json.Serialization;
public class ProductResponse
{
    public int Id {get; private set;}
    public string Name {get; private set;}
    public string Sku {get; private set;}
    public decimal Price {get; private set;}
    public int StockQuantity {get; private set;}
    public string Category {get; private set;}
    public DateTime CreatedAt {get; private set;}
    
    [JsonConstructor]
    private ProductResponse(int id, string name, string sku, decimal price, int stockQuantity, string category, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        Category = category;
        CreatedAt = createdAt;
    }
    public static ProductResponse FromProduct(Product product) => new
    (
        product.Id, 
        product.Name, 
        product.Sku, 
        product.Price, 
        product.StockQuantity, 
        product.Category, 
        product.CreatedAt
    ); 
}