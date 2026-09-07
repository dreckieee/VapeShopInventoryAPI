namespace VapeShopInventoryAPI.Api.DTOs;
public record SettlementResponse
{
    public int Id {get; init;}
    public int? SaleId {get; init;}
    public int? ExpenseId {get; init;}
    public required decimal Amount {get; init;}
    public required PaymentMethod PaymentMethod {get; init;}
    public string? PaymentNote {get; init;}
    public required DateTime Date {get; init;}
    public static SettlementResponse FromSettlement(Settlement settlement) => new()
    {
        Id = settlement.Id,
        SaleId = settlement.SaleId,
        ExpenseId = settlement.ExpenseId,
        Amount = settlement.Amount,
        PaymentMethod = settlement.PaymentMethod,
        PaymentNote = settlement.PaymentNote,
        Date = settlement.Date
    };
}