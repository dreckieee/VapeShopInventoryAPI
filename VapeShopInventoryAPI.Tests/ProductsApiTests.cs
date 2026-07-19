using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Net;
using System.Text.Json;

namespace VapeShopInventoryAPI.Tests;

public class ProductsApiTests : PlaywrightTest
{
    private int? _createdProductId;
    private const string BaseUrl = "http://localhost:5208";
    [Test]
    public async Task GetProducts_ReturnsSuccessAndNonEmptyList()
    {
        await using IAPIRequestContext apiContext = await Playwright.APIRequest.NewContextAsync(new ()
        {
            BaseURL = BaseUrl
        });   

        var response = await apiContext.GetAsync("/api/Products");
        Assert.That(response.Ok, Is.True, $"Expected 200 Ok() status, but received {response.Status}");

        var responseBody = await response.TextAsync();
        var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        var products = JsonSerializer.Deserialize<List<Product>>(responseBody, options);
        Assert.That(products, Is.Not.Null);
        Assert.That(products.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetProduct_NonExistentId_ReturnsNotFound()
    {
        int testId = -1;
        await using IAPIRequestContext apiContext = await Playwright.APIRequest.NewContextAsync(new ()
        {
            BaseURL = BaseUrl
        });   

        var response = await apiContext.GetAsync($"/api/Products/{testId}");
        Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.Status}");
    }

    [Test]
    public async Task CreateProduct_ValidProduct_ReturnsCreated()
    {
        await using IAPIRequestContext apiContext = await Playwright.APIRequest.NewContextAsync(new ()
        {
            BaseURL = BaseUrl
        });   

        var newProductPayLoad = new
        {
            name = "TestProduct",
            sku = "test001",
            price = 99.99m,
            stockQuantity = 99,
            category = "Test"
        };
        var response = await apiContext.PostAsync("/api/Products", new APIRequestContextOptions
        {
            DataObject = newProductPayLoad
        });
        Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.Status}");

        var responseBody = await response.TextAsync();
        var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        var product = JsonSerializer.Deserialize<Product>(responseBody, options);
        
        Assert.That(product, Is.Not.Null);
        _createdProductId = product.Id;
        
        Assert.That(product.Name, Is.EqualTo(newProductPayLoad.name));
        Assert.That(product.Sku, Is.EqualTo(newProductPayLoad.sku));
        Assert.That(product.Price, Is.EqualTo(newProductPayLoad.price));
        Assert.That(product.StockQuantity, Is.EqualTo(newProductPayLoad.stockQuantity));
        Assert.That(product.Category, Is.EqualTo(newProductPayLoad.category));

    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
        if (_createdProductId != null)
        {
            try
            {
                await using IAPIRequestContext apiContext = await Playwright.APIRequest.NewContextAsync(new ()
                {
                    BaseURL = BaseUrl
                });
                var response = await apiContext.DeleteAsync($"/api/Products/{_createdProductId}");
                if(response.Status != (int)HttpStatusCode.NoContent)
                {
                    throw new Exception($"Expected 204 NoContent() status, but received {response.Status}");
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
}
