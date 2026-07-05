using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly VapeShopInventoryDbContext _context;
    private readonly ILogger<ProductsController> _logger;
    public ProductsController(VapeShopInventoryDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        try
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqliteException sqliteEx)
            {
                var tableName = _context.Model.FindEntityType(typeof(Product))?.GetTableName();
                if (sqliteEx.Message.Contains($"{tableName}.{nameof(product.Sku)}"))
                {
                    _logger.LogWarning(ex, "Duplicate SKU attempted: {Sku}", product.Sku);
                    return Conflict(new {message = "A product with this SKU already exists."});
                }
            }
                _logger.LogError(ex, "Unexpected Error creating product: {Name}", product.Name);
                return Conflict(new {message = "Unable to create product due to a data conflict."});
        }
    }

        
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, string newProductName, string newProductSku, decimal newProductPrice, int newProductStockQuantity, string newProductCategory)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
    
        product.Edit(newProductName, newProductSku, newProductPrice, newProductStockQuantity, newProductCategory);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }


}