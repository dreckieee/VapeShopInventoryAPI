using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

public class ProductsApiTests 
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdProductIds = new();

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
        int validLowStockLevel = 1;
        string validCategory = "Test";

        var (_, product) = await CreateTestProduct(validName,validSku,validPrice,validStockQuantity,validLowStockLevel, validCategory);
        Assert.That(product, Is.Not.Null);

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
        int validLowStockLevel = 1;
        string validCategory = "Test";

        var (response, product) = await CreateTestProduct(validName, validSku, validPrice, validStockQuantity, validLowStockLevel, validCategory);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode}");
        
        Assert.That(product, Is.Not.Null);
        Assert.That(product.Name, Is.EqualTo(validName));
        Assert.That(product.Sku, Is.EqualTo(validSku));
        Assert.That(product.Price, Is.EqualTo(validPrice));
        Assert.That(product.StockQuantity, Is.EqualTo(validStockQuantity));
        Assert.That(product.Category, Is.EqualTo(validCategory));
    }

    [Test]
    public async Task CreateProduct_InvalidProduct_ReturnsBadRequest()
    {
        var responseGetProductsBefore = await _client.GetAsync("api/Products");
        var productsBefore = await responseGetProductsBefore.Content.ReadFromJsonAsync<List<ProductResponse>>();

        var invalidProductPayload = new
        {
            name = "TestCreateInvalidProduct",
            sku = "0a0a2c",
            price = 99.99m,
            stockQuantity = -1,
            lowStockLevel = 1,
            category = "Test"
        };
        var response = await _client.PostAsJsonAsync("/api/Products", invalidProductPayload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode}");

        var responseGetProductsAfter = await _client.GetAsync("api/Products");
        var productsAfter = await responseGetProductsAfter.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.That(productsBefore?.Count, Is.EqualTo(productsAfter?.Count));
    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
        if(_createdProductIds.Count > 0)
        {
            foreach(int i in _createdProductIds)
            {
                var response = await _client.DeleteAsync($"api/Products/{i}");
                if (response.StatusCode != HttpStatusCode.NoContent)
                { 
                    TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with an Id of {i}: Expected 204 No Content() status, but received {response.StatusCode}");
                }
            }
        }
        _createdProductIds.Clear();
    }

    public async Task<(HttpResponseMessage Response, ProductResponse Product)> CreateTestProduct(string name = "Test Product", string? sku = null, decimal price = 99.99m, int stockQuantity = 10, int lowStockLevel = 3, string category = "Test")
    {
        var payload = new
        {
            Name = name,
            Sku = sku ?? Guid.NewGuid().ToString(),
            Price = price,
            StockQuantity = stockQuantity,
            LowStockLevel = lowStockLevel,
            Category = category
        };

        var response = await _client.PostAsJsonAsync("api/Products", payload);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status in creating test product (setup helper), but received {response.StatusCode}");
        }

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        if (product == null)
        {
            throw new InvalidOperationException($"Product is null in creating test product (setup helper) but expected otherwise");
        }
        _createdProductIds.Add(product.Id);

        return (response, product);
    }
}
