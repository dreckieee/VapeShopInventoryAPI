using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Net;
using System.Text.Json;

namespace VapeShopInventoryAPI.Tests;

public class ProductsApiTests : PlaywrightTest
{
    private const string BaseUrl = "http://localhost:5208";
    [Test]
    public async Task GetProducts_ReturnsSuccessAndNonEmptyList()
    {
        await using IAPIRequestContext apiContext = await Playwright.APIRequest.NewContextAsync(new ()
        {
            BaseURL = BaseUrl
        });   

        var response = await apiContext.GetAsync("/api/Products");
        Assert.That(response.Ok, Is.True, $"Expected success status, got {response.Status}");

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
        Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.NotFound), $"Expected not found status, got {response.Status}");
    }
}
