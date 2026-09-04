using System.Net;
using System.Net.Http.Json;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;

namespace VapeShopInventoryAPI.Tests;

[NonParallelizable]
public class SalesApiTests
{   
    // Shared mutable test state (_createdSaleId, _createdProductId, _isCreatedSaleClosed, _skuCounter)
    // assumes NUnit runs this fixture's tests sequentially, not in parallel.
    // Do not enable [Parallelizable] on this class without refactoring this state.
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int? _createdSaleId;
    private bool _isCreatedSaleClosed = false;
    private int? _createdProductId;
    private int _skuCounter = 111;
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
        var saleDate = DateTime.Now;
        var (response, sale) = await CreateTestSaleAsync(saleDate);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        Assert.That(sale!.SaleDate, Is.EqualTo(saleDate));
    }

    [Test]
    public async Task CreateSale_WithInvalidEnumPaymentMethod_ReturnsBadRequest()
    {
        var (_, testSale) = await CreateTestSaleAsync();

        var payload = new { 
        SaleDate = new DateTime(2026, 01, 01), 
        PaymentMethod = (PaymentMethod)999, 
        PaymentNote = "Test Invalid Enum PaymentMethod in Sale Creation" 
        };
        
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expected 400 BadRequest() status, but received {response.StatusCode} instead.");
        
        var responseGetSalesAfter = await _client.GetAsync("api/Sales");
        var salesAfter = await responseGetSalesAfter.Content.ReadFromJsonAsync<List<SaleResponse>>();
        Assert.That(salesAfter, Is.Not.Null);
        Assert.That(salesAfter.Any(s => s.PaymentNote == payload.PaymentNote), Is.False);
    }

    [Test]
    public async Task GetSale_ExistingId_ReturnsOk()
    {
        //setup: create sale
        var saleDate = DateTime.Now;
        var (_, sale) = await CreateTestSaleAsync(saleDate);

        //get sale with existing id
        var response = await _client.GetAsync($"/api/Sales/{sale!.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");

        var saleFound = await response.Content.ReadFromJsonAsync<SaleResponse>();  
        Assert.That(saleFound, Is.Not.Null);
        Assert.That(saleFound.Id, Is.EqualTo(sale.Id));
        Assert.That(saleFound.SaleDate, Is.EqualTo(sale.SaleDate));
        Assert.That(saleFound.CreatedAt, Is.EqualTo(sale.CreatedAt));
        Assert.That(saleFound.PaymentMethod, Is.EqualTo(sale.PaymentMethod));
        Assert.That(saleFound.PaymentNote, Is.EqualTo(sale.PaymentNote));
        Assert.That(saleFound.IsClosed, Is.EqualTo(sale.IsClosed));
        Assert.That(saleFound.TransactionCount, Is.EqualTo(sale.TransactionCount));
        Assert.That(saleFound.ReductionFrequency, Is.EqualTo(sale.ReductionFrequency));
        Assert.That(saleFound.TotalQuantityReduction, Is.EqualTo(sale.TotalQuantityReduction));
        Assert.That(saleFound.SaleItems.Count, Is.EqualTo(sale.SaleItems.Count));
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
        
        var sale = await responseGetSale.Content.ReadFromJsonAsync<SaleResponse>();
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
        //setup: create product, create sale, create sale item
        int saleItemQuantity = 1;

        var (product, sale, saleItem) = await CreateSaleWithItemAsync(
            productName: "Test Product", 
            productSku: _skuCounter.ToString(), 
            productPrice: 99.99m, 
            productStockQuantity: 10, 
            productCategory: "Test", 
            saleItemQuantity: saleItemQuantity,
            productLowStockLevel: 0
            );

        
        Assert.That(sale, Is.Not.Null);

        Assert.That(saleItem, Is.Not.Null);
        Assert.That(saleItem.ProductId, Is.EqualTo(product.Id));
        Assert.That(saleItem.Quantity, Is.EqualTo(saleItemQuantity));
        Assert.That(saleItem.UnitPriceAtSale, Is.EqualTo(product.Price));
    }


    [Test]
    public async Task ReduceSaleItemQuantity_ValidRequest_ReturnsOk()
    {   
        //setup: create product, create sale, create sale item
        var (product, sale, saleItem) = await CreateSaleWithItemAsync(
            productName: "Test Product", 
            productSku: _skuCounter.ToString(), 
            productPrice: 99.99m, 
            productStockQuantity: 10, 
            productCategory: "Test", 
            saleItemQuantity: 3
            );

        //reduce sale item quantity
        var payload = new
        {
            Amount = 2
        };

        var response = await _client.PatchAsJsonAsync($"/api/SaleItems/{sale.Id}/items/{saleItem.Id}/reduce", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");

        var saleAfterReducing = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(saleAfterReducing, Is.Not.Null);
        Assert.That(saleAfterReducing.SaleItems.Count, Is.GreaterThan(0));

        var saleItemAfterReducing = saleAfterReducing.SaleItems.Find(si => si.ProductId == product.Id);
        Assert.That(saleItemAfterReducing, Is.Not.Null);
        Assert.That(saleItemAfterReducing.Quantity, Is.EqualTo(saleItem.Quantity - payload.Amount));

        Assert.That(saleAfterReducing.ReductionFrequency, Is.EqualTo(sale.ReductionFrequency + 1));
        Assert.That(saleAfterReducing.TotalQuantityReduction, Is.EqualTo(sale.TotalQuantityReduction + payload.Amount));
    }

    [Test]
    public async Task ReduceSaleItemQuantity_ReducesToZero_ReturnsOk()
    {   
        //setup: create product, create sale, create sale item
        var (product, sale, saleItem) = await CreateSaleWithItemAsync(
            productName: "Test Product", 
            productSku: _skuCounter.ToString(), 
            productPrice: 99.99m, 
            productStockQuantity: 10, 
            productCategory: "Test", 
            saleItemQuantity: 3,
            productLowStockLevel: 0
            );

        //reduce sale item quantity to zero (0)
        var payload = new
        {
            Amount = saleItem.Quantity
        };

        var response = await _client.PatchAsJsonAsync($"/api/SaleItems/{sale.Id}/items/{saleItem.Id}/reduce", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");

        var saleAfterReducing = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(saleAfterReducing, Is.Not.Null);

        var saleItemAfterReducing = saleAfterReducing.SaleItems.Find(si => si.ProductId == product.Id);
        Assert.That(saleItemAfterReducing, Is.Null);
    }

    [Test]
    public async Task CloseSale_ValidSale_ReturnsOk()
    {   
        //setup: create product, create sale, create sale item
        var (product, sale, saleItem) = await CreateSaleWithItemAsync(
            productName: "Test Product", 
            productSku: _skuCounter.ToString(), 
            productPrice: 99.99m, 
            productStockQuantity: 10, 
            productCategory: "Test", 
            saleItemQuantity: 3,
            productLowStockLevel: 0
            );
        

        //close sale
        var response = await _client.PostAsync($"/api/Sales/{sale.Id}/close", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}.");

        var saleAfterClosing = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(saleAfterClosing, Is.Not.Null);
        Assert.That(saleAfterClosing.IsClosed, Is.True);
        _isCreatedSaleClosed = true;

        var saleItemAfterClosing = saleAfterClosing.SaleItems.Find(si => si.ProductId == product.Id);
        Assert.That(saleItemAfterClosing, Is.Not.Null);
        Assert.That(saleItemAfterClosing.Quantity, Is.EqualTo(saleItem.Quantity));

        var responseGetProduct = await _client.GetAsync($"/api/Products/{product.Id}");
        Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {responseGetProduct.StatusCode}.");
        
        var productAfterClosing = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.That(productAfterClosing, Is.Not.Null);
        Assert.That(productAfterClosing.StockQuantity, Is.EqualTo(product.StockQuantity - saleItemAfterClosing.Quantity));
    }

    
    [TearDown]
    public async Task DeleteTestSaleAndProduct()
    {
        if (_createdSaleId != null)
        {            
            try
            {
                if(_isCreatedSaleClosed)
                {
                    TestContext.Progress.WriteLine($"Skipped sale cleanup: sale {_createdSaleId} was closed — cancellation blocked by design (audit trail preserved).");
                }
                else
                {
                    var response = await _client.PutAsync($"/api/Sales/{_createdSaleId}/cancel", null);
                    if(response.StatusCode != HttpStatusCode.NoContent)
                    {
                        throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Warning: Failure in cancelling a sale with an Id of {_createdSaleId}: {ex.Message}");
            }
        }
        if (_createdProductId != null)
        {
            try
            {
                if(_isCreatedSaleClosed)
                {
                    TestContext.Progress.WriteLine($"Skipped product cleanup: product {_createdProductId} has existing reference to a sale item in sale {_createdSaleId} - deletion blocked by design (audit trail preserved).");
                }
                else
                {
                    var response = await _client.DeleteAsync($"/api/Products/{_createdProductId}");
                    if(response.StatusCode != HttpStatusCode.NoContent)
                    {
                        throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Warning: Failure in deleting a product with Id {_createdProductId}: {ex.Message}");
            }
        }
        _createdSaleId = null;
        _createdProductId = null;
        _isCreatedSaleClosed = false;
    }

    private async Task <(HttpResponseMessage Response, SaleResponse? Sale)> CreateTestSaleAsync(DateTime? saleDate = null, string? paymentNote = null, PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        var payload = new { 
        SaleDate = saleDate ?? DateTime.Now, 
        PaymentMethod = paymentMethod, 
        PaymentNote = paymentNote 
        };
        
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;
        
        return (response, sale);
    }

    private async Task <(HttpResponseMessage Response, ProductResponse? Product)> CreateTestProductAsync(string name, string sku, decimal price, int stockQuantity, string category, int lowStockLevel = 0)
    {
        var payload = new 
        { 
            name, sku, price, stockQuantity, category, lowStockLevel
        };
        var response = await _client.PostAsJsonAsync("/api/Products", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created(), but received {response.StatusCode}");
        
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.That(product, Is.Not.Null);
        _createdProductId = product.Id;
        _skuCounter ++;

        return (response, product);
    }

    private async Task<(ProductResponse Product, SaleResponse Sale, SaleItemResponse SaleItem)> CreateSaleWithItemAsync(string productName, string productSku, decimal productPrice, int productStockQuantity, string productCategory, int saleItemQuantity, int productLowStockLevel = 0)
    {
        var (_, product) = await CreateTestProductAsync(productName, productSku, productPrice, productStockQuantity, productCategory, productLowStockLevel);
        var (_, sale) = await CreateTestSaleAsync(DateTime.Now);

        var payload = new 
        { 
            ProductId = product!.Id, 
            Quantity = saleItemQuantity, 
            UnitPriceAtSale = product.Price 
        };

        var response = await _client.PostAsJsonAsync($"/api/SaleItems/{sale!.Id}/items", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");

        var updatedSale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        Assert.That(updatedSale, Is.Not.Null);
        Assert.That(updatedSale.SaleItems.Count, Is.GreaterThan(0));

        var saleItem = updatedSale!.SaleItems.Find(si => si.ProductId == product.Id);
        Assert.That(saleItem, Is.Not.Null);

        return (product, updatedSale, saleItem!);
    }
}
