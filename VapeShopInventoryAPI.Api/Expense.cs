public class Expense
{
    public int Id {get; private set;}
    public string Description {get; private set;}
    public decimal Amount {get; private set;}
    public string Category {get; private set;}
    public DateTime Date {get; private set;}
    public Expense (DateTime date, string description, decimal amount, string category)
    {
        GuardExpense(date, description, amount, category);
        Date = date;
        Description = description;
        Amount = amount;
        Category = category;
    }

    public void Edit(DateTime newExpenseDate, string newExpenseDescription, decimal newExpenseAmount, string newExpenseCategory)
    {
        GuardExpense(newExpenseDate, newExpenseDescription, newExpenseAmount, newExpenseCategory);
        Date = newExpenseDate;
        Description = newExpenseDescription;
        Amount = newExpenseAmount;
        Category = newExpenseCategory;
    }
    private static void GuardExpense(DateTime expenseDate, string expenseDescription, decimal expenseAmount, string expenseCategory)
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
    }
}