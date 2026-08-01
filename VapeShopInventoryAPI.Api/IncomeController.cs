using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class IncomeController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<IncomeController> _logger;
    public IncomeController(VapeShopInventoryDbContext context, ILogger<IncomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IncomeResponse>> GetIncome([FromQuery] int? year, [FromQuery] int? month)
    {
        var querySales = _context.Sales.Where(sale => sale.IsClosed).AsQueryable();
        var queryExpenses = _context.Expenses.AsQueryable();

        if(year != null)
        {
            querySales = querySales.Where(sale => sale.SaleDate.Year == year);
            queryExpenses = queryExpenses.Where(expense => expense.Date.Year == year);
        }

        if(month != null)
        {
            querySales = querySales.Where(sale => sale.SaleDate.Month == month);
            queryExpenses = queryExpenses.Where(expense => expense.Date.Month == month);
        }

        var totalSales = await querySales.SelectMany(sale => sale.SaleItems).SumAsync(saleItem => saleItem.Quantity * saleItem.UnitPriceAtSale);
        var totalExpenses = await queryExpenses.SumAsync(expense => expense.Amount);
        var response = IncomeResponse.FromSalesExpenses(year, month, totalSales, totalExpenses);
        
        return Ok(response);
    }
}