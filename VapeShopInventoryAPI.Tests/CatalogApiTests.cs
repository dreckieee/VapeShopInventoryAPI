using System.Net;

namespace VapeShopInventoryAPI.Tests;

public class CatalogApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

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
    public async Task GetCatalogProduct_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"api/Catalog/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 status, but received {response.StatusCode} instead.");
    }
}
