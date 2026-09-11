using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

[NonParallelizable]
public class SalesApiTests
{   
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdProductIds = new();
    private List<int> _createdSaleIds = new();
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
    public async Task GetSale_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Sales/{int.MaxValue}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.StatusCode}");
    }

    [Test]
    public async Task CreateSale_ValidSaleRequest_ReturnsCreated()
    {
        var payload = new CreateSaleRequest
        {
            SaleDate = new DateTime(2026, 01, 01),
            PaymentMethod = PaymentMethod.Cash,
            PaymentNote = "Test payment note for create sale (valid) sale request"
        };
        var response = await _client.PostAsJsonAsync("api/Sales", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.SaleDate, Is.EqualTo(payload.SaleDate));
        Assert.That(sale.PaymentMethod, Is.EqualTo(payload.PaymentMethod));
        Assert.That(sale.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(sale.SaleItems.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CreateSale_WithInvalidEnumPaymentMethod_ReturnsBadRequest()
    {
        var payload = new { 
        SaleDate = new DateTime(2026, 01, 01), 
        PaymentMethod = (PaymentMethod)999, 
        PaymentNote = "Test Invalid Enum PaymentMethod in Sale Creation" 
        };
        
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");
        
        var responseGetSalesAfter = await _client.GetAsync("api/Sales");
        var salesAfter = await responseGetSalesAfter.Content.ReadFromJsonAsync<List<SaleResponse>>(TestJsonOptions.Default);
        Assert.That(salesAfter, Is.Not.Null);
        Assert.That(salesAfter.Any(s => s.PaymentNote == payload.PaymentNote), Is.False);
    }

    [Test]
    public async Task CreateSale_NoSettlement_ReturnsCorrectComputedFields()
    {
        var (_, testSale, testSaleItem) = await CreateTestSaleWithItemAsync();
        
        var response = await _client.GetAsync($"/api/Sales/{testSale.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");
        
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.TotalAmount, Is.EqualTo(testSaleItem.Quantity * testSaleItem.UnitPriceAtSale));
        Assert.That(sale.OutstandingBalance, Is.EqualTo(testSaleItem.Quantity * testSaleItem.UnitPriceAtSale));
        Assert.That(sale.AmountSettled, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSale_ExistingId_ReturnsOk()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var response = await _client.GetAsync($"/api/Sales/{testSale.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");

        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.Id, Is.EqualTo(testSale.Id));
        Assert.That(sale.SaleDate, Is.EqualTo(testSale.SaleDate));
        Assert.That(sale.CreatedAt, Is.EqualTo(testSale.CreatedAt));
        Assert.That(sale.PaymentMethod, Is.EqualTo(testSale.PaymentMethod));
        Assert.That(sale.PaymentNote, Is.EqualTo(testSale.PaymentNote));
        Assert.That(sale.IsClosed, Is.EqualTo(testSale.IsClosed));
        Assert.That(sale.TransactionCount, Is.EqualTo(testSale.TransactionCount));
        Assert.That(sale.ReductionFrequency, Is.EqualTo(testSale.ReductionFrequency));
        Assert.That(sale.TotalQuantityReduction, Is.EqualTo(testSale.TotalQuantityReduction));
        Assert.That(sale.SaleItems.Count, Is.EqualTo(testSale.SaleItems.Count));
    }

    [Test]
    public async Task EditSale_WithInvalidEnumPaymentMethod_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new { 
        SaleDate = new DateTime(2026, 01, 01), 
        PaymentMethod = (PaymentMethod)999, 
        PaymentNote = "Test Invalid Enum PaymentMethod in Editing Sale" 
        };
        
        var response = await _client.PutAsJsonAsync($"/api/Sales/{testSale!.Id}", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");
        
        var responseGetSale = await _client.GetAsync($"api/Sales/{testSale.Id}");
        Assert.That(responseGetSale.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetSale.StatusCode} instead.");
        
        var sale = await responseGetSale.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);

        Assert.That(sale.Id, Is.EqualTo(testSale.Id));
        Assert.That(sale.SaleDate, Is.EqualTo(testSale.SaleDate));
        Assert.That(sale.CreatedAt, Is.EqualTo(testSale.CreatedAt));
        Assert.That(sale.PaymentMethod, Is.EqualTo(testSale.PaymentMethod));
        Assert.That(sale.PaymentNote, Is.EqualTo(testSale.PaymentNote));
        Assert.That(sale.IsClosed, Is.EqualTo(testSale.IsClosed));
        Assert.That(sale.TransactionCount, Is.EqualTo(testSale.TransactionCount));
        Assert.That(sale.ReductionFrequency, Is.EqualTo(testSale.ReductionFrequency));
        Assert.That(sale.TotalQuantityReduction, Is.EqualTo(testSale.TotalQuantityReduction));
        Assert.That(sale.SaleItems.Count, Is.EqualTo(testSale.SaleItems.Count));
    }

    [Test]
    public async Task AddSaleItem_ValidRequest_ReturnsOk()
    {   
        var (_, testProduct) = await CreateTestProductAsync();
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new AddSaleItemRequest
        {
            ProductId = testProduct.Id,
            Quantity = 1,
            UnitPriceAtSale = testProduct.Price
        };

        var response = await _client.PostAsJsonAsync($"api/SaleItems/{testSale.Id}/items", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode} instead.");

        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(sale, Is.Not.Null);
        Assert.That(sale.SaleItems.Count, Is.EqualTo(testSale.SaleItems.Count + 1));

        var saleItem = sale.SaleItems.Find(si => si.ProductId == testProduct.Id);
        Assert.That(saleItem, Is.Not.Null);
        Assert.That(saleItem.Quantity, Is.EqualTo(payload.Quantity));
        Assert.That(saleItem.UnitPriceAtSale, Is.EqualTo(payload.UnitPriceAtSale));
    }


    [Test]
    public async Task ReduceSaleItemQuantity_ValidRequest_ReturnsOk()
    {
        var (testProduct, testSale, testSaleItem) = await CreateTestSaleWithItemAsync(saleItemQuantity: 2);

        var payload = new ReduceSaleItemQuantityRequest
        {
            Amount = 1
        };

        var response = await _client.PatchAsJsonAsync($"/api/SaleItems/{testSale.Id}/items/{testSaleItem.Id}/reduce", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");

        var saleAfterReducing = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(saleAfterReducing, Is.Not.Null);
        Assert.That(saleAfterReducing.SaleItems.Count, Is.EqualTo(testSale.SaleItems.Count));

        var saleItemAfterReducing = saleAfterReducing.SaleItems.Find(si => si.ProductId == testProduct.Id);
        Assert.That(saleItemAfterReducing, Is.Not.Null);
        Assert.That(saleItemAfterReducing.Quantity, Is.EqualTo(testSaleItem.Quantity - payload.Amount));

        Assert.That(saleAfterReducing.ReductionFrequency, Is.EqualTo(testSale.ReductionFrequency + 1));
        Assert.That(saleAfterReducing.TotalQuantityReduction, Is.EqualTo(testSale.TotalQuantityReduction + payload.Amount));
        Assert.That(saleAfterReducing.TransactionCount, Is.EqualTo(testSale.TransactionCount));
    }

    [Test]
    public async Task ReduceSaleItemQuantity_ReducesToZero_ReturnsOk()
    {   
        var (testProduct, testSale, testSaleItem) = await CreateTestSaleWithItemAsync();

        var payload = new ReduceSaleItemQuantityRequest
        {
            Amount = 1
        };

        var response = await _client.PatchAsJsonAsync($"/api/SaleItems/{testSale.Id}/items/{testSaleItem.Id}/reduce", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");

        var saleAfterReducing = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(saleAfterReducing, Is.Not.Null);
        Assert.That(saleAfterReducing.SaleItems.Count, Is.EqualTo(testSale.SaleItems.Count - 1));

        var saleItemAfterReducing = saleAfterReducing.SaleItems.Find(si => si.ProductId == testProduct.Id);
        Assert.That(saleItemAfterReducing, Is.Null);

        Assert.That(saleAfterReducing.ReductionFrequency, Is.EqualTo(testSale.ReductionFrequency + 1));
        Assert.That(saleAfterReducing.TotalQuantityReduction, Is.EqualTo(testSale.TotalQuantityReduction + payload.Amount));
        Assert.That(saleAfterReducing.TransactionCount, Is.EqualTo(testSale.TransactionCount));
    }

    [Test]
    public async Task CloseSale_ValidSale_ReturnsOk()
    {   
        var (testProduct, testSale, testSaleItem) = await CreateTestSaleWithItemAsync();

        var response = await _client.PostAsync($"/api/Sales/{testSale.Id}/close", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");
        
        var saleAfterClosing = await response.Content.ReadFromJsonAsync<SaleResponse>(TestJsonOptions.Default);
        Assert.That(saleAfterClosing, Is.Not.Null);
        Assert.That(saleAfterClosing.IsClosed, Is.True);

        var saleItemAfterClosing = saleAfterClosing.SaleItems.Find(si => si.ProductId == testProduct.Id);
        Assert.That(saleItemAfterClosing, Is.Not.Null);
        Assert.That(saleItemAfterClosing.Quantity, Is.EqualTo(testSaleItem.Quantity));

        var responseGetProduct = await _client.GetAsync($"/api/Products/{testProduct.Id}");
        Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetProduct.StatusCode}.");

        var productAfterClosing = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>(TestJsonOptions.Default);
        Assert.That(productAfterClosing, Is.Not.Null);
        Assert.That(productAfterClosing.StockQuantity, Is.EqualTo(testProduct.StockQuantity - testSaleItem.Quantity));
    }

    
    [TearDown]
    public async Task DeleteTestSaleAndProduct()
    {
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
        _createdProductIds.Clear();
        _createdSaleIds.Clear();
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

    private async Task <(HttpResponseMessage Response, SaleResponse Sale)> CreateTestSaleAsync(
        DateTime? saleDate = null, 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
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

    private async Task<(ProductResponse Product, SaleResponse Sale, SaleItemResponse SaleItem)> CreateTestSaleWithItemAsync(
        string name = "Test Product", 
        string? sku = null, 
        decimal price = 99.75m, 
        int stockQuantity = 10, 
        int lowStockLevel = 3, 
        string category = "Test",

        DateTime? saleDate = null, 
        string? paymentNote = null, 
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        
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
        
        return (product, updatedSale, saleItem);
    }
}
