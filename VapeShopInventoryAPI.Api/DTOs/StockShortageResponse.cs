public class StockShortageResponse
{
    public int ProductId { get; set;}
    public required string ProductName { get; set;}
    public int RequestedQuantity { get; set;}
    public int AvailableQuantity { get; set;}
}