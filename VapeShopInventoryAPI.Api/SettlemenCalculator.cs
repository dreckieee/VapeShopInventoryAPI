using Microsoft.EntityFrameworkCore;
namespace VapeShopInventoryAPI.Api;
public static class SettlementCalculator
{
    public static async Task <(decimal OutstandingBalance, decimal AmountSettled)> CalculateSaleBalance (VapeShopInventoryDbContext context, Sale sale)
    {
        decimal amountSettled = await context.Settlements.Where(se => se.SaleId == sale.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = sale.TotalAmount - amountSettled;
        return (outstandingBalance, amountSettled);
    }
    public static async Task <(decimal OutstandingBalance, decimal AmountSettled)> CalculateExpenseBalance (VapeShopInventoryDbContext context, Expense expense)
    {
        decimal amountSettled = await context.Settlements.Where(se => se.ExpenseId == expense.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = expense.Amount - amountSettled;
        return (outstandingBalance, amountSettled);
    }
}