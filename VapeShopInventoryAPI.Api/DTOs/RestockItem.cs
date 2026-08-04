namespace VapeShopInventoryAPI.Api.DTOs;
public class RestockItem
{
    public int ProductId {get; set;}
    public int Quantity {get; set;}
    public decimal UnitCost {get; set;}
}