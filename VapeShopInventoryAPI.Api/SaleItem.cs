public class SaleItem
{
    public int SaleId {get; private set;}
    public Sale Sale {get; private set;} = null!;
    public Product Product {get; private set;} = null!;
    public int Id {get; private set;}
    public int ProductId {get; private set;}
    public int Quantity {get; private set;}
    public decimal UnitPriceAtSale {get; private set;}
    public int TransactionNumber {get; private set;}
    public SaleItem (int productId, int quantity, decimal unitPriceAtSale)
    {
        GuardSaleItem(productId, quantity, unitPriceAtSale);
        ProductId = productId;
        Quantity = quantity;
        UnitPriceAtSale = unitPriceAtSale;
    }

    private static void GuardSaleItem(int productId, int quantity, decimal unitPriceAtSale)
    {
        GuardSaleItemProductId(productId);
        GuardSaleItemQuantity(quantity);
        GuardSaleItemUnitPriceAtSale(unitPriceAtSale);
    }

    private static void GuardSaleItemProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentException("Product Id cannot be zero(0) or below", nameof(productId));
        }
    }

    private static void GuardSaleItemQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity cannot be zero(0) or below.", nameof(quantity));
        }
    }

    private static void GuardSaleItemUnitPriceAtSale(decimal unitPriceAtSale)
    {
        if (unitPriceAtSale <= 0)
        {
            throw new ArgumentException("Unit Price cannot be zero(0) or below.", nameof(unitPriceAtSale));
        }   
    }   
    public void CombineQuantity (int quantity)
    {
        GuardSaleItemQuantity(quantity);
        Quantity += quantity;
    }
    public void AssignTransactionNumber (int transactionNumber)
    {
        if (transactionNumber <= 0)
        {
            throw new ArgumentException("Transaction number cannot be zero(0) or below.", nameof(transactionNumber));
        }
        TransactionNumber = transactionNumber;
    }
    public void ReduceQuantity (int amount)
    {
        GuardSaleItemQuantity(amount);
        if (amount > Quantity)
        {
            throw new ArgumentException("Cannot reduce quantity of sale item by more than its current quantity.", nameof(amount));
        }
        Quantity -= amount;
    }

}