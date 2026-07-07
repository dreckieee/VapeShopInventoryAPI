public class SaleItem
{
    public int SaleId {get; private set;}
    public Sale Sale {get; private set;} = null!;
    public Product Product {get; private set;} = null!;
    public int Id {get; private set;}
    public int ProductId {get; private set;}
    public int Quantity {get; private set;}
    public decimal UnitPriceAtSale {get; private set;}
    public SaleItem (int productId, int quantity, decimal unitPriceAtSale)
    {
        GuardSaleItem(productId, quantity, unitPriceAtSale);
        ProductId = productId;
        Quantity = quantity;
        UnitPriceAtSale = unitPriceAtSale;
    }

    private static void GuardSaleItem(int productId, int quantity, decimal unitPriceAtSale)
    {
        if (productId <= 0)
        {
            throw new ArgumentException("Product Id cannot be zero(0) or below", nameof(productId));
        }
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity cannot be zero(0) or below.", nameof(quantity));
        }
        if (unitPriceAtSale <= 0)
        {
            throw new ArgumentException("Unit Price cannot be zero(0) or below.", nameof(unitPriceAtSale));
        }
    }



}