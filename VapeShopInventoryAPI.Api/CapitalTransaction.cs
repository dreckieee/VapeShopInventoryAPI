namespace VapeShopInventoryAPI.Api;
public class CapitalTransaction
{
    public int Id {get; private set;}
    public CapitalTransactionType Type {get; private set;}
    public decimal Amount {get; private set;}
    public PaymentMethod PaymentMethod {get; private set;}
    public string? Note {get; private set;}
    public DateTime Date {get; private set;}
    public DateTime CreatedAt {get; private set;}
   
    public CapitalTransaction (CapitalTransactionType type, decimal amount, PaymentMethod paymentMethod, string? note, DateTime date)
    {
        GuardCapitalTransaction(type, amount, paymentMethod, date);
        Type = type;
        Amount = amount;
        PaymentMethod = paymentMethod;
        Note = note;
        Date = date;
        CreatedAt = DateTime.Now;
    }

    private static void GuardCapitalTransaction(CapitalTransactionType type, decimal amount, PaymentMethod paymentMethod, DateTime date)
    {
        if (!Enum.IsDefined(type))
        {
            string capitalTransactionTypes = string.Join(", ", Enum.GetNames<CapitalTransactionType>());
            throw new ArgumentOutOfRangeException(nameof(type), $"Type provided for capital transaction is incorrect. Choose between: {capitalTransactionTypes}");
        }
        if (amount <= 0)
        {
            throw new ArgumentException("A deposit/withdrawal amount cannot be zero (0) or below.", nameof(amount));
        }
        if (!Enum.IsDefined(paymentMethod))
        {
            string paymentMethods = $"{PaymentMethod.Cash}, {PaymentMethod.DigitalPayment}";
            throw new ArgumentOutOfRangeException(nameof(paymentMethod), $"Payment method provided for capital transaction is incorrect. Choose between: {paymentMethods}");
        }
        if (paymentMethod == PaymentMethod.Receivable || paymentMethod == PaymentMethod.Payable)
        {
            throw new ArgumentException("Payment Method for a capital transaction can only be through Cash or Digital Payment", nameof(paymentMethod));
        }
        if (date == default)
        {
            throw new ArgumentException("Date of deposit/withdrawal must be provided.", nameof(date));
        }
        if (date > DateTime.Now)
        {
            throw new ArgumentException("Date of deposit/withdrawal cannot be in the future.", nameof(date));
        }
    }
}