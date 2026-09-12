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
    private List<int> _createdProductIds = new ();
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
        var (_, testSale, _) = await CreateTestSaleWithItemAsync();

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
    public async Task CreateSettlement_SaleNotClosed_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) sale not closed",
            Date = new DateTime(2026, 01, 01)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task CreateSettlement_DateBeforeSaleDate_ReturnsBadRequest()
    {
        var (_, testSale, _) = await CreateTestSaleWithItemAsync(saleDate: new DateTime(2026, 01, 01));

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) settlement date earlier than sale date",
            Date = new DateTime(2025, 01, 01)
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
    public async Task CreateSettlement_AmountExceedsOutstandingBalance_ReturnsBadRequest()
    {
        var (_, testSale, _) = await CreateTestSaleWithItemAsync();

        var payload = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.76m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (invalid) amount exceeds outstanding balance",
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
            Date = DateTime.Now.AddYears(1000)
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 Bad Request() status, but received {response.StatusCode} instead.");
    }

    [Test]
    public async Task UpdateSaleWithSettlement_PaymentMethodChange_ReturnsConflict()
    {
        var (_, testSale, _) = await CreateTestSaleWithItemAsync(paymentMethod: PaymentMethod.Receivable);

        var payloadCreateSettlement = new CreateSettlementRequest
        {
            SaleId = testSale.Id,
            ExpenseId = null,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (valid) for updating sale payment method (invalid)",
            Date = new DateTime(2026, 01, 01)
        };

        var responseCreateSettlement = await _client.PostAsJsonAsync("api/Settlements", payloadCreateSettlement);
        Assert.That(responseCreateSettlement.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {responseCreateSettlement.StatusCode} instead.");

        var settlement = await responseCreateSettlement.Content.ReadFromJsonAsync<SettlementResponse>(TestJsonOptions.Default);
        Assert.That(settlement, Is.Not.Null);
        _createdSettlementIds.Add(settlement.Id);

        var payload = new EditSaleRequest
        {
            SaleDate = new DateTime(2026, 02, 02),
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "test payment note for updating sale (invalid) with settlement payment method change",
        };

        var response = await _client.PutAsJsonAsync($"api/Sales/{testSale.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), $"Expected 409 Conflict() status, but received {response.StatusCode} instead.");

        var responseGetSale = await _client.GetAsync($"api/Sales/{testSale.Id}");
        var sale = await responseGetSale.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.Id, Is.EqualTo(testSale.Id));
        Assert.That(sale.SaleDate, Is.EqualTo(testSale.SaleDate));
        Assert.That(sale.PaymentMethod, Is.EqualTo(testSale.PaymentMethod));
        Assert.That(sale.PaymentNote, Is.EqualTo(testSale.PaymentNote));
    }

    [Test]
    public async Task UpdateExpenseWithSettlement_PaymentMethodChange_ReturnsConflict()
    {
        var (_, testExpense) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Payable);

        var payloadCreateSettlement = new CreateSettlementRequest
        {
            SaleId = null,
            ExpenseId = testExpense.Id,
            Amount = 99.75m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create settlement test (valid) for updating expense payment method (invalid)",
            Date = new DateTime(2026, 03, 03)
        };

        var responseCreateSettlement = await _client.PostAsJsonAsync("api/Settlements", payloadCreateSettlement);
        Assert.That(responseCreateSettlement.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {responseCreateSettlement.StatusCode} instead.");

        var settlement = await responseCreateSettlement.Content.ReadFromJsonAsync<SettlementResponse>(TestJsonOptions.Default);
        Assert.That(settlement, Is.Not.Null);
        _createdSettlementIds.Add(settlement.Id);

        var payload = new UpdateExpenseRequest
        {
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "test payment note for updating expense (invalid) with settlement payment method change",
            Description = "test description for updating expense (invalid) with settlement payment method change",
            Amount = testExpense.Amount,
            Category = "test category",
            Date = new DateTime(2026, 02, 02)
        };
        
        var response = await _client.PutAsJsonAsync($"api/Expenses/{testExpense.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), $"Expected 409 Conflict() status, but received {response.StatusCode} instead.");

        var responseGetExpense = await _client.GetAsync($"api/Expenses/{testExpense.Id}");
        var expense = await responseGetExpense.Content.ReadFromJsonAsync<ExpenseResponse>(TestJsonOptions.Default);
        Assert.That(expense, Is.Not.Null);
        Assert.That(expense.Id, Is.EqualTo(testExpense.Id));
        Assert.That(expense.PaymentMethod, Is.EqualTo(testExpense.PaymentMethod));
        Assert.That(expense.PaymentNote, Is.EqualTo(testExpense.PaymentNote));
        Assert.That(expense.Description, Is.EqualTo(testExpense.Description));
        Assert.That(expense.Amount, Is.EqualTo(testExpense.Amount));
        Assert.That(expense.Category, Is.EqualTo(testExpense.Category));
        Assert.That(expense.Date, Is.EqualTo(testExpense.Date));
    }

    public async Task<(HttpResponseMessage Response, ProductResponse Product)> CreateTestProductAsync(
        string name = "Test Product", 
        string? sku = null, 
        decimal price = 99.75m, 
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

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(TestJsonOptions.Default);
        if (product == null)
        {
            throw new InvalidOperationException($"Product is null in creating test product (setup helper) but expected otherwise");
        }
        _createdProductIds.Add(product.Id);

        return (response, product);
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
            Date = date ?? new DateTime(2026, 01, 01)
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
        SaleDate = saleDate ?? new DateTime(2026, 01, 01), 
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

    private async Task<(ProductResponse Product, SaleResponse Sale, SaleItemResponse SaleItem)> CreateTestSaleWithItemAsync(
        string name = "Test Product", 
        string? sku = null, 
        decimal price = 99.75m, 
        int stockQuantity = 10, 
        int lowStockLevel = 3, 
        string category = "Test",

        DateTime? saleDate = null, 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Receivable,
        
        int saleItemQuantity = 1)
    {
        var (_, product) = await CreateTestProductAsync(name, sku, price, stockQuantity, lowStockLevel, category);
        var (_, sale) = await CreateTestSaleAsync(saleDate, paymentNote, paymentMethod);

        var payload = new 
        { 
            ProductId = product!.Id, 
            Quantity = saleItemQuantity, 
            UnitPriceAtSale = product.Price 
        };

        var response = await _client.PostAsJsonAsync($"/api/SaleItems/{sale!.Id}/items", payload);
        if(response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Expected 200 Ok() status, but received {response.StatusCode}");
        }

        var updatedSale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        if (updatedSale == null)
        {
            throw new InvalidOperationException($"Failed to deserialize SaleResponse after creating test sale");
        }
        if (updatedSale.SaleItems.Count == 0)
        {
            throw new InvalidOperationException($"Updated sale (with sale item) does not reflect any sale item added");
        }

        var saleItem = updatedSale!.SaleItems.Find(si => si.ProductId == product.Id);
        if (saleItem == null)
        {
            throw new InvalidOperationException($"Failure in finding added sale item");
        }
        
        var responseCloseSale = await _client.PostAsync($"/api/Sales/{updatedSale.Id}/close", null);
        if(responseCloseSale.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Expected 200 Ok() status, but received {responseCloseSale.StatusCode}");
        }

        updatedSale = await responseCloseSale.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        if (updatedSale == null)
        {
            throw new InvalidOperationException($"Failed to deserialize SaleResponse after creating test sale");
        }
        if (!updatedSale.IsClosed)
        {
            throw new InvalidOperationException($"Failed to close test sale");
        }
        return (product, updatedSale, saleItem);
    }

    [TearDown]
    public async Task DeleteTestExpenseSaleProductSettlement()
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

        //Test Product Cleanup
        if(_createdProductIds.Count > 0)
        {
            foreach(int i in _createdProductIds)
            {
                var response = await _client.DeleteAsync($"api/Products/{i}");
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    TestContext.Progress.WriteLine($"Skipped product cleanup: product with id {i} has existing reference(s) — deletion blocked by design (audit trail preserved).");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TestContext.Progress.WriteLine($"Product with an Id of {i} cannot be found -- already deleted or does not exist.");
                }
                else if (response.StatusCode != HttpStatusCode.NoContent)
                { 
                    TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with an Id of {i}: Expected 204 No Content() status, but received {response.StatusCode}");
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
        _createdProductIds.Clear();
        _createdSaleIds.Clear();
    }
}