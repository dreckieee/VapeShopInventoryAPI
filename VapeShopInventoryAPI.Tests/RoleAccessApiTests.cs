using System.Net;

namespace VapeShopInventoryAPI.Tests;

public class RoleAccessApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _cashierClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _cashierClient = _factory.CreateCashierClient();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        _client.Dispose();
        _cashierClient.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetCapitalTransactions_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/CapitalTransactions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }
}
