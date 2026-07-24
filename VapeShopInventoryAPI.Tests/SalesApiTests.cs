using System.Net;
using System.Net.Http.Json;


namespace VapeShopInventoryAPI.Tests;

public class SalesApiTests
{   
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int? _createdSaleId;
    private int? _createdProductId;
    private int _testInvalidId = -1;
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetSale_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Sales/{_testInvalidId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.StatusCode}");
    }

    [Test]
    public async Task CreateSale_ValidSaleRequest_ReturnsCreated()
    {
        var saleDate = DateTime.Now;
        var (_, sale) = await CreateTestSaleAsync(saleDate);

        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;

        Assert.That(saleDate, Is.EqualTo(sale.SaleDate));
    }

    [Test]
    public async Task GetSale_ExistingId_ReturnsOk()
    {
        var saleDate = DateTime.Now;
        var (_, sale) = await CreateTestSaleAsync(saleDate);

        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;

        var response = await _client.GetAsync($"/api/Sales/{sale.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");
    }

    [Test]
    public async Task AddSaleItem_ValidRequest_ReturnsOk()
    {
        var saleDate = DateTime.Now;
        var (_, sale) = await CreateTestSaleAsync(saleDate);
        
        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;

        string productName = "TestCreateValidProduct"; 
        string productSku = "0a0a2d";
        decimal productPrice = 99.99m; 
        int productStockQuantity = 9;
        string productCategory = "Test";

        var (_, product) = await CreateTestProductAsync(productName,productSku,productPrice,productStockQuantity,productCategory);
        
        Assert.That(product, Is.Not.Null);
        _createdProductId = product.Id;

        int saleItemQuantity = 1;
        decimal saleItemUnitPriceAtSale = product.Price;
        var payload = new
        {
            ProductId = product.Id,
            Quantity = saleItemQuantity,
            UnitPriceAtSale = saleItemUnitPriceAtSale
        };

        var response = await _client.PostAsJsonAsync($"/api/SaleItems/{sale.Id}/items", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");

        sale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.SaleItems.Count, Is.GreaterThan(0));

        var saleItem = sale.SaleItems.Find(si => si.ProductId == product.Id);
        Assert.That(saleItem, Is.Not.Null);

        Assert.That(saleItem.ProductId, Is.EqualTo(product.Id));
        Assert.That(saleItem.Quantity, Is.EqualTo(saleItemQuantity));
        Assert.That(saleItem.UnitPriceAtSale, Is.EqualTo(saleItemUnitPriceAtSale));

        
    
    }

    [TearDown]
    public async Task DeleteTestSaleAndProduct()
    {
        if (_createdSaleId != null)
        {
            try
            {
                var response = await _client.PutAsync($"/api/Sales/{_createdSaleId}/cancel", null);
                if(response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Warning: Failure in cancelling a sale with an Id of {_createdSaleId}: {ex.Message}");
            }
            finally
            {
                _createdSaleId = null;
            }
        }
        if (_createdProductId != null)
        {
            try
            {
                var response = await _client.DeleteAsync($"/api/Products/{_createdProductId}");
                if(response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
                }
                
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with Id {_createdProductId}: {ex.Message}");
            }
            finally
            {
                _createdProductId = null;
            }
        }
    }

    private async Task <(HttpResponseMessage Response, SaleResponse? Sale)> CreateTestSaleAsync(DateTime saleDate)
    {
        var payload = new { saleDate };
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        return (response, sale);
    }

    private async Task <(HttpResponseMessage Response, ProductResponse? Product)> CreateTestProductAsync(string name, string sku, decimal price, int stockQuantity, string category)
    {
        var payload = new 
        { 
            name, sku, price, stockQuantity, category
        };
        var response = await _client.PostAsJsonAsync("/api/Products", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return (response, product);
    }
}
