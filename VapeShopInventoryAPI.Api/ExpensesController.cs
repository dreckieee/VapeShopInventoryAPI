using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Api;
[ApiController]
[Route("api/[controller]")]

public class ExpensesController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<ExpensesController> _logger;
    public ExpensesController (VapeShopInventoryDbContext context, ILogger<ExpensesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetExpenses([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = _context.Expenses.AsQueryable();
        if(year != null)
        {
            query = query.Where(expense => expense.Date.Year == year);
        }
        if(month != null)
        {
            query = query.Where(expense => expense.Date.Month == month);
        }

        var expenses = await query.ToListAsync();
        var response = new List<ExpenseResponse>();
        foreach(Expense expense in expenses)
        {
            var (outstandingBalance, alreadySettled) = await SettlementCalculator.CalculateExpenseBalanceAsync(_context, expense);
            response.Add(ExpenseResponse.FromExpense(expense, outstandingBalance, alreadySettled));
        }
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseResponse>> GetExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if(expense == null)
        {
            return NotFound();
        }
        var (outstandingBalance, alreadySettled) = await SettlementCalculator.CalculateExpenseBalanceAsync(_context, expense);
        var response = ExpenseResponse.FromExpense(expense, outstandingBalance, alreadySettled);
        return Ok(response);
    }


    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expense = new Expense(request.Date, request.Description, request.Amount, request.Category, request.PaymentMethod, request.PaymentNote);

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var (outstandingBalance, alreadySettled) = await SettlementCalculator.CalculateExpenseBalanceAsync(_context, expense);
            var response = ExpenseResponse.FromExpense(expense, outstandingBalance, alreadySettled);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, response);
        }
        catch(ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

        
    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseResponse>> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
            {
                return NotFound();
            }

            var hasDeliveryReferences = await _context.DeliveryItems.AnyAsync(di => di.ExpenseId == expense.Id);
            if (hasDeliveryReferences && (expense.Amount != request.Amount || expense.Category != request.Category))
            {
                return Conflict(new {message = "Cannot edit Amount or Category on an expense linked to existing delivery item record/s."});
            }

            var hasSettlementReferences = await _context.Settlements.AnyAsync(se => se.ExpenseId == expense.Id);
            decimal alreadySettled = 0;
            decimal outstandingBalance = 0;
            if (hasSettlementReferences)
            {
                (outstandingBalance, alreadySettled) = await SettlementCalculator.CalculateExpenseBalanceAsync(_context, expense);
            }
            if (hasSettlementReferences && request.PaymentMethod != expense.PaymentMethod)
            {
                return Conflict(new {message = "Cannot edit Payment Method on an expense linked to existing settlement record/s."});
            }
            if (hasSettlementReferences && request.Amount < alreadySettled)
            {
                return Conflict(new {message = "Cannot reduce Amount below the total already settled on this expense."});
            }
            if (hasSettlementReferences)
            {
                DateTime minDate = await _context.Settlements.Where(se => se.ExpenseId == expense.Id).MinAsync(se => se.Date);
                if(minDate < request.Date)
                {
                    return Conflict(new {message = "Cannot edit Date to later than the earliest settlement."});
                }
            }

            expense.Edit(request.Date, request.Description, request.Amount, request.Category,request.PaymentMethod, request.PaymentNote); 
            await _context.SaveChangesAsync();
            
            (outstandingBalance, alreadySettled) = await SettlementCalculator.CalculateExpenseBalanceAsync(_context, expense);
            var response = ExpenseResponse.FromExpense(expense, outstandingBalance, alreadySettled);
            return Ok(response);
        }
        catch(ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message});
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        var hasDeliveryReferences = await _context.DeliveryItems.AnyAsync(di => di.ExpenseId == expense.Id);
        if (hasDeliveryReferences)
        {
            return Conflict(new {message = "Cannot delete this expense due to existing delivery item records."});
        }
        var hasSettlementReferences = await _context.Settlements.AnyAsync(se => se.ExpenseId == expense.Id);
        if (hasSettlementReferences)
        {
            return Conflict(new {message = "Cannot delete this expense due to existing settlement records."});
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}