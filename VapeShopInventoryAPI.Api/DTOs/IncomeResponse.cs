namespace VapeShopInventoryAPI.Api.DTOs;
public record IncomeResponse
{
    public int? Year {get; init;}
    public int? Month {get; init;}
    public required decimal TotalSales {get; init;}
    public required decimal TotalExpenses {get; init;}
    public required decimal NetIncome {get; init;}
    public static IncomeResponse FromSalesExpenses (int? year, int? month, decimal totalSales, decimal totalExpenses) => new()
    {
        Year = year,
        Month = month,
        TotalSales = totalSales,
        TotalExpenses = totalExpenses,
        NetIncome = totalSales - totalExpenses
    };
    
}