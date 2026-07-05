public class Expense
{
    public int Id {get; private set;}
    public string Description {get; private set;}
    public decimal Amount {get; private set;}
    public string Category {get; private set;}
    public DateTime Date {get; private set;}
    public Expense (string description, decimal amount, string category)
    {
        GuardExpense(description, amount, category);
        Date = DateTime.UtcNow;
        Description = description;
        Amount = amount;
        Category = category;
    }

    public void Edit(Expense newExpense)
    {
        GuardExpense(newExpense.Description, newExpense.Amount, newExpense.Category);
        Description = newExpense.Description;
        Amount = newExpense.Amount;
        Category = newExpense.Category;
    }
    private static void GuardExpense(string expenseDescription, decimal expenseAmount, string expenseCategory)
    {
        if (string.IsNullOrWhiteSpace(expenseDescription))
        {
            throw new ArgumentException("Invalid Description.", nameof(expenseDescription));
        }
        if (expenseAmount < 0)
        {
            throw new ArgumentException("Expense amount cannot be negative.", nameof(expenseAmount));
        }
        if (string.IsNullOrWhiteSpace(expenseCategory))
        {
            throw new ArgumentException("Invalid Category.", nameof(expenseCategory));
        }
    }
}