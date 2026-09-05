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

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetProduct_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Products/{int.MaxValue}");
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
        var productsBefore = await responseGetProductsBefore.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);

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
        var productsAfter = await responseGetProductsAfter.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(productsBefore?.Count, Is.EqualTo(productsAfter?.Count));
    }

    [Test]
    public async Task FilterProducts_FromQueryName_ReturnsOk()
    {
        var responseBefore = await _client.GetAsync("/api/Products?name=blue");
        Assert.That(responseBefore.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {responseBefore.StatusCode}");
        var productsBefore = await responseBefore.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);

        var (_, productA) = await CreateTestProduct(name: "Blue Razz Vape Juice", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Liquids");
        var (_, productB) = await CreateTestProduct(name: "Blue Coil Kit", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Hardware");
        var (_, productC) = await CreateTestProduct(name: "Mint Vape Juice", price: 399.99m, stockQuantity: 30, lowStockLevel: 9, category: "Liquids");
        var (_, productD) = await CreateTestProduct(name: "Battery Charger", price: 499.99m, stockQuantity: 40, lowStockLevel: 12, category: "Hardware");
        var createdProducts = new List<ProductResponse> { productA, productB, productC, productD };

        var response = await _client.GetAsync("/api/Products?name=blue");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {response.StatusCode}");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.EqualTo(productsBefore?.Count + 2));
        Assert.That(products.Any(p => p.Name == productA.Name), Is.True);
        Assert.That(products.Any(p => p.Name == productB.Name), Is.True);
        Assert.That(products.Any(p => p.Name == productC.Name), Is.False);
        Assert.That(products.Any(p => p.Name == productD.Name), Is.False);
    }

    [Test]
    public async Task FilterProducts_FromQueryCategory_ReturnsOk()
    {
        var responseBefore = await _client.GetAsync("/api/Products?category=liquids");
        Assert.That(responseBefore.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {responseBefore.StatusCode}");
        var productsBefore = await responseBefore.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);

        var (_, productA) = await CreateTestProduct(name: "Blue Razz Vape Juice", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Liquids");
        var (_, productB) = await CreateTestProduct(name: "Blue Coil Kit", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Hardware");
        var (_, productC) = await CreateTestProduct(name: "Mint Vape Juice", price: 399.99m, stockQuantity: 30, lowStockLevel: 9, category: "Liquids");
        var (_, productD) = await CreateTestProduct(name: "Battery Charger", price: 499.99m, stockQuantity: 40, lowStockLevel: 12, category: "Hardware");
        var createdProducts = new List<ProductResponse> { productA, productB, productC, productD };

        var response = await _client.GetAsync("/api/Products?category=liquids");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {response.StatusCode}");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.EqualTo(productsBefore?.Count + 2));
        Assert.That(products.Any(p => p.Name == productA.Name), Is.True);
        Assert.That(products.Any(p => p.Name == productB.Name), Is.False);
        Assert.That(products.Any(p => p.Name == productC.Name), Is.True);
        Assert.That(products.Any(p => p.Name == productD.Name), Is.False);
    }

    [Test]
    public async Task FilterProducts_FromQueryNameAndCategory_ReturnsOk()
    {
        var responseBefore = await _client.GetAsync("/api/Products?name=blue&category=hardware");
        Assert.That(responseBefore.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {responseBefore.StatusCode}");
        var productsBefore = await responseBefore.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);

        var (_, productA) = await CreateTestProduct(name: "Blue Razz Vape Juice", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Liquids");
        var (_, productB) = await CreateTestProduct(name: "Blue Coil Kit", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Hardware");
        var (_, productC) = await CreateTestProduct(name: "Mint Vape Juice", price: 399.99m, stockQuantity: 30, lowStockLevel: 9, category: "Liquids");
        var (_, productD) = await CreateTestProduct(name: "Battery Charger", price: 499.99m, stockQuantity: 40, lowStockLevel: 12, category: "Hardware");
        var createdProducts = new List<ProductResponse> { productA, productB, productC, productD };

        var response = await _client.GetAsync("/api/Products?name=blue&category=hardware");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {response.StatusCode}");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.EqualTo(productsBefore?.Count + 1));
        Assert.That(products.Any(p => p.Name == productA.Name), Is.False);
        Assert.That(products.Any(p => p.Name == productB.Name), Is.True);
        Assert.That(products.Any(p => p.Name == productC.Name), Is.False);
        Assert.That(products.Any(p => p.Name == productD.Name), Is.False);
    }

    [Test]
    public async Task FilterProducts_FromQueryNonExistentName_ReturnsOkEmptyList()
    {
        var response = await _client.GetAsync("/api/Products?name=nonexistentproduct12345");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() Status, but received {response.StatusCode}");

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.EqualTo(0));
    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
        if(_createdProductIds.Count > 0)
        {
            foreach(int i in _createdProductIds)
            {
                var response = await _client.DeleteAsync($"api/Products/{i}");
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    TestContext.Progress.WriteLine($"Skipped product cleanup: product with id {i} has existing reference(s) — deletion blocked by design (audit trail preserved).");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TestContext.Progress.WriteLine($"Product with an Id of {i} cannot be found -- already deleted or does not exist.");
                }
                else if (response.StatusCode != HttpStatusCode.NoContent)
                { 
                    TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with an Id of {i}: Expected 204 No Content() status, but received {response.StatusCode}.");
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

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(TestJsonOptions.Default);
        if (product == null)
        {
            throw new InvalidOperationException($"Product is null in creating test product (setup helper) but expected otherwise");
        }
        _createdProductIds.Add(product.Id);

        return (response, product);
    }
}
