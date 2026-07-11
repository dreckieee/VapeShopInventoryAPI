using System.Security.Cryptography.Xml;

public class Sale
{
    public int Id {get; private set;}
    public DateTime SaleDate {get; private set;}
    public DateTime CreatedAt {get; private set;}
    private readonly List<SaleItem> _saleItems = new();
    public IReadOnlyList<SaleItem> SaleItems => _saleItems;
    public bool IsClosed {get; private set;} = false;
    public int TransactionCount {get; private set;} = 0;
    public int ReductionFrequency {get; private set;} = 0;
    public int TotalQuantityReduction {get; private set;} = 0;
    public Sale (DateTime saleDate)
    {
        GuardSale(saleDate);
        SaleDate = saleDate;
        CreatedAt = DateTime.Now;
    }

    public void AddSaleItem (SaleItem saleItem)
    {
        GuardClosedSale();
        if (saleItem == null)
        {
            throw new ArgumentNullException(nameof(saleItem), "You must provide the item to be added to the sale.");
        }
        var foundSimilarItem = _saleItems.Find(si => si.ProductId == saleItem.ProductId && si.UnitPriceAtSale == saleItem.UnitPriceAtSale);
        if (foundSimilarItem != null)
        {
            foundSimilarItem.CombineQuantity(saleItem.Quantity);
        }
        else
        {
            TransactionCount ++;
            saleItem.AssignTransactionNumber(TransactionCount);
            _saleItems.Add(saleItem);
        }
    }
    
    public void RemoveSaleItem (SaleItem saleItem)
    {
        GuardClosedSale();
        if (saleItem == null)
        {
            throw new ArgumentNullException(nameof(saleItem), "You must provide the item to be removed from the sale.");
        }
        var saleItemToRemove = _saleItems.Find(i => i.Id == saleItem.Id);
        if (saleItemToRemove == null)
        {
            throw new KeyNotFoundException("No match found for the provided Id of item to be removed.");
        }
        _saleItems.Remove(saleItemToRemove);
    }
    public void EditSaleDate(DateTime newSaleDate)
    {
        GuardClosedSale();
        GuardSale(newSaleDate);
        SaleDate = newSaleDate;
    }
    public void ReduceSaleItemQuantity(SaleItem saleItem, int amount)
    {
        GuardClosedSale();
        if (saleItem == null)
        {
            throw new ArgumentNullException(nameof(saleItem), "You must provide the item which to reduce the quantity from the sale.");
        }
        var saleItemToReduceQuantity = _saleItems.Find(i => i.Id == saleItem.Id);
        if (saleItemToReduceQuantity == null)
        {
            throw new KeyNotFoundException("No match found for the provided Id of item which to reduce quantity from.");
        }
        saleItemToReduceQuantity.ReduceQuantity(amount);
        ReductionFrequency ++;
        TotalQuantityReduction += amount;
        if (saleItemToReduceQuantity.Quantity == 0)
        {
            _saleItems.Remove(saleItemToReduceQuantity);
        }
    }
    public void CloseSale ()
    {
        GuardClosedSale();
        if (_saleItems.Count == 0)
        {
            throw new InvalidOperationException("You cannot close an empty sale. Add items to the sale first.");
        }
        
        var shortages = new List<StockShortage>();
        foreach (SaleItem si in _saleItems)
        {
            if(si.Quantity > si.Product.StockQuantity)
            {
                var shortage = new StockShortage(si.Product.Id, si.Product.Name, si.Quantity, si.Product.StockQuantity);
                shortages.Add(shortage);
            }
        }
        if (shortages.Count > 0)
        {
            throw new InsufficientStockException(shortages);
        }
        foreach(SaleItem si in _saleItems)
        {
            si.Product.ReduceStock(si.Quantity);
        }
        IsClosed = true;
    }
    private void GuardSale(DateTime saleDate)
    {
        if (saleDate == default)
        {
            throw new ArgumentException("Date of sale must be provided.", nameof(saleDate));
        }
        if (saleDate > DateTime.Now)
        {
            throw new ArgumentException("Date of sale cannot be in the future.", nameof(saleDate));
        }
    }

    private void GuardClosedSale()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Sale transaction is already closed.");
        }
    }
}