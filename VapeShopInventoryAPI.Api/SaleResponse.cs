public class SaleResponse
{
    public int Id {get; set;}
    public DateTime SaleDate {get; set;}
    public DateTime CreatedAt {get; set;}
    public bool IsClosed {get; set;}
    public int TransactionCount {get; set;}
    public int ReductionFrequency {get; set;}
    public int TotalQuantityReduction {get; set;}
    public required List<SaleItemResponse> SaleItems {get; set;}
}