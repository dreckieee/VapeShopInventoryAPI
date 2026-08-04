namespace VapeShopInventoryAPI.Api;
public class DeliveryItem
{
    public int Id {get; private set;}
    public int ExpenseId {get; private set;}
    public int ProductId {get; private set;}
    public int Quantity {get; private set;}
    public decimal UnitCost {get; private set;}
    public decimal TotalCost => Quantity * UnitCost; //shorthand for = public decimal TotalCost {get {return Quantity * UnitCost;} }
    public DateTime CreatedAt {get; private set;}
    public DeliveryItem (int expenseId, int productId, int quantity, decimal unitCost)
    {
        GuardDeliveryItem(expenseId, productId, quantity, unitCost);
        ExpenseId = expenseId;
        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
        CreatedAt = DateTime.Now;
    }
    private static void GuardDeliveryItem (int expenseId, int productId, int quantity, decimal unitCost)
    {
        if (expenseId <= 0)
        {
            throw new ArgumentException("Expense Id cannot be zero(0) or below.", nameof(expenseId));
        }
        if (productId <= 0)
        {
            throw new ArgumentException("Product Id cannot be zero(0) or below.", nameof(productId));
        }
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity for a delivery item cannot be zero(0) or below.", nameof(quantity));
        }
        if (unitCost < 0)
        {
            throw new ArgumentException("Unit cost for a delivery item cannot be below zero(0).", nameof(unitCost));
        }
    }
}