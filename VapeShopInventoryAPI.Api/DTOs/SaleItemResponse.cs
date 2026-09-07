namespace VapeShopInventoryAPI.Api.DTOs;
public record SaleItemResponse
{
    public required int Id {get; init;}
    public required int ProductId {get; init;}
    public required int Quantity {get; init;}
    public required decimal UnitPriceAtSale {get; init;}
    public required int TransactionNumber {get; init;}
    public static SaleItemResponse FromSaleItem (SaleItem saleItem) => new()
    {
        Id = saleItem.Id,
        ProductId = saleItem.ProductId,
        Quantity = saleItem.Quantity,
        UnitPriceAtSale = saleItem.UnitPriceAtSale,
        TransactionNumber = saleItem.TransactionNumber
    };
}