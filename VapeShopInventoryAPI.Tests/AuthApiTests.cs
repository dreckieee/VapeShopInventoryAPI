using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

public class AuthApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _ownerClient = null!;
    private HttpClient _cashierClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _ownerClient = _factory.CreateOwnerClient();
        _cashierClient = _factory.CreateCashierClient();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        _client.Dispose();
        _ownerClient.Dispose();
        _cashierClient.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetCurrentUser_OwnerClient_ReturnsOk()
    {
        var response = await _ownerClient.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        Assert.That(currentUser, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(currentUser.Id, Is.GreaterThan(0));
            Assert.That(currentUser.Username, Is.EqualTo("test-owner-username"));
            Assert.That(currentUser.Role, Is.EqualTo("Owner"));
        });
    }

    [Test]
    public async Task GetCurrentUser_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/Auth/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        Assert.That(currentUser, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(currentUser.Id, Is.GreaterThan(0));
            Assert.That(currentUser.Username, Is.EqualTo("test-cashier-username"));
            Assert.That(currentUser.Role, Is.EqualTo("Cashier"));
        });
    }
}
