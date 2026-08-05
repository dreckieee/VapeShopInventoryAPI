namespace VapeShopInventoryAPI.Api;
public class Product
{
    public int Id {get; private set;}
    public string Name {get; private set;}
    public string Sku {get; private set;}
    public decimal Price {get; private set;}
    public int StockQuantity {get; private set;}
    public string Category {get; private set;}
    public DateTime CreatedAt {get; private set;}
    public bool IsLowStock => StockQuantity <= LowStockLevel;
    public int LowStockLevel {get; private set;}
    public Product (string name, string sku, decimal price, int stockQuantity,int lowStockLevel, string category)
    {
        GuardProduct(name, sku, price, stockQuantity, lowStockLevel, category);
        CreatedAt = DateTime.Now;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        LowStockLevel = lowStockLevel;
        Category = category;
        
    }

    public void Edit(string newProductName, string newProductSku, decimal newProductPrice, int newProductStockQuantity, int newProductLowStockLevel, string newProductCategory)
    {
        GuardProduct(newProductName, newProductSku, newProductPrice, newProductStockQuantity, newProductLowStockLevel, newProductCategory);
        Name = newProductName;
        Sku = newProductSku;
        Price = newProductPrice;
        StockQuantity = newProductStockQuantity;
        LowStockLevel = newProductLowStockLevel;
        Category = newProductCategory;
        
    }

    public void ReduceStock(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Cannot reduce stock by zero (0) or below.", nameof(amount));
        }
        if (amount > StockQuantity)
        {
            throw new InvalidOperationException("Cannot reduce stock by more than the current quantity of stock");
        }
        StockQuantity -= amount;
    }
    public void Restock(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Cannot restock by zero (0) or below.", nameof(amount));
        }
        StockQuantity += amount;
    }
    private static void GuardProduct(string productName, string productSku, decimal productPrice, int productStockQuantity, int productLowStockLevel, string productCategory)
    {
        if (productName == null)
        {
            throw new ArgumentNullException(nameof(productName), "Name cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Name cannot be empty", nameof(productName));
        }

        if (productSku == null)
        {
            throw new ArgumentNullException(nameof(productSku), "Sku cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(productSku))
        {
            throw new ArgumentException("Sku cannot be empty", nameof(productSku));
        }

        if (productPrice <= 0)
        {
            throw new ArgumentException("Price cannot be 0 or below.", nameof(productPrice));
        }

        if (productStockQuantity < 0)
        {
            throw new ArgumentException("Stock cannot be negative.", nameof(productStockQuantity));
        }
        if (productLowStockLevel < 0)
        {
            throw new ArgumentException("A level of low stock cannot be negative.", nameof(productLowStockLevel));
        }
        if (productCategory == null)
        {
            throw new ArgumentNullException(nameof(productCategory), "Category cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(productCategory))
        {
            throw new ArgumentException("Category cannot be empty", nameof(productCategory));
        }
        
    }
}