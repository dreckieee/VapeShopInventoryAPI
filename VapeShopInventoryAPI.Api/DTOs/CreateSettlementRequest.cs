namespace VapeShopInventoryAPI.Api.DTOs;
public class CreateSettlementRequest
{
    public int? SaleId {get; set;}
    public int? ExpenseId {get; set;}
    public required decimal Amount {get; set;}
    public required PaymentMethod PaymentMethod {get; set;}
    public string? PaymentNote {get; set;}
    public required DateTime Date {get; set;}
}