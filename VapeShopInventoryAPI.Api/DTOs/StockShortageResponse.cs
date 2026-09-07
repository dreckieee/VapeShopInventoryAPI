namespace VapeShopInventoryAPI.Api.DTOs;
public class StockShortageResponse
{
    public required int ProductId { get; set;}
    public required string ProductName { get; set;}
    public required int RequestedQuantity { get; set;}
    public required int AvailableQuantity { get; set;}
}