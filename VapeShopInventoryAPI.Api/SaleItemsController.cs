using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class SaleItemsController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<SaleItemsController> _logger;
    public SaleItemsController(VapeShopInventoryDbContext context, ILogger<SaleItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }


    [HttpPost("{saleId}/items")]
    public async Task<ActionResult<SaleResponse>> AddSaleItem(int saleId, [FromBody] AddSaleItemRequest request)
    {
        
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == saleId);
        if (sale == null)
        {
            return NotFound();
        }
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound();
        }
        if (product.StockQuantity < request.Quantity)
        {
            return Conflict(new {message = "Stock quantity is not enough for the request quantity."});
        }
        try
        {
            var saleItem = new SaleItem(request.ProductId, request.Quantity, request.UnitPriceAtSale);    
            sale.AddSaleItem(saleItem);
            await _context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        var saleItems = sale.SaleItems.Select(item => new SaleItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPriceAtSale = item.UnitPriceAtSale,
            TransactionNumber = item.TransactionNumber
        }).ToList();

        var saleResponse = new SaleResponse
        {
            Id = sale.Id,
            SaleDate = sale.SaleDate, 
            CreatedAt = sale.CreatedAt, 
            IsClosed = sale.IsClosed, 
            TransactionCount = sale.TransactionCount, 
            ReductionFrequency = sale.ReductionFrequency, 
            TotalQuantityReduction = sale.TotalQuantityReduction,
            SaleItems = saleItems
        };
        return Ok(saleResponse);       
    }

    [HttpPatch("{saleId}/items/{itemId}/reduce")]
    public async Task<ActionResult<SaleResponse>> ReduceSaleItemQuantity(int saleId, int itemId, [FromBody] ReduceSaleItemQuantityRequest request)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == saleId);
        if (sale == null)
        {
            return NotFound();
        }
        try
        {
            var saleItem = sale.SaleItems.FirstOrDefault(si => si.Id == itemId);
            if (saleItem == null)
            {
                return NotFound();
            }
            sale.ReduceSaleItemQuantity(saleItem, request.Amount);
            await _context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        var saleItems = sale.SaleItems.Select(item => new SaleItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPriceAtSale = item.UnitPriceAtSale,
            TransactionNumber = item.TransactionNumber
        }).ToList();

        var saleResponse = new SaleResponse
        {
            Id = sale.Id,
            SaleDate = sale.SaleDate, 
            CreatedAt = sale.CreatedAt, 
            IsClosed = sale.IsClosed, 
            TransactionCount = sale.TransactionCount, 
            ReductionFrequency = sale.ReductionFrequency, 
            TotalQuantityReduction = sale.TotalQuantityReduction,
            SaleItems = saleItems
        };
        return Ok(saleResponse);       
    }

}