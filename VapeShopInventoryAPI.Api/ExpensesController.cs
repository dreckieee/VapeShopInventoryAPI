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
        var response = expenses.Select(expense => ExpenseResponse.FromExpense(expense)).ToList();

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
        var response = ExpenseResponse.FromExpense(expense);
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

            var response = ExpenseResponse.FromExpense(expense);
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
            expense.Edit(request.Date, request.Description, request.Amount, request.Category,request.PaymentMethod, request.PaymentNote); 
            await _context.SaveChangesAsync();
            
            var response = ExpenseResponse.FromExpense(expense);
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

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}