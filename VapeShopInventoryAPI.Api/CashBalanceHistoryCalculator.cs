using Microsoft.EntityFrameworkCore;

namespace VapeShopInventoryAPI.Api;

public static class CashBalanceHistoryCalculator
{
    private record HistoryEntry
    {
        public required DateTime Date { get; init; }
        public required CashBalanceSourceType SourceType { get; init; }
        public required int SourceId { get; init; }
        public required PaymentMethod PaymentMethod { get; init; }
        public required decimal Amount { get; init; }
        public required string? Description { get; init; }
    }

    private static async Task<List<HistoryEntry>> GetSaleEntriesAsync(VapeShopInventoryDbContext context, DateTime cutoffDate)
    {
        var saleEntries = await context.Sales
        .Where(s => s.IsClosed && s.SaleDate >= cutoffDate && (s.PaymentMethod == PaymentMethod.Cash || s.PaymentMethod == PaymentMethod.DigitalPayment))
        .Select(s => new HistoryEntry
        {
            Date = s.SaleDate,
            SourceType = CashBalanceSourceType.Sale,
            SourceId = s.Id,
            PaymentMethod = s.PaymentMethod,
            Amount = s.SaleItems.Sum(si => si.Quantity * si.UnitPriceAtSale),
            Description = s.PaymentNote
        })
        .ToListAsync();

        return saleEntries;
    }

    private static async Task<List<HistoryEntry>> GetExpenseEntriesAsync(VapeShopInventoryDbContext context, DateTime cutoffDate)
    {
        var expenseEntries = await context.Expenses
        .Where(e => e.Date >= cutoffDate && (e.PaymentMethod == PaymentMethod.Cash || e.PaymentMethod == PaymentMethod.DigitalPayment))
        .Select(e => new HistoryEntry
        {
            Date = e.Date,
            SourceType = CashBalanceSourceType.Expense,
            SourceId = e.Id,
            PaymentMethod = e.PaymentMethod,
            Amount = -e.Amount,
            Description = e.PaymentNote
        })
        .ToListAsync();

        return expenseEntries;
    }
}