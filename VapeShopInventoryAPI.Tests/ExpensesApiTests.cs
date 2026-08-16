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