public record SaleResponse
{
    public int Id {get; init;}
    public DateTime SaleDate {get; init;}
    public DateTime CreatedAt {get; init;}
    public bool IsClosed {get; init;}
    public int TransactionCount {get; init;}
    public int ReductionFrequency {get; init;}
    public int TotalQuantityReduction {get; init;}
    public List<SaleItemResponse> SaleItems {get; init;} = new();
    public static SaleResponse FromSale(Sale sale) 
    {
        var saleItems = sale.SaleItems.Select(item => SaleItemResponse.FromSaleItem(item)).ToList();
      
        return new SaleResponse{Id = sale.Id,
        SaleDate = sale.SaleDate,
        CreatedAt = sale.CreatedAt,
        IsClosed = sale.IsClosed,
        TransactionCount = sale.TransactionCount,
        ReductionFrequency = sale.ReductionFrequency,
        TotalQuantityReduction = sale.TotalQuantityReduction,
        SaleItems = saleItems};

    }
    
}