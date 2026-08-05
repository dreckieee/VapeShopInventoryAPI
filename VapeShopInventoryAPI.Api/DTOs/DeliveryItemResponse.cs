namespace VapeShopInventoryAPI.Api.DTOs;
public record DeliveryItemResponse
{
    public int ProductId { get; init; }
    public required string ProductName {get; init;}
    public int Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal TotalCost { get; init; }
    public static DeliveryItemResponse FromDeliveryItem(DeliveryItem deliveryItem, string name) => new()
    {
        ProductId = deliveryItem.ProductId,
        ProductName = name,
        Quantity = deliveryItem.Quantity,
        UnitCost = deliveryItem.UnitCost,
        TotalCost = deliveryItem.TotalCost
    };
}