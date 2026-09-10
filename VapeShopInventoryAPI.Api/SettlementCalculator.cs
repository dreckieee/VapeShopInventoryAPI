using Microsoft.EntityFrameworkCore;
namespace VapeShopInventoryAPI.Api;
public static class SettlementCalculator
{
    public static async Task <(decimal OutstandingBalance, decimal AlreadySettled)> CalculateSaleBalanceAsync (VapeShopInventoryDbContext context, Sale sale)
    {
        decimal alreadySettled = await context.Settlements.Where(se => se.SaleId == sale.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = sale.TotalAmount - alreadySettled;
        return (outstandingBalance, alreadySettled);
    }
    public static async Task <(decimal OutstandingBalance, decimal AlreadySettled)> CalculateExpenseBalanceAsync (VapeShopInventoryDbContext context, Expense expense)
    {
        decimal alreadySettled = await context.Settlements.Where(se => se.ExpenseId == expense.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = expense.Amount - alreadySettled;
        return (outstandingBalance, alreadySettled);
    }
}