namespace VapeShopInventoryAPI.Api.DTOs;
public record CapitalTransactionResponse
{
    public required int Id {get; init;}
    public required CapitalTransactionType Type {get; init;}
    public required decimal Amount {get; init;}
    public required PaymentMethod PaymentMethod {get; init;}
    public required string? Note {get; init;}
    public required DateTime Date {get; init;}
    public required DateTime CreatedAt {get; init;}
    public static CapitalTransactionResponse FromCapitalTransaction (CapitalTransaction capitalTransaction) => new()
    {
        Id = capitalTransaction.Id,
        Type = capitalTransaction.Type,
        Amount = capitalTransaction.Amount,
        PaymentMethod = capitalTransaction.PaymentMethod,
        Note = capitalTransaction.Note,
        Date = capitalTransaction.Date,
        CreatedAt = capitalTransaction.CreatedAt
    };
 
}