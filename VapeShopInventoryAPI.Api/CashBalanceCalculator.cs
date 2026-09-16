using Microsoft.EntityFrameworkCore;
namespace VapeShopInventoryAPI.Api;
public static class CashBalanceCalculator
{
    public static async Task<(decimal CashOnHand, decimal DigitalBalance, decimal ReceivablesOutstanding, decimal PayablesOutstanding)> CalculateCashBalanceAsync(VapeShopInventoryDbContext context)
    {
        var cashOnHand = await CalculateCashOrDigitalBalanceAsync(context, PaymentMethod.Cash);
        var digitalBalance = await CalculateCashOrDigitalBalanceAsync(context, PaymentMethod.DigitalPayment);
        
        var receivablesSettled = await context.Settlements.Where(se => se.SaleId != null).Join(context.Sales.Where(s => s.PaymentMethod == PaymentMethod.Receivable),se => se.SaleId, s => s.Id, (se, s) => se.Amount).SumAsync();
        var receivablesOutstanding = (await context.Sales.Where(s => s.PaymentMethod == PaymentMethod.Receivable && s.IsClosed).SelectMany(s => s.SaleItems).SumAsync(si => si.UnitPriceAtSale * si.Quantity)) - receivablesSettled;

        var payablesSettled = await context.Settlements.Where(se => se.ExpenseId != null).Join(context.Expenses.Where(e => e.PaymentMethod == PaymentMethod.Payable),se => se.ExpenseId, e => e.Id, (se, e) => se.Amount).SumAsync();
        var payablesOutstanding = (await context.Expenses.Where(e => e.PaymentMethod == PaymentMethod.Payable).SumAsync(e => e.Amount)) - payablesSettled;

        return (cashOnHand, digitalBalance, receivablesOutstanding, payablesOutstanding);
    }

    private static async Task <decimal> CalculateCashOrDigitalBalanceAsync (VapeShopInventoryDbContext context, PaymentMethod paymentMethod)
    {
        var sales = await context.Sales.Where(s => s.IsClosed && s.PaymentMethod == paymentMethod).SelectMany(s => s.SaleItems).SumAsync(si => si.UnitPriceAtSale * si.Quantity);
        var expenses = await context.Expenses.Where(e => e.PaymentMethod == paymentMethod).SumAsync(e => e.Amount);
        var saleSettlements = await context.Settlements.Where(se => se.PaymentMethod == paymentMethod && se.SaleId != null).SumAsync(se => se.Amount);
        var expenseSettlements = await context.Settlements.Where(se => se.PaymentMethod == paymentMethod && se.ExpenseId != null).SumAsync(se => se.Amount);
        var deposits = await context.CapitalTransactions.Where(ct => ct.PaymentMethod == paymentMethod && ct.Type == CapitalTransactionType.Deposit).SumAsync(ct => ct.Amount);
        var withdrawals = await context.CapitalTransactions.Where(ct => ct.PaymentMethod == paymentMethod && ct.Type == CapitalTransactionType.Withdrawal).SumAsync(ct => ct.Amount);

        var amount = sales - expenses + saleSettlements - expenseSettlements + deposits - withdrawals;
        return amount;
    }
}