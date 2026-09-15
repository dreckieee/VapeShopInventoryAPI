using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Api;
[ApiController]
[Route("api/[controller]")]
public class CapitalTransactionsController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<CapitalTransactionsController> _logger;
    public CapitalTransactionsController(VapeShopInventoryDbContext context, ILogger<CapitalTransactionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CapitalTransactionResponse>>> GetCapitalTransactions([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = _context.CapitalTransactions.AsQueryable();
        if(year != null)
        {
            query = query.Where(capitalTransaction => capitalTransaction.Date.Year == year);
        }
        if(month != null)
        {
            query = query.Where(capitalTransaction => capitalTransaction.Date.Month == month);
        }

        var capitalTransactions = await query.ToListAsync();
        var response = new List<CapitalTransactionResponse>();
        foreach(CapitalTransaction capitalTransaction in capitalTransactions)
        {
            response.Add(CapitalTransactionResponse.FromCapitalTransaction(capitalTransaction));
        }
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CapitalTransactionResponse>> GetCapitalTransaction(int id)
    {
        var capitalTransaction = await _context.CapitalTransactions.FindAsync(id);
        if(capitalTransaction == null)
        {
            return NotFound();
        }
        
        var response = CapitalTransactionResponse.FromCapitalTransaction(capitalTransaction);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CapitalTransactionResponse>> CreateCapitalTransaction([FromBody] CreateCapitalTransactionRequest request)
    {
        try
        {
            var capitalTransaction = new CapitalTransaction(request.Type, request.Amount, request.PaymentMethod, request.Note, request.Date);

            _context.CapitalTransactions.Add(capitalTransaction);
            await _context.SaveChangesAsync();

            var response = CapitalTransactionResponse.FromCapitalTransaction(capitalTransaction);
            return CreatedAtAction(nameof(GetCapitalTransaction), new { id = capitalTransaction.Id }, response);
        }
        catch(ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}