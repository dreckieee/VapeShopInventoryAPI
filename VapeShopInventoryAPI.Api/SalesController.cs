using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;
using VapeShopInventoryAPI.Api.Exceptions;
namespace VapeShopInventoryAPI.Api;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleResponse>>> GetSales ([FromQuery] int? year, [FromQuery] int? month, [FromQuery] bool? isClosed)
    {
        var query = _context.Sales.AsQueryable();
        if (year != null)
        {
            query = query.Where(s => s.SaleDate.Year == year);
        }
        if (month != null)
        {
            query = query.Where(s => s.SaleDate.Month == month);
        }
        if (isClosed != null)
        {
            query = query.Where(s => s.IsClosed == isClosed);
        }
        
        var sales = await query.ToListAsync();
        var response = sales.Select(sale => SaleResponse.FromSale(sale)).ToList();
        
        return Ok(response);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<SaleResponse>> GetSale(int id)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null)
        {
            return NotFound();
        }

        var saleResponse = SaleResponse.FromSale(sale);
        return Ok(saleResponse);
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> CreateSale([FromBody] CreateSaleRequest request)
    {
        try
        {
            var sale = new Sale(request.SaleDate, request.PaymentMethod, request.PaymentNote);
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            var saleResponse = SaleResponse.FromSale(sale);
            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, saleResponse);
        }
        catch(ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<SaleResponse>> CloseSale(int id)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).ThenInclude(si => si.Product).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null)
        {
            return NotFound();
        }
        try
        {
            sale.CloseSale();
            await _context.SaveChangesAsync();

            var saleResponse = SaleResponse.FromSale(sale);
            return Ok(saleResponse);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InsufficientStockException ex)
        {
            var shortages = ex.Shortages.Select(shortage => new StockShortageResponse{
                ProductId = shortage.ProductId,
                ProductName = shortage.ProductName,
                RequestedQuantity = shortage.RequestedQuantity,
                AvailableQuantity = shortage.AvailableQuantity 
            }).ToList();

            return Conflict(shortages);
        }
    }

    [HttpPatch("{id}/edit")]
    public async Task<ActionResult<SaleResponse>> EditSale(int id, [FromBody] EditSaleRequest request)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null)
        {
            return NotFound();
        }
        try
        {
            sale.EditSale(request.SaleDate, request.PaymentMethod, request.PaymentNote);
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

        var saleResponse = SaleResponse.FromSale(sale);
        return Ok(saleResponse);  
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelSale (int id)
    {
        var sale = await _context.Sales.FindAsync(id);
        if (sale == null)
        {
            return NotFound();
        }
        try
        {
            sale.Cancel();

            var saleItems = await _context.SaleItems.Where(si => si.SaleId == id).ToListAsync();
            _context.SaleItems.RemoveRange(saleItems);
            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}