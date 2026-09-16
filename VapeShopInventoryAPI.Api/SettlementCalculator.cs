using Microsoft.EntityFrameworkCore;
namespace VapeShopInventoryAPI.Api;
public static class SettlementCalculator
{
    public static async Task <(decimal OutstandingBalance, decimal AlreadySettled)> CalculateSaleBalanceAsync (VapeShopInventoryDbContext context, Sale sale)
    {
        if(sale.PaymentMethod == PaymentMethod.Cash || sale.PaymentMethod == PaymentMethod.DigitalPayment)
        {
            return (0m, sale.TotalAmount);
        }
        decimal alreadySettled = await context.Settlements.Where(se => se.SaleId == sale.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = sale.TotalAmount - alreadySettled;
        return (outstandingBalance, alreadySettled);
    }
    public static async Task <(decimal OutstandingBalance, decimal AlreadySettled)> CalculateExpenseBalanceAsync (VapeShopInventoryDbContext context, Expense expense)
    {
        if(expense.PaymentMethod == PaymentMethod.Cash || expense.PaymentMethod == PaymentMethod.DigitalPayment)
        {
            return (0m, expense.Amount);
        }
        decimal alreadySettled = await context.Settlements.Where(se => se.ExpenseId == expense.Id).SumAsync(se => se.Amount);
        decimal outstandingBalance = expense.Amount - alreadySettled;
        return (outstandingBalance, alreadySettled);
    }
}