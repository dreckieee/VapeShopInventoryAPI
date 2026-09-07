namespace VapeShopInventoryAPI.Api.DTOs;
public record DeliveryItemResponse
{
    public required int ProductId { get; init; }
    public required int ExpenseId { get; init; }
    public required string ProductName {get; init;}
    public required int Quantity { get; init; }
    public required decimal UnitCost { get; init; }
    public required decimal TotalCost { get; init; }
    public static DeliveryItemResponse FromDeliveryItem(DeliveryItem deliveryItem, string name) => new()
    {
        ProductId = deliveryItem.ProductId,
        ExpenseId = deliveryItem.ExpenseId,
        ProductName = name,
        Quantity = deliveryItem.Quantity,
        UnitCost = deliveryItem.UnitCost,
        TotalCost = deliveryItem.TotalCost
    };
}