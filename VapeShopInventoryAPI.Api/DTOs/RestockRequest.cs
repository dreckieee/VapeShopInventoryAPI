namespace VapeShopInventoryAPI.Api.DTOs;
public class RestockRequest
{
    public DateTime Date {get; set;}
    public required string Description {get; set;}
    public required List<RestockItemRequest> Items {get; set;}
}