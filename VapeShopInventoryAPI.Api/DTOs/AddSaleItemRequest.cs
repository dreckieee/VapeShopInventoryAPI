namespace VapeShopInventoryAPI.Api.DTOs;
public class AddSaleItemRequest
{
    public required int ProductId {get; set;}
    public required int Quantity {get; set;}
    public required decimal UnitPriceAtSale {get; set;}
}