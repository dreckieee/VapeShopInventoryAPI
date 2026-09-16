namespace VapeShopInventoryAPI.Api.DTOs;
public record CashBalanceResponse
{
    public required decimal CashOnHand {get; init;}
    public required decimal DigitalBalance {get; init;}
    public required decimal ReceivablesOutstanding {get; init;}
    public required decimal PayablesOutstanding {get; init;}
    public static CashBalanceResponse CreateCashBalance (decimal cashOnHand, decimal digitalBalance, decimal receivablesOutstanding, decimal payablesOutstanding) => new()
    {
        CashOnHand = cashOnHand,
        DigitalBalance = digitalBalance,
        ReceivablesOutstanding = receivablesOutstanding,
        PayablesOutstanding = payablesOutstanding
    };
}