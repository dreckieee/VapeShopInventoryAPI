using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

public class CatalogApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _ownerClient = null!;
    private List<int> _createdProductIds = new();

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _ownerClient = _factory.CreateOwnerClient();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        _client.Dispose();
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetCatalogProduct_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"api/Catalog/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCatalogProduct_ExistingId_ReturnsOk()
    {
        string name = "Test Get Catalog Product";
        decimal price = 10m;
        int stockQuantity = 1;
        int lowStockLevel = 0;
        string category = "Test";

        var seededProduct = await CreateTestProductAsync(name: name, price: price, stockQuantity: stockQuantity, lowStockLevel: lowStockLevel, category: category);

        var response = await _client.GetAsync($"api/Catalog/{seededProduct.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var product = await response.Content.ReadFromJsonAsync<PublicProductResponse>(TestJsonOptions.Default);
        Assert.That(product, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(product.Id, Is.EqualTo(seededProduct.Id));
            Assert.That(product.Name, Is.EqualTo(name));
            Assert.That(product.Price, Is.EqualTo(price));
            Assert.That(product.IsInStock, Is.True);
            Assert.That(product.Category, Is.EqualTo(category));
        });
    }

    [Test]
    public async Task GetCatalog_ProductsWithAndWithoutStock_ReturnsCorrectIsInStockFlags()
    {
        string inStockName = "Test Catalog List In Stock";
        int inStockQuantity = 1;

        var inStockProduct = await CreateTestProductAsync(name: inStockName, stockQuantity: inStockQuantity);

        string outOfStockName = "Test Catalog List Out Of Stock";
        int outOfStockQuantity = 0;

        var outOfStockProduct = await CreateTestProductAsync(name: outOfStockName, stockQuantity: outOfStockQuantity);

        var response = await _client.GetAsync("api/Catalog");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var catalog = await response.Content.ReadFromJsonAsync<List<PublicProductResponse>>(TestJsonOptions.Default);
        Assert.That(catalog, Is.Not.Null);

        var inStockRow = catalog.Find(p => p.Id == inStockProduct.Id);
        Assert.That(inStockRow, Is.Not.Null);

        var outOfStockRow = catalog.Find(p => p.Id == outOfStockProduct.Id);
        Assert.That(outOfStockRow, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(inStockRow.IsInStock, Is.True);
            Assert.That(outOfStockRow.IsInStock, Is.False);
        });
    }

    [Test]
    public async Task GetCatalogProduct_ExistingId_ReturnsOnlyPublicFields()
    {
        var seededProduct = await CreateTestProductAsync();

        var response = await _client.GetAsync($"api/Catalog/{seededProduct.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var responseBody = await response.Content.ReadAsStringAsync();
        using var parsedBody = JsonDocument.Parse(responseBody);
        List<string> names = parsedBody.RootElement
            .EnumerateObject()
            .Select(p => p.Name)
            .ToList();

        Assert.That(names, Is.EquivalentTo(new[]
        {
            "id",
            "name",
            "price",
            "isInStock",
            "category"
        }));
    }

    [Test]
    public async Task GetCatalog_NameFilterDifferentCase_ReturnsOnlyMatchingProducts()
    {
        string product1name = Guid.NewGuid().ToString();
        string escapedProduct1name = Uri.EscapeDataString(product1name.ToUpper());
        var product1 = await CreateTestProductAsync(name: product1name);

        string product2name = "Test Get Catalog Product";
        var product2 = await CreateTestProductAsync(name: product2name);

        var response = await _client.GetAsync($"api/Catalog?name={escapedProduct1name}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var products = await response.Content.ReadFromJsonAsync<List<PublicProductResponse>>(TestJsonOptions.Default);
        Assert.That(products, Is.Not.Null);

        var found = products.Find(p => p.Id == product1.Id);
        var notFound = products.Find(p => p.Id == product2.Id);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.Not.Null);
            Assert.That(notFound, Is.Null);
        });
    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
        foreach (int i in _createdProductIds)
        {
            var response = await _ownerClient.DeleteAsync($"api/Products/{i}");
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
        _createdProductIds.Clear();
    }

    private async Task<ProductResponse> CreateTestProductAsync(string name = "Test Product", string? sku = null, decimal price = 99.99m, int stockQuantity = 10, int lowStockLevel = 3, string category = "Test")
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

        var response = await _ownerClient.PostAsJsonAsync("api/Products", payload);
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

        return product;
    }
}
