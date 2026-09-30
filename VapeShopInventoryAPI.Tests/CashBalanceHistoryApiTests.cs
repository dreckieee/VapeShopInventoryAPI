using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace VapeShopInventoryAPI.Tests;

public class CashBalanceHistoryApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdProductIds = new ();
    private List<int> _createdExpenseIds = new ();
    private List<int> _createdSaleIds = new ();
    private List<int> _createdSettlementIds = new ();
    private List<int> _createdCapitalTransactionIds = new ();
    private readonly DateTime _today = DateTime.Today;

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
    public async Task GetCashBalanceHistory_WithDays1_TopRowMatchesCurrentCashBalance()
    {
        var (_, _, _) = await CreateTestSaleWithItemAsync(price: 100m, paymentMethod: PaymentMethod.Cash, saleItemQuantity: 2);

        var responseGetCashBalance = await _client.GetAsync("api/CashBalance");
        Assert.That(responseGetCashBalance.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetCashBalance.StatusCode} instead.");

        var cashBalance = await responseGetCashBalance.Content.ReadFromJsonAsync<CashBalanceResponse>(TestJsonOptions.Default);        
        Assert.That(cashBalance, Is.Not.Null);

        var response = await _client.GetAsync("api/CashBalance/history?days=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var cashBalanceHistory = await response.Content.ReadFromJsonAsync<List<CashBalanceHistoryEntryResponse>>(TestJsonOptions.Default);
        Assert.That(cashBalanceHistory, Is.Not.Null);
        Assert.That(cashBalanceHistory[0].RunningCashBalance, Is.EqualTo(cashBalance.CashOnHand));
        Assert.That(cashBalanceHistory[0].RunningDigitalBalance, Is.EqualTo(cashBalance.DigitalBalance));
    }

    [Test]
    public async Task GetCashBalanceHistory_WithDays1AndMultipleCashTransactions_ReturnsCorrectSeededDataAndOk()
    {
        //sale1 cash 199.98
        var (_, sale1, _) = await CreateTestSaleWithItemAsync(price: 99.99m, paymentMethod: PaymentMethod.Cash, saleItemQuantity: 2, saleDate: _today);

        //expense1 cash 49.99
        var (_, expense1) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Cash, amount: 49.99m, date: _today);

        //deposit1 cash 500.00
        var (_, deposit1) = await CreateTestCapitalTransactionAsync(type: CapitalTransactionType.Deposit, amount: 500.00m, paymentMethod: PaymentMethod.Cash, date: _today);

        //withdrawal1 cash 10.00
        var (_, withdrawal1) = await CreateTestCapitalTransactionAsync(type: CapitalTransactionType.Withdrawal, paymentMethod: PaymentMethod.Cash, amount: 10.00m, date: _today);

        var response = await _client.GetAsync("api/CashBalance/history?days=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var cashBalanceHistory = await response.Content.ReadFromJsonAsync<List<CashBalanceHistoryEntryResponse>>(TestJsonOptions.Default);
        Assert.That(cashBalanceHistory, Is.Not.Null);

        var saleRow = cashBalanceHistory.Find(entry => entry.SourceId == sale1.Id && entry.SourceType == CashBalanceSourceType.Sale);
        var expenseRow = cashBalanceHistory.Find(entry => entry.SourceId == expense1.Id && entry.SourceType == CashBalanceSourceType.Expense);
        var depositRow = cashBalanceHistory.Find(entry => entry.SourceId == deposit1.Id && entry.SourceType == CashBalanceSourceType.CapitalTransaction);
        var withdrawalRow = cashBalanceHistory.Find(entry => entry.SourceId == withdrawal1.Id && entry.SourceType == CashBalanceSourceType.CapitalTransaction);

        Assert.That(saleRow, Is.Not.Null);
        Assert.That(expenseRow, Is.Not.Null);
        Assert.That(depositRow, Is.Not.Null);
        Assert.That(withdrawalRow, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(saleRow.Amount, Is.EqualTo(199.98m));
            Assert.That(expenseRow.Amount, Is.EqualTo(-49.99m));
            Assert.That(depositRow.Amount, Is.EqualTo(500.00m));
            Assert.That(withdrawalRow.Amount, Is.EqualTo(-10.00m));

            Assert.That(saleRow.Date, Is.EqualTo(_today));
            Assert.That(expenseRow.Date, Is.EqualTo(_today));
            Assert.That(depositRow.Date, Is.EqualTo(_today));
            Assert.That(withdrawalRow.Date, Is.EqualTo(_today));

            Assert.That(saleRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
            Assert.That(expenseRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
            Assert.That(depositRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
            Assert.That(withdrawalRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
        });
    }

    [Test]
    public async Task GetCashBalanceHistory_WithDays1AndSettlements_ReturnsCorrectSettlementsDataAndOk()
    {
        //sale1 receivable 160.00
        var (_, sale1, _) = await CreateTestSaleWithItemAsync(price: 80.00m, paymentMethod: PaymentMethod.Receivable, saleItemQuantity: 2, saleDate: _today);

        //expense1 payable 120.00
        var (_, expense1) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Payable, amount: 120.00m, date: _today);
        
        //settlement1 sale cash 60.00
        var (_, settlement1) = await CreateTestSettlementAsync(saleId: sale1.Id, paymentMethod: PaymentMethod.Cash, amount: 60.00m, date: _today);

        //settlement2 expense cash 45.00
        var (_, settlement2) = await CreateTestSettlementAsync(expenseId: expense1.Id, paymentMethod: PaymentMethod.Cash, amount: 45.00m, date: _today);

        var response = await _client.GetAsync("api/CashBalance/history?days=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var cashBalanceHistory = await response.Content.ReadFromJsonAsync<List<CashBalanceHistoryEntryResponse>>(TestJsonOptions.Default);
        Assert.That(cashBalanceHistory, Is.Not.Null);

        Assert.That(cashBalanceHistory.Find(entry => entry.SourceId == sale1.Id && entry.SourceType == CashBalanceSourceType.Sale), Is.Null);
        Assert.That(cashBalanceHistory.Find(entry => entry.SourceId == expense1.Id && entry.SourceType == CashBalanceSourceType.Expense), Is.Null);

        var saleSettlementRow = cashBalanceHistory.Find(entry => entry.SourceId == settlement1.Id && entry.SourceType == CashBalanceSourceType.Settlement);
        var expenseSettlementRow = cashBalanceHistory.Find(entry => entry.SourceId == settlement2.Id && entry.SourceType == CashBalanceSourceType.Settlement);

        Assert.That(saleSettlementRow, Is.Not.Null);
        Assert.That(expenseSettlementRow, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(saleSettlementRow.Amount, Is.EqualTo(60.00m));
            Assert.That(expenseSettlementRow.Amount, Is.EqualTo(-45.00m));
            Assert.That(saleSettlementRow.Date, Is.EqualTo(_today));
            Assert.That(expenseSettlementRow.Date, Is.EqualTo(_today));
            Assert.That(saleSettlementRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
            Assert.That(expenseSettlementRow.PaymentMethod, Is.EqualTo(PaymentMethod.Cash));
        }); 
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
            Date = date ?? _today
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
            Date = date ?? _today
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

    private async Task <(HttpResponseMessage Response, SaleResponse Sale)> CreateTestSaleAsync(
        DateTime? saleDate = null, 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Receivable)
    {
        var payload = new { 
        SaleDate = saleDate ?? _today, 
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

    private async Task<(HttpResponseMessage Response, SettlementResponse Settlement)> CreateTestSettlementAsync(
        int? saleId = null,
        int? expenseId = null,
        decimal amount = 99.75m,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        string? paymentNote = null,
        DateTime? date = null)
    {
        if (saleId == null && expenseId == null)
        {
            throw new InvalidOperationException($"Failure in creating a test settlement: both saleid and expenseid are null");
        }
        if (saleId != null && expenseId != null)
        {
            throw new InvalidOperationException($"Failure in creating a test settlement: both saleid and expenseid have values");
        }

        var payload = new CreateSettlementRequest
        {
            SaleId = saleId,
            ExpenseId = expenseId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentNote = paymentNote,
            Date = date ?? _today
        };

        var response = await _client.PostAsJsonAsync("api/Settlements", payload);
        if(response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Expected 201 Created() status, but received {response.StatusCode}");
        }

        var settlement = await response.Content.ReadFromJsonAsync<SettlementResponse>(TestJsonOptions.Default);
        if (settlement == null)
        {
            throw new InvalidOperationException($"Failed to deserialize SettlementResponse after creating test settlement");
        }
        _createdSettlementIds.Add(settlement.Id);

        return (response, settlement);
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
        _createdSettlementIds.Clear();
        _createdExpenseIds.Clear();
        _createdProductIds.Clear();
        _createdSaleIds.Clear();
    }
}