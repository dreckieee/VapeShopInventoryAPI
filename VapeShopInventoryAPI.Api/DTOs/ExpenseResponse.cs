namespace VapeShopInventoryAPI.Api.DTOs;
public record ExpenseResponse
{
    public int Id {get; init;}
    public required string Description {get; init;}
    public decimal Amount {get; init;}
    public required string Category {get; init;}
    public DateTime Date {get; init;}
    public DateTime CreatedAt {get; init;}
    public static ExpenseResponse FromExpense(Expense expense) => new()
    {
        Id = expense.Id, 
        Description = expense.Description,
        Amount = expense.Amount,
        Category = expense.Category,
        Date = expense.Date,
        CreatedAt = expense.CreatedAt
    };
}