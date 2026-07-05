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

    public void Edit(string newExpenseDescription, decimal newExpenseAmount, string newExpenseCategory)
    {
        GuardExpense(newExpenseDescription, newExpenseAmount, newExpenseCategory);
        Description = newExpenseDescription;
        Amount = newExpenseAmount;
        Category = newExpenseCategory;
    }
    private static void GuardExpense(string expenseDescription, decimal expenseAmount, string expenseCategory)
    {
        if (expenseDescription == null)
        {
            throw new ArgumentNullException(nameof(expenseDescription), "Description cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(expenseDescription))
        {
            throw new ArgumentException("Description cannot be empty", nameof(expenseDescription));
        }

        if (expenseAmount <= 0)
        {
            throw new ArgumentException("Expense amount cannot be 0 or below.", nameof(expenseAmount));
        }

        if (expenseCategory == null)
        {
            throw new ArgumentNullException(nameof(expenseCategory), "Category cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(expenseCategory))
        {
            throw new ArgumentException("Category cannot be empty", nameof(expenseCategory));
        }
    }
}