using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;

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

    [Test]
    public async Task GetCapitalTransactions_CashierClient_ReturnsForbidden()
    {
        var response = await _cashierClient.GetAsync("api/CapitalTransactions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Expected 403 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCapitalTransaction_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"api/CapitalTransactions/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCapitalTransaction_CashierClient_ReturnsForbidden()
    {
        var response = await _cashierClient.GetAsync($"api/CapitalTransactions/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Expected 403 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_AnonymousClient_ReturnsUnauthorized()
    {
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_CashierClient_ReturnsForbidden()
    {
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _cashierClient.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Expected 403 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCashBalance_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/CashBalance");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCashBalance_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/CashBalance");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCashBalanceHistory_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/CashBalance/history?days=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetCashBalanceHistory_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/CashBalance/history?days=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetProducts_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/Products");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetProducts_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/Products");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task DeleteProduct_CashierClient_ReturnsForbidden()
    {
        var response = await _cashierClient.DeleteAsync($"api/Products/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Expected 403 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetSales_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/Sales");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetSales_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/Sales");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task AddSaleItem_AnonymousClient_ReturnsUnauthorized()
    {
        var payload = new AddSaleItemRequest
        {
            ProductId = 1,
            Quantity = 1,
            UnitPriceAtSale = 10m
        };

        var response = await _client.PostAsJsonAsync($"api/SaleItems/{int.MaxValue}/items", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task AddSaleItem_CashierClientUnknownSale_ReturnsNotFound()
    {
        var payload = new AddSaleItemRequest
        {
            ProductId = 1,
            Quantity = 1,
            UnitPriceAtSale = 10m
        };

        var response = await _cashierClient.PostAsJsonAsync($"api/SaleItems/{int.MaxValue}/items", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetSettlements_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/Settlements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetSettlements_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/Settlements");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetExpenses_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/Expenses");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetExpenses_CashierClient_ReturnsOk()
    {
        var response = await _cashierClient.GetAsync("api/Expenses");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task Restock_AnonymousClient_ReturnsUnauthorized()
    {
        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest { ProductId = int.MaxValue, Quantity = 1, UnitCost = 10m }
            },
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task Restock_CashierClientUnknownProduct_ReturnsNotFound()
    {
        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest { ProductId = int.MaxValue, Quantity = 1, UnitCost = 10m }
            },
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _cashierClient.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetIncome_AnonymousClient_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/Income");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }
}
