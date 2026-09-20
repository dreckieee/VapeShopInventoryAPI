namespace VapeShopInventoryAPI.Api.DTOs;

public record CashBalanceHistoryEntryResponse
{
    public required DateTime Date { get; init; }
    public required CashBalanceSourceType SourceType { get; init; }
    public required int SourceId { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public required decimal Amount { get; init; }
    public required decimal RunningCashBalance { get; init; }
    public required decimal RunningDigitalBalance { get; init; }
    public required string? Description { get; init; }
}