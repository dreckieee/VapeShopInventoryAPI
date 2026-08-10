namespace VapeShopInventoryAPI.Api.DTOs;
public class CreateSaleRequest
{
    public required DateTime SaleDate {get; set;}
    public required PaymentMethod PaymentMethod {get; set;}
    public string? PaymentNote {get; set;}
}