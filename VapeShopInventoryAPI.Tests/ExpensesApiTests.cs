using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

public class ExpensesApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdExpenseIds = new ();

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
    public async Task CreateExpense_WithValidData_ReturnsCreatedExpense()
    {
        var payload = new CreateExpenseRequest
        {
            PaymentMethod = PaymentMethod.DigitalPayment,
            PaymentNote = "Testing Digital Payment Method",
            Description = "Test description for create expense test",
            Amount = 99.75m,
            Category = "Test Category for Expense Creation",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Expenses", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {response.StatusCode} instead.");

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        _createdExpenseIds.Add(expense.Id);

        Assert.That(expense.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(payload.Description));
        Assert.That(expense.Amount, Is.EqualTo(payload.Amount));
        Assert.That(expense.Category, Is.EqualTo(payload.Category));
        Assert.That(expense.Date, Is.EqualTo(payload.Date));
    }
    
    [Test]
    public async Task CreateExpense_WithInvalidData_ReturnsBadRequest()
    {
        var responseExpensesBefore = await _client.GetAsync("api/Expenses");
        var expensesBefore = await responseExpensesBefore.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        var payload = new CreateExpenseRequest
        {
            PaymentMethod = PaymentMethod.DigitalPayment,
            PaymentNote = "Testing Digital Payment Method",
            Description = "Test description for create expense test",
            Amount = -99.75m,
            Category = "Test Category for Expense Creation",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Expenses", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");

        var responseExpensesAfter = await _client.GetAsync("api/Expenses");
        var expensesAfter = await responseExpensesAfter.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesBefore?.Count, Is.EqualTo(expensesAfter?.Count));
        
    }
    

    public async Task<(HttpResponseMessage Response, ExpenseResponse Expense)> CreateTestExpense(
    PaymentMethod paymentMethod = PaymentMethod.Cash, 
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

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        if (expense == null)
        {
            throw new InvalidOperationException($"Failed to deserialize ExpenseResponse after creating test expense");
        }

        _createdExpenseIds.Add(expense.Id);
        return (response, expense);
    }

    [TearDown]
    public async Task DeleteTestExpense()
    {
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
        _createdExpenseIds.Clear();
    }
}