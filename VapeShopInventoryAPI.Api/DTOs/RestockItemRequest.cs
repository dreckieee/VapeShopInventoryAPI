namespace VapeShopInventoryAPI.Api.DTOs;
public class RestockItemRequest
{
    public required int ProductId {get; set;}
    public required int Quantity {get; set;}
    public required decimal UnitCost {get; set;}
}