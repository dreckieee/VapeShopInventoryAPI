namespace VapeShopInventoryAPI.Api.DTOs;
public class RestockRequest
{
    public required DateTime Date {get; set;}
    public required PaymentMethod PaymentMethod {get; set;}
    public string? PaymentNote {get; set;}
    public required string Description {get; set;}
    public required List<RestockItemRequest> Items {get; set;}
}