using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.SafeHandles;
using NUnit.Framework.Internal;

namespace VapeShopInventoryAPI.Tests;

public class SettlementsApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdExpenseIds = new ();
    private List<int> _createdSaleIds = new ();
    private List<int> _createdSettlementIds = new ();
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
    public async Task CreateSettlement_ValidSaleSettlement_ReturnsCreated()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (valid) existing sale",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode} instead.");

        var settlement = await response.Content.ReadFromJsonAsync<SettlementResponse>(TestJsonOptions.Default);
        Assert.That(settlement, Is.Not.Null);
        _createdSettlementIds.Add(settlement.Id);

        Assert.That(settlement.SaleId, Is.EqualTo(payload.SaleId));
        Assert.That(settlement.ExpenseId, Is.EqualTo(payload.ExpenseId));
        Assert.That(settlement.Amount, Is.EqualTo(payload.Amount));
        Assert.That(settlement.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(settlement.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(settlement.Date, Is.EqualTo(payload.Date));
    }

    [Test]
    public async Task CreateSettlement_NonExistentSaleId_ReturnsBadRequest()
    {
        var payload = new CreateSettlementRequest
        {
            SaleId = int.MaxValue,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) non-existent sale id",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_SaleNotReceivable_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync(paymentMethod: PaymentMethod.Cash);

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) sale not receivable",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_ValidExpenseSettlement_ReturnsCreated()
    {
        var (_, testExpense) = await CreateTestExpenseAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = testExpense.Id,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (valid) existing expense",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode} instead.");

        var settlement = await response.Content.ReadFromJsonAsync<SettlementResponse>(TestJsonOptions.Default);
        Assert.That(settlement, Is.Not.Null);
        _createdSettlementIds.Add(settlement.Id);

        Assert.That(settlement.SaleId, Is.EqualTo(payload.SaleId));
        Assert.That(settlement.ExpenseId, Is.EqualTo(payload.ExpenseId));
        Assert.That(settlement.Amount, Is.EqualTo(payload.Amount));
        Assert.That(settlement.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(settlement.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(settlement.Date, Is.EqualTo(payload.Date));
    }

    [Test]
    public async Task CreateSettlement_NonExistentExpenseId_ReturnsBadRequest()
    {
        var payload = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = int.MaxValue,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) non-existent expense id",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_ExpenseNotPayable_ReturnsBadRequest()
    {
        var (_, testExpense) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Cash);

        var payload = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = testExpense.Id,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) expense not payable",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_NeitherFkSet_ReturnsBadRequest()
    {
        var payload = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) neither FK set sale/expense id",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_BothFkSet_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();
        var (_, testExpense) = await CreateTestExpenseAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = testExpense.Id,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) both FK set sale/expense id",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_NonPositiveAmount_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = -1,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) non-positive amount",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_InvalidPaymentMethod_ReturnsBadRequest()
    {
        var (_, testExpense) = await CreateTestExpenseAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = testExpense.Id,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Receivable,
            PaymentNote = "Test payment note for create settlement test (invalid) payment method not cash/digital payment",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_FutureDate_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) future date",
            Date = DateTime.Now.AddYears(100)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    public async Task<(HttpResponseMessage Response, ExpenseResponse Expense)> CreateTestExpenseAsync(
        PaymentMethod paymentMethod = PaymentMethod.Payable, 
        string? paymentNote = null, 
        string description = "Test expense description", 
        decimal amount = 99.99m, 
        string category = "Test Expense Category",
        DateTime? date = null)
    {
        var payload = new CreateExpenseRequest
        {
            PaymentMethod = paymentMethod,
            PaymentNote = paymentNote,
            Description = description,
            Amount = amount,
            Category = category,
            Date = date ?? DateTime.Now
        };

        var response = await _client.PostAsJsonAsync("api/Expenses", payload);
        if(response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status, but received {response.StatusCode}");
        }

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>(TestJsonOptions.Default);
        if (expense == null)
        {
            throw new InvalidOperationException($"Failed to deserialize ExpenseResponse after creating test expense");
        }

        _createdExpenseIds.Add(expense.Id);
        return (response, expense);
    }

    private async Task <(HttpResponseMessage Response, SaleResponse Sale)> CreateTestSaleAsync(
        DateTime? saleDate = null, 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Receivable)
    {
        var payload = new { 
        SaleDate = saleDate ?? DateTime.Now, 
        PaymentMethod = paymentMethod, 
        PaymentNote = paymentNote 
        };
        
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        if(response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status, but received {response.StatusCode}");
        }

        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        if (sale == null)
        {
            throw new InvalidOperationException($"Failed to deserialize SaleResponse after creating test sale");
        }

        _createdSaleIds.Add(sale.Id);
        return (response, sale);
    }

    [TearDown]
    public async Task DeleteTestExpenseSaleSettlement()
    {
        //Test Settlement Cleanup
        if(_createdSettlementIds.Count > 0)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

            foreach(int i in _createdSettlementIds)
            {
                var settlement = await context.Settlements.FindAsync(i);
                if (settlement != null)
                {
                    context.Settlements.Remove(settlement);
                }
            }
            await context.SaveChangesAsync();
        }

        //Test Expense Cleanup
        if(_createdExpenseIds.Count > 0)
        {
            foreach(int i in _createdExpenseIds)
            {
                var response = await _client.DeleteAsync($"api/Expenses/{i}");
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    TestContext.Progress.WriteLine($"Skipped expense cleanup: expense {i} has existing reference (to a delivery item or delivery items) - deletion blocked by design (audit trail preserved).");
                }
                else if(response.StatusCode == HttpStatusCode.NotFound)
                {
                    TestContext.Progress.WriteLine($"Expense with an Id of {i} cannot be found -- already deleted or does not exist.");
                }
                else if (response.StatusCode != HttpStatusCode.NoContent)
                { 
                    TestContext.Progress.WriteLine($"Warning: Failure in deleting an expense with an Id of {i}: Expected 204 No Content() status, but received {response.StatusCode}");
                }
            }
        }

        //Test Sale Cleanup
        if(_createdSaleIds.Count > 0)
        {
            foreach(int i in _createdSaleIds)
            {
                var responseGetSale = await _client.GetAsync($"/api/Sales/{i}");
                if(responseGetSale.StatusCode == HttpStatusCode.NotFound)
                {
                    TestContext.Progress.WriteLine($"Sale with an Id of {i} cannot be found -- already deleted or does not exist.");
                    continue;
                }
                
                try
                {
                    var sale = await responseGetSale.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
                    if (sale!.IsClosed)
                    {
                        TestContext.Progress.WriteLine($"Skipped sale cleanup: sale {i} was closed — cancellation blocked by design (audit trail preserved).");
                    }
                    else
                    {
                        var response = await _client.PutAsync($"/api/Sales/{i}/cancel", null);
                        if(response.StatusCode != HttpStatusCode.NoContent)
                        {
                            throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestContext.Progress.WriteLine($"Warning: Failure in cancelling a sale with an Id of {i}: {ex.Message}");
                }
            } 
        }
        _createdSettlementIds.Clear();
        _createdExpenseIds.Clear();
        _createdSaleIds.Clear();
    }
}