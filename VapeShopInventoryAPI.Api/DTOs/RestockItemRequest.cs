namespace VapeShopInventoryAPI.Api.DTOs;
public class RestockItemRequest
{
    public int ProductId {get; set;}
    public int Quantity {get; set;}
    public decimal UnitCost {get; set;}
}