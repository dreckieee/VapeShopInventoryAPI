using System.Net;
using System.Net.Http.Json;

namespace VapeShopInventoryAPI.Tests;

public class ProductsApiTests 
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int? _createdProductId;

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
    public async Task GetProducts_ReturnsSuccessAndNonEmptyList()
    {
        string validName = "TestGetProducts"; 
        string validSku = "0a0a1b"; 
        decimal validPrice = 99.99m;
        int validStockQuantity = 9;
        string validCategory = "Test";

        var (responseCreate, product) = await CreateTestProductAsync(validName,validSku,validPrice,validStockQuantity,validCategory);
        Assert.That(product, Is.Not.Null);
        _createdProductId = product.Id;

        var response = await _client.GetAsync("/api/Products");
        Assert.That(response.IsSuccessStatusCode, Is.True, $"Expected 200 Ok() status, but received {response.StatusCode}");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetProduct_NonExistentId_ReturnsNotFound()
    {
        int testInvalidId = -1;
        var response = await _client.GetAsync($"/api/Products/{testInvalidId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.StatusCode}");
    }

    [Test]
    public async Task CreateProduct_ValidProduct_ReturnsCreated()
    {        
        string validName = "TestCreateValidProduct"; 
        string validSku = "0a0a2b"; 
        decimal validPrice = 99.99m;
        int validStockQuantity = 9;
        string validCategory = "Test";

        var (response, product) = await CreateTestProductAsync(validName,validSku,validPrice,validStockQuantity,validCategory);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode}");
        
        Assert.That(product, Is.Not.Null);
        _createdProductId = product.Id;
        
        Assert.That(product.Name, Is.EqualTo(validName));
        Assert.That(product.Sku, Is.EqualTo(validSku));
        Assert.That(product.Price, Is.EqualTo(validPrice));
        Assert.That(product.StockQuantity, Is.EqualTo(validStockQuantity));
        Assert.That(product.Category, Is.EqualTo(validCategory));
    }

    [Test]
    public async Task CreateProduct_InvalidProduct_ReturnsBadRequest()
    {
        var newInvalidProductPayload = new
        {
            name = "TestCreateInvalidProduct",
            sku = "0a0a2c",
            price = 99.99m,
            stockQuantity = -1,
            category = "Test"
        };
        var response = await _client.PostAsJsonAsync("/api/Products", newInvalidProductPayload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode}");
    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
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

    private async Task <(HttpResponseMessage Response, ProductResponse? Product)> CreateTestProductAsync(string name, string sku, decimal price, int stockQuantity, string category)
    {
        var payload = new 
        { 
            name, sku, price, stockQuantity, category
        };
        var response = await _client.PostAsJsonAsync("/api/Products", payload);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return (response, product);
    }
}
