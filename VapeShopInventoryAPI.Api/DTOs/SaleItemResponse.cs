public record SaleItemResponse
{
    public int Id {get; init;}
    public int ProductId {get; init;}
    public int Quantity {get; init;}
    public decimal UnitPriceAtSale {get; init;}
    public int TransactionNumber {get; init;}
    public static SaleItemResponse FromSaleItem (SaleItem saleItem) => new()
    {
        Id = saleItem.Id,
        ProductId = saleItem.ProductId,
        Quantity = saleItem.Quantity,
        UnitPriceAtSale = saleItem.UnitPriceAtSale,
        TransactionNumber = saleItem.TransactionNumber
    };
}