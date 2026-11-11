using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<CatalogController> _logger;
    public CatalogController(VapeShopInventoryDbContext context, ILogger<CatalogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublicProductResponse>>> GetCatalog([FromQuery] string? name, [FromQuery] string? category)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(product => product.Name.ToLower().Contains(name.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(product => product.Category.ToLower().Contains(category.ToLower()));
        }
        var publicProducts = await query.Select(product => new PublicProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            IsInStock = product.StockQuantity > 0,
            Category = product.Category
        }).ToListAsync();

        return Ok(publicProducts);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<PublicProductResponse>> GetCatalogProduct(int id)
    {
        var publicProduct = await _context.Products
            .Where(product => product.Id == id)
            .Select(product => new PublicProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                IsInStock = product.StockQuantity > 0,
                Category = product.Category
            })
            .FirstOrDefaultAsync();
        if (publicProduct == null)
        {
            return NotFound();
        }

        return Ok(publicProduct);
    }
}
