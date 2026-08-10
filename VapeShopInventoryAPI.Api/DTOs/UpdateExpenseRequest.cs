namespace VapeShopInventoryAPI.Api.DTOs;
public class UpdateExpenseRequest
{
    public required PaymentMethod PaymentMethod {get; set;}
    public string? PaymentNote {get; set;}
    public required string Description {get; set;}
    public required decimal Amount {get; set;}
    public required string Category {get; set;}
    public required DateTime Date {get; set;}
}