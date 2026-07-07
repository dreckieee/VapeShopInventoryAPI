public class Sale
{
    public int Id {get; private set;}
    public DateTime SaleDate {get; private set;}
    public DateTime CreatedAt {get; private set;}
    private readonly List<SaleItem> _saleItems = new();
    public IReadOnlyList<SaleItem> SaleItems => _saleItems;
    public bool IsClosed {get; private set;} = false;
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
        _saleItems.Add(saleItem);  
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
    public void Edit(DateTime newSaleDate)
    {
        GuardClosedSale();
        GuardSale(newSaleDate);
        SaleDate = newSaleDate;
    }
    public void CloseSale ()
    {
        GuardClosedSale();
        if (_saleItems.Count == 0)
        {
            throw new InvalidOperationException("You cannot close an empty sale. Add items to the sale first.");
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