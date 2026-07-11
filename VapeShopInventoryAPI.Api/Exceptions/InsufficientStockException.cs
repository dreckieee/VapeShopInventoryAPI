public class InsufficientStockException : Exception
{
    public  List<StockShortage> Shortages {get;}
    public InsufficientStockException (List<StockShortage> shortages) : base ("One or more items have insufficient stock.")
    {
        Shortages = shortages;
    }
}