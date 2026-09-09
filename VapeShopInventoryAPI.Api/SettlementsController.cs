using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class SettlementsController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<SettlementsController> _logger;
    public SettlementsController(VapeShopInventoryDbContext context, ILogger<SettlementsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SettlementResponse>> GetSettlement(int id)
    {
        var settlement = await _context.Settlements.FindAsync(id);
        if (settlement == null)
        {
            return NotFound();
        }
        var settlementResponse = SettlementResponse.FromSettlement(settlement);

        return Ok(settlementResponse);
    }

    [HttpPost]
    public async Task<ActionResult<SettlementResponse>> CreateSettlement([FromBody] CreateSettlementRequest request)
    {
        try
        {
            if(request.SaleId != null)
            {
                var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == request.SaleId);
                if (sale == null)
                {
                    return BadRequest(new { message = $"Cannot find any sale with an id of {request.SaleId}" });
                }
                if(sale.PaymentMethod != PaymentMethod.Receivable)
                {
                    return BadRequest(new { message = "Found Sale's payment method is not receivable" });
                }

                decimal alreadySettled = await _context.Settlements.Where(se => se.SaleId == sale.Id).SumAsync(se => se.Amount);
                decimal targetTotal = sale.SaleItems.Sum(s => s.Quantity * s.UnitPriceAtSale);
                decimal outstandingBalance = targetTotal - alreadySettled;
                if (request.Amount > outstandingBalance)
                {
                    return BadRequest(new { message = "Settlement amount is greater than the sale outstanding balance" });
                }
            }
            if(request.ExpenseId != null)
            {
                var expense = await _context.Expenses.FindAsync(request.ExpenseId);
                if (expense == null)
                {
                    return BadRequest(new { message = $"Cannot find any expense with an id of {request.ExpenseId}" });
                }
                if(expense.PaymentMethod != PaymentMethod.Payable)
                {
                    return BadRequest(new { message = "Found Expense's payment method is not payable" });
                }
                decimal alreadySettled = await _context.Settlements.Where(se => se.ExpenseId == expense.Id).SumAsync(se => se.Amount);
                decimal outstandingBalance = expense.Amount - alreadySettled;
                if (request.Amount > outstandingBalance)
                {
                    return BadRequest(new { message = "Settlement amount is greater than the expense outstanding balance" });
                }
            }
            
            Settlement settlement = new Settlement(request.SaleId, request.ExpenseId, request.Amount, request.PaymentMethod, request.PaymentNote, request.Date);
            
            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync();

            var settlementResponse = SettlementResponse.FromSettlement(settlement);
            return CreatedAtAction(nameof(GetSettlement), new { id = settlement.Id }, settlementResponse);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}