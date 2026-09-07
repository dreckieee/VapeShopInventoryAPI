namespace VapeShopInventoryAPI.Api.DTOs;
public record StockShortageResponse
{
    public required int ProductId { get; init;}
    public required string ProductName { get; init;}
    public required int RequestedQuantity { get; init;}
    public required int AvailableQuantity { get; init;}
}