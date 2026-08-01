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
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses([FromQuery] int? year, [FromQuery] int? month)
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
        var expense = new Expense(request.Date, request.Description, request.Amount, request.Category);

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        var response = ExpenseResponse.FromExpense(expense);
        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, response);
    }

        
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }
        expense.Edit(request.Date, request.Description, request.Amount, request.Category);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
        {
            return NotFound();
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}