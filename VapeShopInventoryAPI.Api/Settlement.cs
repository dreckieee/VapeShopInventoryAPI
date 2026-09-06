namespace VapeShopInventoryAPI.Api;
public class Settlement
{
    public int Id {get; private set;}
    public int? SaleId {get; private set;}
    public int? ExpenseId {get; private set;}
    public decimal Amount {get; private set;}
    public PaymentMethod PaymentMethod {get; private set;}
    public string? PaymentNote {get; private set;}
    public DateTime Date {get; private set;}
    public Settlement(int? saleId, int? expenseId, decimal amount, PaymentMethod paymentMethod, string? paymentNote, DateTime date)
    {
        GuardSettlement(saleId, expenseId, amount, paymentMethod, date);
        SaleId = saleId;
        ExpenseId = expenseId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentNote = paymentNote;
        Date = date;
    }
    private static void GuardSettlement(int? saleId, int? expenseId, decimal amount, PaymentMethod paymentMethod, DateTime date)
    {
        if (saleId == null && expenseId == null)
        {
            throw new ArgumentException("Sale Id and Expense Id cannot be both null.");
        }
        if (saleId != null && expenseId != null)
        {
            throw new ArgumentException("Sale Id and Expense Id cannot be both non-null.");
        }
        if (amount <= 0)
        {
            throw new ArgumentException("Amount cannot be zero (0) or less", nameof(amount));
        }
        if (paymentMethod != PaymentMethod.Cash && paymentMethod != PaymentMethod.DigitalPayment)
        {
            throw new ArgumentException("Payment Method must only be Cash or Digital Payment", nameof(paymentMethod));
        }
        if (date > DateTime.Now)
        {
            throw new ArgumentException("Date of settlement cannot be in the future", nameof(date));
        }
    }
}