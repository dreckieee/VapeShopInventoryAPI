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
    private List<int> _createdProductIds = new ();
    private List<int> _restockedProductIds = new ();
    

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

    [Test]
    public async Task CreateExpense_WithInvalidEnumPaymentMethod_ReturnsBadRequest()
    {
        var payload = new CreateExpenseRequest
        {
            PaymentMethod = (PaymentMethod)999,
            PaymentNote = "Testing Enum Payment Method",
            Description = "Test description for create expense test",
            Amount = 99.75m,
            Category = "Test Category for Expense Creation with invalid Enum Payment Method",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Expenses", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");

        var responseGetExpensesAfter = await _client.GetAsync("api/Expenses");
        var expensesAfter = await responseGetExpensesAfter.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesAfter, Is.Not.Null);
        Assert.That(expensesAfter.Any(e => e.Category == payload.Category), Is.False);
    }
    
    [Test]
    public async Task GetExpense_WithExistingId_ReturnsOk()
    {
        var (_, testExpense) = await CreateTestExpense();

        var response = await _client.GetAsync($"api/Expenses/{testExpense.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(testExpense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(testExpense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(testExpense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(testExpense.Description));
        Assert.That(expense.Amount, Is.EqualTo(testExpense.Amount));
        Assert.That(expense.Category, Is.EqualTo(testExpense.Category));
        Assert.That(expense.Date, Is.EqualTo(testExpense.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(testExpense.CreatedAt));
    }

    [Test]
    public async Task GetExpense_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"api/Expenses/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 Not Found() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task GetExpenses_FilterByYear_ReturnsOnlyMatchingYear()
    {
        var (_, testExpense1) = await CreateTestExpense(paymentMethod: PaymentMethod.DigitalPayment, paymentNote: "payment note for test expense 1", description: "description for test expense 1", amount: 99.99m, category: "Test Expense 1 Category", date: new DateTime(2026, 01, 01));
        var (_, testExpense2) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 2", description: "description for test expense 2", amount: 199.99m, category: "Test Expense 2 Category", date: new DateTime(2025, 02, 02));
        var (_, testExpense3) = await CreateTestExpense(paymentMethod: PaymentMethod.Payable, paymentNote: "payment note for test expense 3", description: "description for test expense 3", amount: 299.99m, category: "Test Expense 3 Category", date: new DateTime(2026, 03, 03));
        var (_, testExpense4) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 4", description: "description for test expense 4", amount: 399.99m, category: "Test Expense 4 Category", date: new DateTime(2024, 04, 04));

        int year = 2026;
        var response = await _client.GetAsync($"api/Expenses?year={year}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expenses, Is.Not.Null);
        Assert.That(expenses.All(e => e.Date.Year == year), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense1.Id), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense2.Id), Is.False);
        Assert.That(expenses.Any(e => e.Id == testExpense3.Id), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense4.Id), Is.False);
    }

    [Test]
    public async Task GetExpenses_FilterByMonth_ReturnsOnlyMatchingMonth()
    {
        var (_, testExpense1) = await CreateTestExpense(paymentMethod: PaymentMethod.DigitalPayment, paymentNote: "payment note for test expense 1", description: "description for test expense 1", amount: 99.99m, category: "Test Expense 1 Category", date: new DateTime(2026, 01, 01));
        var (_, testExpense2) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 2", description: "description for test expense 2", amount: 199.99m, category: "Test Expense 2 Category", date: new DateTime(2025, 02, 02));
        var (_, testExpense3) = await CreateTestExpense(paymentMethod: PaymentMethod.Payable, paymentNote: "payment note for test expense 3", description: "description for test expense 3", amount: 299.99m, category: "Test Expense 3 Category", date: new DateTime(2024, 03, 03));
        var (_, testExpense4) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 4", description: "description for test expense 4", amount: 399.99m, category: "Test Expense 4 Category", date: new DateTime(2023, 01, 04));

        int month = 1;
        var response = await _client.GetAsync($"api/Expenses?month={month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expenses, Is.Not.Null);
        Assert.That(expenses.All(e => e.Date.Month == month), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense1.Id), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense2.Id), Is.False);
        Assert.That(expenses.Any(e => e.Id == testExpense3.Id), Is.False);
        Assert.That(expenses.Any(e => e.Id == testExpense4.Id), Is.True);
    }

    [Test]
    public async Task GetExpenses_FilterByYearAndMonth_ReturnsComposedResult()
    {
        var (_, testExpense1) = await CreateTestExpense(paymentMethod: PaymentMethod.DigitalPayment, paymentNote: "payment note for test expense 1", description: "description for test expense 1", amount: 99.99m, category: "Test Expense 1 Category", date: new DateTime(2026, 06, 01));
        var (_, testExpense2) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 2", description: "description for test expense 2", amount: 199.99m, category: "Test Expense 2 Category", date: new DateTime(2026, 09, 02));
        var (_, testExpense3) = await CreateTestExpense(paymentMethod: PaymentMethod.Payable, paymentNote: "payment note for test expense 3", description: "description for test expense 3", amount: 299.99m, category: "Test Expense 3 Category", date: new DateTime(2023, 06, 03));
        var (_, testExpense4) = await CreateTestExpense(paymentMethod: PaymentMethod.Cash, paymentNote: "payment note for test expense 4", description: "description for test expense 4", amount: 399.99m, category: "Test Expense 4 Category", date: new DateTime(2023, 09, 04));

        int year = 2026;
        int month = 6;
        var response = await _client.GetAsync($"api/Expenses?year={year}&month={month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expenses, Is.Not.Null);

        Assert.That(expenses.All(e => e.Date.Year == year && e.Date.Month == month), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense1.Id), Is.True);
        Assert.That(expenses.Any(e => e.Id == testExpense2.Id), Is.False);
        Assert.That(expenses.Any(e => e.Id == testExpense3.Id), Is.False);
        Assert.That(expenses.Any(e => e.Id == testExpense4.Id), Is.False);
    }

    [Test]
    public async Task GetExpenses_WithNoMatches_ReturnsEmptyList()
    {
        int year = 1;
        int month = 1;
        var response = await _client.GetAsync($"api/Expenses?year={year}&month={month}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expenses, Is.Not.Null);
        Assert.That(expenses, Is.Empty);
    }

    [Test]
    public async Task UpdateExpense_WithValidData_ReturnsOk()
    {
        var (_, testExpense) = await CreateTestExpense();

        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = PaymentMethod.Payable,
            PaymentNote = "Promised to pay rent in 2 months",
            Description = "Rent Credit",
            Amount = 6000m,
            Category = "Rent",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PutAsJsonAsync($"api/Expenses/{testExpense.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(payload.Description));
        Assert.That(expense.Amount, Is.EqualTo(payload.Amount));
        Assert.That(expense.Category, Is.EqualTo(payload.Category));
        Assert.That(expense.Date, Is.EqualTo(payload.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(testExpense.CreatedAt));
    }
    
    [Test]
    public async Task UpdateExpense_WithInvalidData_ReturnsBadRequest()
    {
        var (_, testExpense) = await CreateTestExpense();

        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = PaymentMethod.Payable,
            PaymentNote = "Promised to pay rent in 2 months",
            Description = "Rent Credit",
            Amount = -6000m,
            Category = "Rent",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PutAsJsonAsync($"api/Expenses/{testExpense.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");

        var responseGetExpense = await _client.GetAsync($"api/Expenses/{testExpense.Id}");
        Assert.That(responseGetExpense.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetExpense.StatusCode} instead.");

        var expense = await responseGetExpense.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(testExpense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(testExpense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(testExpense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(testExpense.Description));
        Assert.That(expense.Amount, Is.EqualTo(testExpense.Amount));
        Assert.That(expense.Category, Is.EqualTo(testExpense.Category));
        Assert.That(expense.Date, Is.EqualTo(testExpense.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(testExpense.CreatedAt));
    }

    [Test]
    public async Task UpdateExpense_WithInvalidEnumPaymentMethod_ReturnsBadRequest()
    {
        var (_, testExpense) = await CreateTestExpense();

        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = (PaymentMethod)999,
            PaymentNote = "Promised to pay rent in 2 months",
            Description = "Test Invalid Enum Payment Method",
            Amount = 6000m,
            Category = "Rent",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PutAsJsonAsync($"api/Expenses/{testExpense.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");

        var responseGetExpense = await _client.GetAsync($"api/Expenses/{testExpense.Id}");
        Assert.That(responseGetExpense.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetExpense.StatusCode} instead.");

        var expense = await responseGetExpense.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(testExpense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(testExpense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(testExpense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(testExpense.Description));
        Assert.That(expense.Amount, Is.EqualTo(testExpense.Amount));
        Assert.That(expense.Category, Is.EqualTo(testExpense.Category));
        Assert.That(expense.Date, Is.EqualTo(testExpense.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(testExpense.CreatedAt));
    }

    [Test]
    public async Task UpdateExpense_WithNonExistentId_ReturnsNotFound()
    {
        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = PaymentMethod.Payable,
            PaymentNote = "Promised to pay rent in 2 months",
            Description = "Rent Credit",
            Amount = 6000m,
            Category = "Rent",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PutAsJsonAsync($"api/Expenses/{int.MaxValue}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task UpdateExpense_WithRestockReference_ReturnsConflict()
    {
        var (_, product) = await CreateTestProduct();

        int testQuantity = 11;
        decimal testUnitCost = 149.99m;
        DateTime testDate = new DateTime(2026, 01, 01);
        string? testPaymentNote = "test restock payment note";
        PaymentMethod testPaymentMethod = PaymentMethod.Cash;
        var items = new List<RestockItemRequest> { new RestockItemRequest {ProductId = product.Id, Quantity = testQuantity, UnitCost = testUnitCost} };
        
        var (_, restockResponse) = await RestockTestProducts(items, date: testDate, paymentNote: testPaymentNote, paymentMethod: testPaymentMethod);

        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = restockResponse.Expense.PaymentMethod,
            PaymentNote = restockResponse.Expense.PaymentNote,
            Description = restockResponse.Expense.Description,
            Amount = restockResponse.Expense.Amount + 1,
            Category = restockResponse.Expense.Category,
            Date = restockResponse.Expense.Date
        };

        var response = await _client.PutAsJsonAsync($"api/Expenses/{restockResponse.Expense.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), $"Expected 409 Conflict() status, but received {response.StatusCode} instead.");

        var responseGetExpense = await _client.GetAsync($"api/Expenses/{restockResponse.Expense.Id}");
        Assert.That(responseGetExpense.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetExpense.StatusCode} instead.");

        var expense = await responseGetExpense.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(restockResponse.Expense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(restockResponse.Expense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(restockResponse.Expense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(restockResponse.Expense.Description));
        Assert.That(expense.Amount, Is.EqualTo(restockResponse.Expense.Amount));
        Assert.That(expense.Category, Is.EqualTo(restockResponse.Expense.Category));
        Assert.That(expense.Date, Is.EqualTo(restockResponse.Expense.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(restockResponse.Expense.CreatedAt));
    }

    [Test]
    public async Task DeleteExpense_WithValidId_ReturnsNoContent()
    {
        var (_, testExpense) = await CreateTestExpense();

        var response = await _client.DeleteAsync($"api/Expenses/{testExpense.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"Expected 204 No Content() status, but received {response.StatusCode} instead.");

        var responseGet = await _client.GetAsync($"api/Expenses/{testExpense.Id}");
        Assert.That(responseGet.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 Not Found() status, but received {responseGet.StatusCode} instead.");
    }   

    [Test]
    public async Task DeleteExpense_WithNonExistentId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"api/Expenses/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 Not Found() status, but received {response.StatusCode} instead.");
    }   

    [Test]
    public async Task DeleteExpense_WithRestockReference_ReturnsConflict()
    {
        var (_, product) = await CreateTestProduct();

        int testQuantity = 11;
        decimal testUnitCost = 149.99m;
        DateTime testDate = new DateTime(2026, 01, 01);
        string? testPaymentNote = "test restock payment note";
        PaymentMethod testPaymentMethod = PaymentMethod.Cash;
        var items = new List<RestockItemRequest> { new RestockItemRequest {ProductId = product.Id, Quantity = testQuantity, UnitCost = testUnitCost} };
        
        var (_, restockResponse) = await RestockTestProducts(items, date: testDate, paymentNote: testPaymentNote, paymentMethod: testPaymentMethod);

        var response = await _client.DeleteAsync($"api/Expenses/{restockResponse.Expense.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), $"Expected 409 Conflict() status, but received {response.StatusCode} instead.");

        var responseGetExpense = await _client.GetAsync($"api/Expenses/{restockResponse.Expense.Id}");
        Assert.That(responseGetExpense.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetExpense.StatusCode} instead.");

        var expense = await responseGetExpense.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(restockResponse.Expense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(restockResponse.Expense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(restockResponse.Expense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(restockResponse.Expense.Description));
        Assert.That(expense.Amount, Is.EqualTo(restockResponse.Expense.Amount));
        Assert.That(expense.Category, Is.EqualTo(restockResponse.Expense.Category));
        Assert.That(expense.Date, Is.EqualTo(restockResponse.Expense.Date));
        Assert.That(expense.CreatedAt, Is.EqualTo(restockResponse.Expense.CreatedAt));
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

    public async Task<(HttpResponseMessage Response, ProductResponse Product)> CreateTestProduct(
        string name = "Test Product", 
        string? sku = null, 
        decimal price = 99.99m, 
        int stockQuantity = 10, 
        int lowStockLevel = 3, 
        string category = "Test")
    {
        var payload = new
        {
            Name = name,
            Sku = sku ?? Guid.NewGuid().ToString(),
            Price = price,
            StockQuantity = stockQuantity,
            LowStockLevel = lowStockLevel,
            Category = category,
        };

        var response = await _client.PostAsJsonAsync("api/Products", payload);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status in creating test product (setup helper), but received {response.StatusCode}");
        }

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        if (product == null)
        {
            throw new InvalidOperationException($"Product is null in creating test product (setup helper) but expected otherwise");
        }
        _createdProductIds.Add(product.Id);

        return (response, product);
    }

    public async Task<(HttpResponseMessage Response, RestockResponse Restock)> RestockTestProducts (
        List<RestockItemRequest> items, 
        DateTime? date = null, 
        string description = "Default restock test description", 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        var payload = new RestockRequest
        {
            Date = date ?? DateTime.Now,
            Description = description,
            Items = items,
            PaymentNote = paymentNote,
            PaymentMethod = paymentMethod
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        if(response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Expecting 200 Ok() status, but received {response.StatusCode}");
        }
        
        var restockResponse = await response.Content.ReadFromJsonAsync<RestockResponse>();
        if (restockResponse == null)
        {
            throw new InvalidOperationException($"RestockResponse is null in restocking a test product (setup helper) but expected otherwise");
        }

        _restockedProductIds.AddRange(restockResponse.UpdatedProducts.Select(p => p.Id));
        return (response, restockResponse);
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

        if(_createdProductIds.Count > 0)
        {
            foreach(int i in _createdProductIds)
            {
                if(_restockedProductIds.Contains(i))
                {
                    TestContext.Progress.WriteLine($"Skipped product cleanup: product with id {i} has references to a delivery item/expense — deletion blocked by design (audit trail preserved).");
                    
                }
                else
                {
                    var response = await _client.DeleteAsync($"api/Products/{i}");
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    { 
                        TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with an Id of {i}: Expected 204 No Content() status, but received {response.StatusCode}");
                    }
                }
            }
        }
        _createdProductIds.Clear();
        _restockedProductIds.Clear();
    }
}