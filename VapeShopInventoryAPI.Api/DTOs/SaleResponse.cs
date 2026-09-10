using VapeShopInventoryAPI.Api;
namespace VapeShopInventoryAPI.Api.DTOs;
public record SaleResponse
{
    public required int Id {get; init;}
    public required DateTime SaleDate {get; init;}
    public required DateTime CreatedAt {get; init;}
    public required PaymentMethod PaymentMethod {get; init;}
    public string? PaymentNote {get; init;}
    public required bool IsClosed {get; init;}
    public required int TransactionCount {get; init;}
    public required int ReductionFrequency {get; init;}
    public required int TotalQuantityReduction {get; init;}
    public required List<SaleItemResponse> SaleItems {get; init;} = new();
    public required decimal TotalAmount {get; init;}
    public required decimal OutstandingBalance {get; init;}
    public required decimal AmountSettled {get; init;}
    public static SaleResponse FromSale(Sale sale, decimal outstandingBalance, decimal amountSettled) 
    {
        var saleItems = sale.SaleItems.Select(item => SaleItemResponse.FromSaleItem(item)).ToList();
      
        return new SaleResponse{Id = sale.Id,
        SaleDate = sale.SaleDate,
        CreatedAt = sale.CreatedAt,
        PaymentMethod = sale.PaymentMethod,
        PaymentNote = sale.PaymentNote,
        IsClosed = sale.IsClosed,
        TransactionCount = sale.TransactionCount,
        ReductionFrequency = sale.ReductionFrequency,
        TotalQuantityReduction = sale.TotalQuantityReduction,
        SaleItems = saleItems,
        TotalAmount = sale.TotalAmount,
        OutstandingBalance = outstandingBalance,
        AmountSettled = amountSettled
        };
    }
    
}