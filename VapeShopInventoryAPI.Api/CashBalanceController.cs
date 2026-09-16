using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}