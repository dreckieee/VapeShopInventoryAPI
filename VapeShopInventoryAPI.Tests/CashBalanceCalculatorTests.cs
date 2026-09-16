using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace VapeShopInventoryAPI.Tests;

public class CashBalanceCalculatorTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdProductIds = new ();
    private List<int> _createdExpenseIds = new ();
    private List<int> _createdSaleIds = new ();
    private List<int> _createdSettlementIds = new ();
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
    public async Task CalculateCashBalanceAsync_WithMixedTransactions_ReturnsCorrectBalances()
    {
        //cash

        //sale1 cash (cashonhand + 200)
        var (product1, sale1, saleItem1) = await CreateTestSaleWithItemAsync(price: 100m, paymentMethod: PaymentMethod.Cash, saleItemQuantity: 2);
        
        //sale2 receivable (receivable + 150)
        var (product2, sale2, saleItem2) = await CreateTestSaleWithItemAsync(price: 150m, paymentMethod: PaymentMethod.Receivable, saleItemQuantity: 1);

        //sale2 cash settlement (cashonhand + 149, receivable - 149)
        var (_, settlement1) = await CreateTestSettlementAsync(saleId: sale2.Id, paymentMethod: PaymentMethod.Cash, amount: 149m);

        //expense1 cash (cashonhand - 50)
        var (_, expense1) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Cash, amount: 50);

        //expense2 payable (payable + 11)
        var (_, expense2) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.Payable, amount: 11);

        //expense2 cash settlement (cashonhand - 10, payable - 10)
        var (_, settlement2) = await CreateTestSettlementAsync(expenseId: expense2.Id, paymentMethod: PaymentMethod.Cash, amount: 10m);

        //deposit1 cash (cashonhand + 500)
        var (_, deposit1) = await CreateTestCapitalTransactionAsync(type: CapitalTransactionType.Deposit, amount: 500m, paymentMethod: PaymentMethod.Cash);

        //withdrawal1 cash (cashonhand - 9)
        var (_, withdrawal1) = await CreateTestCapitalTransactionAsync(type: CapitalTransactionType.Withdrawal, amount: 9m);

        //digital

        //sale3 digitalpayment cash (digitalBalance + 198)
        var (product3, sale3, saleItem3) = await CreateTestSaleWithItemAsync(price: 99.5m, paymentMethod: PaymentMethod.DigitalPayment, saleItemQuantity: 2);
        
        //sale4 receivable (receivable + 149)
        var (product4, sale4, saleItem4) = await CreateTestSaleWithItemAsync(price: 149m, paymentMethod: PaymentMethod.Receivable, saleItemQuantity: 1);

        //sale4 digitalpayment settlement (digitalBalance + 148, receivable - 148)
        var (_, settlement3) = await CreateTestSettlementAsync(saleId: sale4.Id, paymentMethod: PaymentMethod.DigitalPayment, amount: 148m);

        //expense3 digitalpayment (digitalBalance - 49)
        var (_, expense3) = await CreateTestExpenseAsync(paymentMethod: PaymentMethod.DigitalPayment, amount: 49);

        //deposit2 digitalpayment (digitalbalance + 499)
        var (_, deposit2) = await CreateTestCapitalTransactionAsync(type: CapitalTransactionType.Deposit, amount: 499m, paymentMethod: PaymentMethod.DigitalPayment);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

        var (cashOnHand, digitalBalance, receivablesOutstanding, payablesOutstanding) = await CashBalanceCalculator.CalculateCashBalanceAsync(context);
        Assert.That(cashOnHand, Is.EqualTo(780));
        Assert.That(digitalBalance, Is.EqualTo(797));
        Assert.That(receivablesOutstanding, Is.EqualTo(2));
        Assert.That(payablesOutstanding, Is.EqualTo(1));
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
            Date = date ?? new DateTime(2026, 01, 01)
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