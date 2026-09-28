using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Api;

public static class CashBalanceHistoryCalculator
{
    private const int MinDays = 1;
    private const int MaxDays = 366;

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

    private static async Task<List<HistoryEntry>> GetSettlementEntriesAsync(VapeShopInventoryDbContext context, DateTime cutoffDate)
    {
        var settlementEntries = await context.Settlements
            .Where(se => se.Date >= cutoffDate)
            .Select(se => new HistoryEntry
            {
                Date = se.Date,
                SourceType = CashBalanceSourceType.Settlement,
                SourceId = se.Id,
                PaymentMethod = se.PaymentMethod,
                Amount = se.SaleId != null ? se.Amount : -se.Amount,
                Description = se.PaymentNote
            })
            .ToListAsync();

        return settlementEntries;
    }

    private static async Task<List<HistoryEntry>> GetCapitalTransactionEntriesAsync(VapeShopInventoryDbContext context, DateTime cutoffDate)
    {
        var capitalTransactionEntries = await context.CapitalTransactions
            .Where(ct => ct.Date >= cutoffDate)
            .Select(ct => new HistoryEntry
            {
                Date = ct.Date,
                SourceType = CashBalanceSourceType.CapitalTransaction,
                SourceId = ct.Id,
                PaymentMethod = ct.PaymentMethod,
                Amount = ct.Type == CapitalTransactionType.Deposit ? ct.Amount : -ct.Amount,
                Description = ct.Note
            })
            .ToListAsync();

        return capitalTransactionEntries;
    }

    public static async Task<List<CashBalanceHistoryEntryResponse>> GetCashBalanceHistoryAsync(VapeShopInventoryDbContext context, int days)
    {
        if (days < MinDays || days > MaxDays)
        {
            throw new ArgumentOutOfRangeException(nameof(days), $"Days for showing cash balance history entries should be between {MinDays} and {MaxDays}.");
        }

        DateTime cutoffDate = DateTime.Today.AddDays(-(days - 1));

        var historyEntries = new List<HistoryEntry>();
        var saleEntries = await GetSaleEntriesAsync(context, cutoffDate);
        historyEntries.AddRange(saleEntries);
        var expenseEntries = await GetExpenseEntriesAsync(context, cutoffDate);
        historyEntries.AddRange(expenseEntries);
        var settlementEntries = await GetSettlementEntriesAsync(context, cutoffDate);
        historyEntries.AddRange(settlementEntries);
        var capitalTransactionEntries = await GetCapitalTransactionEntriesAsync(context, cutoffDate);
        historyEntries.AddRange(capitalTransactionEntries);

        historyEntries = historyEntries
            .OrderByDescending(he => he.Date)
            .ThenBy(he => he.SourceType)
            .ThenByDescending(he => he.SourceId)
            .ToList();

        var cashBalanceHistoryEntries = new List<CashBalanceHistoryEntryResponse>();
        var (runningCashOnHand, runningDigitalBalance, _, _) = await CashBalanceCalculator.CalculateCashBalanceAsync(context);
        foreach (HistoryEntry he in historyEntries)
        {
            var cashBalanceHistoryEntry = new CashBalanceHistoryEntryResponse
            {
                Date = he.Date,
                SourceType = he.SourceType,
                SourceId = he.SourceId,
                PaymentMethod = he.PaymentMethod,
                Amount = he.Amount,
                RunningCashBalance = runningCashOnHand,
                RunningDigitalBalance = runningDigitalBalance,
                Description = he.Description
            };
            cashBalanceHistoryEntries.Add(cashBalanceHistoryEntry);
            if (he.PaymentMethod == PaymentMethod.Cash)
            {
                runningCashOnHand -= he.Amount;
            }
            if (he.PaymentMethod == PaymentMethod.DigitalPayment)
            {
                runningDigitalBalance -= he.Amount;
            }
        }

        return cashBalanceHistoryEntries;
    }
}