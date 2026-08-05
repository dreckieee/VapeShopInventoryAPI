
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;

[ApiController]
[Route("api/[controller]")]

public class RestockController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<RestockController> _logger;
    public RestockController(VapeShopInventoryDbContext context, ILogger<RestockController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<RestockResponse>> Restock ([FromBody] RestockRequest request)
    {
        try
        {
            if(request.Items.Count == 0)
            {
                throw new ArgumentException("Restock request must include at least one item.", nameof(request));
            }

            var products = new List<Product>();
            foreach(RestockItemRequest ri in request.Items)
            {
                var product = await _context.Products.FindAsync(ri.ProductId);
                if (product == null)
                {
                    return NotFound();
                }
                products.Add(product);
            }

            decimal restockTotalAmount = request.Items.Sum(item => item.Quantity * item.UnitCost);
            var expense = new Expense(request.Date, request.Description, restockTotalAmount, Expense.RestockCategory);
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var deliveryItems = new List<DeliveryItem>();
            foreach (RestockItemRequest ri in request.Items)
            {
                var product = products.Find(p => p.Id == ri.ProductId);
                product!.Restock(ri.Quantity);
                var deliveryItem = new DeliveryItem(expense.Id, product!.Id, ri.Quantity, ri.UnitCost);    
                _context.DeliveryItems.Add(deliveryItem);
                deliveryItems.Add(deliveryItem);
            }

            await _context.SaveChangesAsync();

            var distinctProducts = products.DistinctBy(p => p.Id).ToList();
            var response = RestockResponse.FromRestock(expense, deliveryItems, distinctProducts);
            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

    }
}
