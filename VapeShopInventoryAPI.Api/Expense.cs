namespace VapeShopInventoryAPI.Api;
public class Expense
{
    public int Id {get; private set;}
    public DateTime Date {get; private set;}
    public string Description {get; private set;}
    public decimal Amount {get; private set;}
    public string Category {get; private set;}
    public PaymentMethod PaymentMethod {get; private set;}
    public string? PaymentNote {get; private set;}
    
    public DateTime CreatedAt {get; private set;}
    public const string RestockCategory = "Restock";
    public Expense (DateTime date, string description, decimal amount, string category, PaymentMethod paymentMethod, string? paymentNote)
    {
        GuardExpense(date, description, amount, category, paymentMethod);
        Date = date;
        Description = description;
        Amount = amount;
        Category = category;
        PaymentMethod = paymentMethod;
        PaymentNote = paymentNote;
        CreatedAt = DateTime.Now;
    }

    public void Edit(DateTime newExpenseDate, string newExpenseDescription, decimal newExpenseAmount, string newExpenseCategory, PaymentMethod newPaymentMethod, string? newPaymentNote)
    {
        GuardExpense(newExpenseDate, newExpenseDescription, newExpenseAmount, newExpenseCategory, newPaymentMethod);
        Date = newExpenseDate;
        Description = newExpenseDescription;
        Amount = newExpenseAmount;
        Category = newExpenseCategory;
        PaymentMethod = newPaymentMethod;
        PaymentNote = newPaymentNote;
    }
    private static void GuardExpense(DateTime expenseDate, string expenseDescription, decimal expenseAmount, string expenseCategory, PaymentMethod paymentMethod)
    {
        if (expenseDate == default)
        {
            throw new ArgumentException("Date of incurred expense must be provided.", nameof(expenseDate));
        }
        if (expenseDescription == null)
        {
            throw new ArgumentNullException(nameof(expenseDescription), "Description of expense must be provided.");
        }
        if (string.IsNullOrWhiteSpace(expenseDescription))
        {
            throw new ArgumentException("Description of expense cannot be empty", nameof(expenseDescription));
        }

        if (expenseAmount <= 0)
        {
            throw new ArgumentException("Expense amount cannot be zero (0) or below.", nameof(expenseAmount));
        }

        if (expenseCategory == null)
        {
            throw new ArgumentNullException(nameof(expenseCategory), "Category of expense must be provided.");
        }
        if (string.IsNullOrWhiteSpace(expenseCategory))
        {
            throw new ArgumentException("Category of expense cannot be empty", nameof(expenseCategory));
        }
        if (!Enum.IsDefined(paymentMethod))
        {
            string paymentMethods = string.Join(", ", Enum.GetNames<PaymentMethod>());
            throw new ArgumentOutOfRangeException(nameof(paymentMethod), $"Payment method provided for expense is incorrect. Choose between: {paymentMethods}");
        }
    }
}