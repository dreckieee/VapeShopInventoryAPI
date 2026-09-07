namespace VapeShopInventoryAPI.Api.DTOs;
public record ExpenseResponse
{
    public required int Id {get; init;}
    public required PaymentMethod PaymentMethod {get; init;}
    public string? PaymentNote {get; init;}
    public required string Description {get; init;}
    public required decimal Amount {get; init;}
    public required string Category {get; init;}
    public required DateTime Date {get; init;}
    public required DateTime CreatedAt {get; init;}
    public static ExpenseResponse FromExpense(Expense expense) => new()
    {
        Id = expense.Id, 
        PaymentMethod = expense.PaymentMethod,
        PaymentNote = expense.PaymentNote,
        Description = expense.Description,
        Amount = expense.Amount,
        Category = expense.Category,
        Date = expense.Date,
        CreatedAt = expense.CreatedAt
    };
}