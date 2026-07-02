public class Product
{
    public int Id {get; private set;}
    public string Name {get; private set;}
    public string Sku {get; private set;}
    public decimal Price {get; private set;}
    public int StockQuantity {get; private set;}
    public string Category {get; private set;}
    public DateTime CreatedAt {get; private set;}
    public Product (string name, string sku, decimal price, int stockQuantity, string category)
    {
        if (string.IsNullOrWhiteSpace(name) )
        {
            throw new ArgumentException ("Invalid Name.", nameof(name) );
        }
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException ("Invalid Sku.", nameof(sku));
        }
        if (price < 0)
        {
            throw new ArgumentException ("Price cannot be negative.", nameof(price));
        }
        if (stockQuantity < 0)
        {
            throw new ArgumentException ("Stock cannot be negative.", nameof(stockQuantity));
        }
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException ("Invalid Category.", nameof(category));
        }
        CreatedAt = DateTime.UtcNow;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        Category = category;
    }
}