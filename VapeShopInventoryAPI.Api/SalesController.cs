using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<SalesController> _logger;
    public SalesController(VapeShopInventoryDbContext context, ILogger<SalesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleResponse>> GetSale(int id)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null)
        {
            return NotFound();
        }

        var saleItems = sale.SaleItems.Select(item => new SaleItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPriceAtSale = item.UnitPriceAtSale,
            TransactionNumber = item.TransactionNumber
        }).ToList();

        var saleResponse = new SaleResponse {Id = sale.Id, 
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

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> CreateSale([FromBody] CreateSaleRequest request)
    {
        var sale = new Sale(request.SaleDate);
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        var saleResponse = new SaleResponse {Id = sale.Id, 
        SaleDate = sale.SaleDate, 
        CreatedAt = sale.CreatedAt, 
        IsClosed = sale.IsClosed, 
        TransactionCount = sale.TransactionCount, 
        ReductionFrequency = sale.ReductionFrequency, 
        TotalQuantityReduction = sale.TotalQuantityReduction,
        SaleItems = new()
        }; 

        return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, saleResponse);
    }

    [HttpPatch("{id}/date")]
    public async Task<ActionResult<SaleResponse>> EditSaleDate(int id, [FromBody] EditSaleDateRequest request)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null)
        {
            return NotFound();
        }
        try
        {
            sale.EditSaleDate(request.SaleDate);
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