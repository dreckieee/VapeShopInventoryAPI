namespace VapeShopInventoryAPI.Api.DTOs;
public class CreateCapitalTransactionRequest
{
    public required CapitalTransactionType Type {get; set;}
    public required decimal Amount {get; set;}
    public required PaymentMethod PaymentMethod {get; set;}
    public string? Note {get; set;}
    public required DateTime Date {get; set;}
}