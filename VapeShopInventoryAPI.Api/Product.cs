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
        GuardProduct(name, sku, price, stockQuantity, category);
        CreatedAt = DateTime.UtcNow;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        Category = category;
    }

    public void Edit(Product newProduct)
    {
        GuardProduct(newProduct.Name, newProduct.Sku, newProduct.Price, newProduct.StockQuantity, newProduct.Category);

        Name = newProduct.Name;
        Sku = newProduct.Sku;
        Price = newProduct.Price;
        StockQuantity = newProduct.StockQuantity;
        Category = newProduct.Category;
    }
    private static void GuardProduct(string productName, string productSku, decimal productPrice, int productStockQuantity, string productCategory)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Invalid Name.", nameof(productName));
        }
        if (string.IsNullOrWhiteSpace(productSku))
        {
            throw new ArgumentException("Invalid Sku.", nameof(productSku));
        }
        if (productPrice < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(productPrice));
        }
        if (productStockQuantity < 0)
        {
            throw new ArgumentException("Stock cannot be negative.", nameof(productStockQuantity));
        }
        if (string.IsNullOrWhiteSpace(productCategory))
        {
            throw new ArgumentException("Invalid Category.", nameof(productCategory));
        }
    }
}