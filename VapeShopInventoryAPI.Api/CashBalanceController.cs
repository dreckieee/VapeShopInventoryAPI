using Microsoft.AspNetCore.Mvc;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class CashBalanceController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<CashBalanceController> _logger;
    public CashBalanceController(VapeShopInventoryDbContext context, ILogger<CashBalanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CashBalanceResponse>> GetCashBalance()
    {
        var (cashOnHand, digitalBalance, receivablesOutstanding, payablesOutstanding) = await CashBalanceCalculator.CalculateCashBalanceAsync(_context);
        var response = CashBalanceResponse.CreateCashBalance(cashOnHand, digitalBalance, receivablesOutstanding, payablesOutstanding);
        
        return Ok(response);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<CashBalanceHistoryEntryResponse>>> GetCashBalanceHistory([FromQuery] int days)
    {
        try
        {
            var cashBalanceHistory = await CashBalanceHistoryCalculator.GetCashBalanceHistoryAsync(_context, days);
            return Ok(cashBalanceHistory);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}