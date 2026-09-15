using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace VapeShopInventoryAPI.Tests;

public class CapitalTransactionsApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdCapitalTransactionIds = new ();
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
    public async Task CreateCapitalTransaction_DepositValidData_ReturnsCreated()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction (deposit) with all valid data",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode} instead.");

        var capitalTransaction = await response.Content.ReadFromJsonAsync<CapitalTransactionResponse>(TestJsonOptions.Default);
        Assert.That(capitalTransaction, Is.Not.Null);
        _createdCapitalTransactionIds.Add(capitalTransaction.Id);

        Assert.That(capitalTransaction.Type, Is.EqualTo(payload.Type));
        Assert.That(capitalTransaction.Amount, Is.EqualTo(payload.Amount));
        Assert.That(capitalTransaction.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(capitalTransaction.Note, Is.EqualTo(payload.Note));
        Assert.That(capitalTransaction.Date, Is.EqualTo(payload.Date));
    }

    [Test]
    public async Task CreateCapitalTransaction_WithdrawalValidData_ReturnsCreated()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Withdrawal,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction (withdrawal) with all valid data",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode} instead.");

        var capitalTransaction = await response.Content.ReadFromJsonAsync<CapitalTransactionResponse>(TestJsonOptions.Default);
        Assert.That(capitalTransaction, Is.Not.Null);
        _createdCapitalTransactionIds.Add(capitalTransaction.Id);

        Assert.That(capitalTransaction.Type, Is.EqualTo(payload.Type));
        Assert.That(capitalTransaction.Amount, Is.EqualTo(payload.Amount));
        Assert.That(capitalTransaction.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(capitalTransaction.Note, Is.EqualTo(payload.Note));
        Assert.That(capitalTransaction.Date, Is.EqualTo(payload.Date));
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidNegativeAmount_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = -99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction (deposit) with invalid negative amount",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidZeroAmount_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 0m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction (deposit) with invalid zero amount",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidReceivablePaymentMethod_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Receivable,
            Note = "Test note for creating capital transaction (deposit) with invalid receivable payment method",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidPayablePaymentMethod_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Payable,
            Note = "Test note for creating capital transaction (deposit) with invalid payable payment method",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidUndefinedPaymentMethod_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = (PaymentMethod)999,
            Note = "Test note for creating capital transaction (deposit) with invalid undefined payment method",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_InvalidUndefinedType_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = (CapitalTransactionType)999,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction with invalid undefined type",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateCapitalTransaction_DepositInvalidDefaultDate_ReturnsBadRequest()
    {
        
        var payload = new CreateCapitalTransactionRequest
        {
            Type = CapitalTransactionType.Deposit,
            Amount = 99.99m,
            PaymentMethod = PaymentMethod.Cash,
            Note = "Test note for creating capital transaction (deposit) with invalid default date",
            Date = default
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    public async Task<(HttpResponseMessage Response, CapitalTransactionResponse CapitalTransaction)> CreateTestCapitalTransactionAsync(
        CapitalTransactionType type = CapitalTransactionType.Deposit, 
        decimal amount = 99.99m, 
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        string? note = null,
        DateTime? date = null)
    {
        var payload = new CreateCapitalTransactionRequest
        {
            Type = type,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Note = note,
            Date = date ?? new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/CapitalTransactions", payload);
        if(response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status, but received {response.StatusCode}");
        }

        var capitalTransaction = await response.Content.ReadFromJsonAsync<CapitalTransactionResponse>(TestJsonOptions.Default);
        if (capitalTransaction == null)
        {
            throw new InvalidOperationException($"Failed to deserialize CapitalTransactionResponse after creating test capital transaction");
        }

        _createdCapitalTransactionIds.Add(capitalTransaction.Id);
        return (response, capitalTransaction);
    }


    [TearDown]
    public async Task DeleteTestCapitalTransaction()
    {
        //Test Capital Transaction Cleanup
        if(_createdCapitalTransactionIds.Count > 0)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

            foreach(int ct in _createdCapitalTransactionIds)
            {
                var capitalTransaction = await context.CapitalTransactions.FindAsync(ct);
                if (capitalTransaction != null)
                {
                    context.CapitalTransactions.Remove(capitalTransaction);
                }
            }
            await context.SaveChangesAsync();
        }
        _createdCapitalTransactionIds.Clear();
    }
}