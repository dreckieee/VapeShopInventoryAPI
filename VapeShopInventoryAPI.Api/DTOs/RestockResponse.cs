namespace VapeShopInventoryAPI.Api.DTOs;
public record RestockResponse
{
    public required ExpenseResponse Expense { get; init; }
    public required List<DeliveryItemResponse> DeliveryItems { get; init; }
    public required List<ProductResponse> UpdatedProducts { get; init; }

    public static RestockResponse FromRestock(Expense expense, List<DeliveryItem> deliveryItems, List<Product> products) => new()
    {
        Expense = ExpenseResponse.FromExpense(expense),
        DeliveryItems = deliveryItems.Select(di =>
        {
            var product = products.Find(p => p.Id == di.ProductId);
            return DeliveryItemResponse.FromDeliveryItem(di, product!.Name);
        }).ToList(),
        UpdatedProducts = products.Select(ProductResponse.FromProduct).ToList()
    };
}