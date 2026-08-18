using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VapeShopInventoryAPI.Api;
using VapeShopInventoryAPI.Api.DTOs;
namespace VapeShopInventoryAPI.Tests;

public class RestockApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private List<int> _createdProductIds = new();
    private List<int> _restockedProductIds = new();
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
    [Test]
    public async Task CreateRestock_ValidSingleRestockRequest_ReturnsOk()
    {
        var (_, product) = await CreateTestProduct();

        int testQuantity = 11;
        decimal testUnitCost = 149.99m;
        DateTime testDate = new DateTime(2026, 01, 01);
        string? testPaymentNote = "test payment note";
        PaymentMethod testPaymentMethod = PaymentMethod.Cash;
        var items = new List<RestockItemRequest> { new RestockItemRequest {ProductId = product.Id, Quantity = testQuantity, UnitCost = testUnitCost} };
        
        var (responseMessage, restockResponse) = await RestockTestProducts(items, date: testDate, paymentNote: testPaymentNote, paymentMethod: testPaymentMethod);
        Assert.That(responseMessage.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseMessage.StatusCode}");
        Assert.That(restockResponse, Is.Not.Null);

        Assert.That(restockResponse.Expense.Date, Is.EqualTo(testDate));
        Assert.That(restockResponse.Expense.Amount, Is.EqualTo(items.Sum(i => i.Quantity * i.UnitCost)));
        Assert.That(restockResponse.Expense.Category, Is.EqualTo(Expense.RestockCategory));
        Assert.That(restockResponse.Expense.PaymentNote, Is.EqualTo(testPaymentNote));
        Assert.That(restockResponse.Expense.PaymentMethod, Is.EqualTo(testPaymentMethod));

        Assert.That(restockResponse.DeliveryItems.Count, Is.EqualTo(items.Count));
        Assert.That(restockResponse.DeliveryItems[0].ProductId, Is.EqualTo(items[0].ProductId));
        Assert.That(restockResponse.DeliveryItems[0].ExpenseId, Is.EqualTo(restockResponse.Expense.Id));
        Assert.That(restockResponse.DeliveryItems[0].Quantity, Is.EqualTo(items[0].Quantity));
        Assert.That(restockResponse.DeliveryItems[0].UnitCost, Is.EqualTo(items[0].UnitCost));
        Assert.That(restockResponse.DeliveryItems[0].TotalCost, Is.EqualTo(items[0].UnitCost * items[0].Quantity));

        Assert.That(restockResponse.UpdatedProducts.Count, Is.EqualTo(1));
        Assert.That(restockResponse.UpdatedProducts[0].Id, Is.EqualTo(product.Id));
        Assert.That(restockResponse.UpdatedProducts[0].Name, Is.EqualTo(product.Name));
        Assert.That(restockResponse.UpdatedProducts[0].Sku, Is.EqualTo(product.Sku));
        Assert.That(restockResponse.UpdatedProducts[0].Price, Is.EqualTo(product.Price));
        Assert.That(restockResponse.UpdatedProducts[0].StockQuantity, Is.EqualTo(items[0].Quantity + product.StockQuantity));
        Assert.That(restockResponse.UpdatedProducts[0].LowStockLevel, Is.EqualTo(product.LowStockLevel));
        Assert.That(restockResponse.UpdatedProducts[0].Category, Is.EqualTo(product.Category));
        Assert.That(restockResponse.UpdatedProducts[0].CreatedAt, Is.EqualTo(product.CreatedAt));
    }

    [Test]
    public async Task CreateRestock_ValidMultipleRestockRequest_ReturnsOk()
    {
        
        var (_, productA) = await CreateTestProduct(name: "Test Product A", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Test");
        var (_, productB) = await CreateTestProduct(name: "Test Product B", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Test");
        var (_, productC) = await CreateTestProduct(name: "Test Product C", price: 399.99m, stockQuantity: 30, lowStockLevel: 9, category: "Test");
        var createdProducts = new List<ProductResponse> { productA, productB, productC };

        int testQuantityA = 11;
        int testQuantityB = 22;
        int testQuantityC = 33;
        decimal testUnitCostA = 149.99m;
        decimal testUnitCostB = 249.99m;
        decimal testUnitCostC = 349.99m;
        DateTime testDate = new DateTime(2026, 01, 01);
        string? testPaymentNote = "test payment note";
        PaymentMethod testPaymentMethod = PaymentMethod.Cash;
        var items = new List<RestockItemRequest> 
        { 
            new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityA, UnitCost = testUnitCostA}, 
            new RestockItemRequest {ProductId = productB.Id, Quantity = testQuantityB, UnitCost = testUnitCostB},
            new RestockItemRequest {ProductId = productC.Id, Quantity = testQuantityC, UnitCost = testUnitCostC}
        };

        var (responseMessage, restockResponse) = await RestockTestProducts(items, date: testDate, paymentNote: testPaymentNote, paymentMethod: testPaymentMethod);
        Assert.That(responseMessage.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseMessage.StatusCode}");
        Assert.That(restockResponse, Is.Not.Null);

        Assert.That(restockResponse.Expense.Date, Is.EqualTo(testDate));
        Assert.That(restockResponse.Expense.Amount, Is.EqualTo(items.Sum(i => i.Quantity * i.UnitCost)));
        Assert.That(restockResponse.Expense.Category, Is.EqualTo(Expense.RestockCategory));
        Assert.That(restockResponse.Expense.PaymentNote, Is.EqualTo(testPaymentNote));
        Assert.That(restockResponse.Expense.PaymentMethod, Is.EqualTo(testPaymentMethod));

        Assert.That(restockResponse.DeliveryItems.Count, Is.GreaterThan(0));
        Assert.That(restockResponse.DeliveryItems.Count, Is.EqualTo(items.Count));
        for (int x = 0; x < restockResponse.DeliveryItems.Count; x++)
        {
            Assert.That(restockResponse.DeliveryItems[x].ProductId, Is.EqualTo(items[x].ProductId));
            Assert.That(restockResponse.DeliveryItems[x].ExpenseId, Is.EqualTo(restockResponse.Expense.Id));
            Assert.That(restockResponse.DeliveryItems[x].Quantity, Is.EqualTo(items[x].Quantity));
            Assert.That(restockResponse.DeliveryItems[x].UnitCost, Is.EqualTo(items[x].UnitCost));
            Assert.That(restockResponse.DeliveryItems[x].TotalCost, Is.EqualTo(items[x].UnitCost * items[x].Quantity));
        }

        var groupedByProduct = items.GroupBy(item => item.ProductId);
        foreach (var group in groupedByProduct)
        {
            int productId = group.Key;
            int totalQuantity = group.Sum(item => item.Quantity);

            var updatedProduct = restockResponse.UpdatedProducts.Find(p => p.Id == productId);
            Assert.That(updatedProduct, Is.Not.Null);
            
            var originalProduct = createdProducts.Find(p => p.Id == updatedProduct.Id);
            Assert.That(originalProduct, Is.Not.Null);
            
            Assert.That(updatedProduct.StockQuantity, Is.EqualTo(totalQuantity + originalProduct.StockQuantity));
            Assert.That(updatedProduct.Name, Is.EqualTo(originalProduct.Name));
            Assert.That(updatedProduct.Sku, Is.EqualTo(originalProduct.Sku));
            Assert.That(updatedProduct.Price, Is.EqualTo(originalProduct.Price));
            Assert.That(updatedProduct.LowStockLevel, Is.EqualTo(originalProduct.LowStockLevel));
            Assert.That(updatedProduct.Category, Is.EqualTo(originalProduct.Category));
            Assert.That(updatedProduct.CreatedAt, Is.EqualTo(originalProduct.CreatedAt));
        }
    }

    [Test]
    public async Task CreateRestock_InvalidProductIdAllOrNothing_ReturnsNotFound()
    {
        var responseGetExpensesA = await _client.GetAsync("api/Expenses");
        var expensesA = await responseGetExpensesA.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        var (_, productA) = await CreateTestProduct(name: "Test Product A", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Test");
        var (_, productB) = await CreateTestProduct(name: "Test Product B", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Test");
        var (_, productC) = await CreateTestProduct(name: "Test Product C", price: 399.99m, stockQuantity: 30, lowStockLevel: 9, category: "Test");
        var createdProducts = new List<ProductResponse> { productA, productB, productC };

        int testInvalidId = -1;

        int testQuantityA = 11;
        int testQuantityB = 22;
        int testQuantityC = 33;
        decimal testUnitCostA = 149.99m;
        decimal testUnitCostB = 249.99m;
        decimal testUnitCostC = 349.99m;

        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "Default restock test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityA, UnitCost = testUnitCostA}, 
                new RestockItemRequest {ProductId = productB.Id, Quantity = testQuantityB, UnitCost = testUnitCostB},
                new RestockItemRequest {ProductId = productC.Id, Quantity = testQuantityC, UnitCost = testUnitCostC},
                new RestockItemRequest {ProductId = testInvalidId, Quantity = 1, UnitCost = 1m}
            },
            PaymentNote = "test payment note",
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expecting 404 Not Found() status, but received {response.StatusCode}");

        var responseGetExpensesB = await _client.GetAsync("api/Expenses");
        var expensesB = await responseGetExpensesB.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesB?.Count, Is.EqualTo(expensesA?.Count));

        foreach(ProductResponse pr in createdProducts)
        {
            var responseGetProduct = await _client.GetAsync($"api/Products/{pr.Id}");
            Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseGetProduct.StatusCode}");

            var responseProduct = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>();
            Assert.That(responseProduct, Is.Not.Null);
            Assert.That(responseProduct.StockQuantity, Is.EqualTo(pr.StockQuantity));
            Assert.That(responseProduct.Name, Is.EqualTo(pr.Name));
            Assert.That(responseProduct.Sku, Is.EqualTo(pr.Sku));
            Assert.That(responseProduct.Price, Is.EqualTo(pr.Price));
            Assert.That(responseProduct.LowStockLevel, Is.EqualTo(pr.LowStockLevel));
            Assert.That(responseProduct.Category, Is.EqualTo(pr.Category));
            Assert.That(responseProduct.CreatedAt, Is.EqualTo(pr.CreatedAt));
        }  
    }

    [Test]
    public async Task CreateRestock_DuplicateProductIdDifferentUnitCost_ReturnsOk()
    {
        var (_, productA) = await CreateTestProduct(name: "Test Product A", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Test");
    
        int testQuantityA = 11;
        int testQuantityB = 22;
        decimal testUnitCostA = 149.99m;
        decimal testUnitCostB = 249.99m;

        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "Default restock test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityA, UnitCost = testUnitCostA}, 
                new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityB, UnitCost = testUnitCostB}
            },
            PaymentNote = "test payment note",
            PaymentMethod = PaymentMethod.Cash
        };

        var responseMessage = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(responseMessage.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseMessage.StatusCode}");

        var restockResponse = await responseMessage.Content.ReadFromJsonAsync<RestockResponse>();
        Assert.That(restockResponse, Is.Not.Null);
        _restockedProductIds.Add(productA.Id);

        Assert.That(restockResponse.Expense.Date, Is.EqualTo(payload.Date));
        Assert.That(restockResponse.Expense.Amount, Is.EqualTo(payload.Items.Sum(i => i.Quantity * i.UnitCost)));
        Assert.That(restockResponse.Expense.Category, Is.EqualTo(Expense.RestockCategory));
        Assert.That(restockResponse.Expense.PaymentNote, Is.EqualTo(payload.PaymentNote));
        Assert.That(restockResponse.Expense.PaymentMethod, Is.EqualTo(payload.PaymentMethod));

        Assert.That(restockResponse.DeliveryItems.Count, Is.EqualTo(2));
        Assert.That(restockResponse.DeliveryItems.Count, Is.EqualTo(payload.Items.Count));
        for (int x = 0; x < restockResponse.DeliveryItems.Count; x++)
        {
            Assert.That(restockResponse.DeliveryItems[x].ProductId, Is.EqualTo(payload.Items[x].ProductId));
            Assert.That(restockResponse.DeliveryItems[x].ExpenseId, Is.EqualTo(restockResponse.Expense.Id));
            Assert.That(restockResponse.DeliveryItems[x].Quantity, Is.EqualTo(payload.Items[x].Quantity));
            Assert.That(restockResponse.DeliveryItems[x].UnitCost, Is.EqualTo(payload.Items[x].UnitCost));
            Assert.That(restockResponse.DeliveryItems[x].TotalCost, Is.EqualTo(payload.Items[x].UnitCost * payload.Items[x].Quantity));
        }

        Assert.That(restockResponse.UpdatedProducts.Count, Is.EqualTo(1));
        var responseGetProduct = await _client.GetAsync($"api/Products/{productA.Id}");
        Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseGetProduct.StatusCode}");

        var updatedProduct = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.That(updatedProduct, Is.Not.Null);
        Assert.That(updatedProduct.StockQuantity, Is.EqualTo(productA.StockQuantity + testQuantityA + testQuantityB));
        Assert.That(updatedProduct.Name, Is.EqualTo(productA.Name));
        Assert.That(updatedProduct.Sku, Is.EqualTo(productA.Sku));
        Assert.That(updatedProduct.Price, Is.EqualTo(productA.Price));
        Assert.That(updatedProduct.LowStockLevel, Is.EqualTo(productA.LowStockLevel));
        Assert.That(updatedProduct.Category, Is.EqualTo(productA.Category));
        Assert.That(updatedProduct.CreatedAt, Is.EqualTo(productA.CreatedAt));
    }

    [Test]
    public async Task CreateRestock_InvalidQuantityAllOrNothing_ReturnsBadRequest()
    {
        var responseGetExpensesA = await _client.GetAsync("api/Expenses");
        var expensesA = await responseGetExpensesA.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        var (_, productA) = await CreateTestProduct(name: "Test Product A", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Test");
        var (_, productB) = await CreateTestProduct(name: "Test Product B", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Test");
        var createdProducts = new List<ProductResponse> { productA, productB };

        int testQuantityA = 11;
        int testQuantityB = 0;
        decimal testUnitCostA = 149.99m;
        decimal testUnitCostB = 249.99m;

        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "Default restock test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityA, UnitCost = testUnitCostA}, 
                new RestockItemRequest {ProductId = productB.Id, Quantity = testQuantityB, UnitCost = testUnitCostB}
            },
            PaymentNote = "test payment note",
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expecting 400 Bad Request() status, but received {response.StatusCode}");

        var responseGetExpensesB = await _client.GetAsync("api/Expenses");
        var expensesB = await responseGetExpensesB.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesB?.Count, Is.EqualTo(expensesA?.Count));

        foreach(ProductResponse pr in createdProducts)
        {
            var responseGetProduct = await _client.GetAsync($"api/Products/{pr.Id}");
            Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseGetProduct.StatusCode}");

            var responseProduct = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>();
            Assert.That(responseProduct, Is.Not.Null);
            Assert.That(responseProduct.StockQuantity, Is.EqualTo(pr.StockQuantity));
            Assert.That(responseProduct.Name, Is.EqualTo(pr.Name));
            Assert.That(responseProduct.Sku, Is.EqualTo(pr.Sku));
            Assert.That(responseProduct.Price, Is.EqualTo(pr.Price));
            Assert.That(responseProduct.LowStockLevel, Is.EqualTo(pr.LowStockLevel));
            Assert.That(responseProduct.Category, Is.EqualTo(pr.Category));
            Assert.That(responseProduct.CreatedAt, Is.EqualTo(pr.CreatedAt));
        }  
    }

    [Test]
    public async Task CreateRestock_InvalidUnitCostAllOrNothing_ReturnsBadRequest()
    {
        var responseGetExpensesA = await _client.GetAsync("api/Expenses");
        var expensesA = await responseGetExpensesA.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        var (_, productA) = await CreateTestProduct(name: "Test Product A", price: 199.99m, stockQuantity: 10, lowStockLevel: 3, category: "Test");
        var (_, productB) = await CreateTestProduct(name: "Test Product B", price: 299.99m, stockQuantity: 20, lowStockLevel: 6, category: "Test");
        var createdProducts = new List<ProductResponse> { productA, productB };

        int testQuantityA = 11;
        int testQuantityB = 22;
        decimal testUnitCostA = 149.99m;
        decimal testUnitCostB = -249.99m;

        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "Default restock test description",
            Items = new List<RestockItemRequest>
            {
                new RestockItemRequest {ProductId = productA.Id, Quantity = testQuantityA, UnitCost = testUnitCostA}, 
                new RestockItemRequest {ProductId = productB.Id, Quantity = testQuantityB, UnitCost = testUnitCostB}
            },
            PaymentNote = "test payment note",
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expecting 400 Bad Request() status, but received {response.StatusCode}");

        var responseGetExpensesB = await _client.GetAsync("api/Expenses");
        var expensesB = await responseGetExpensesB.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesB?.Count, Is.EqualTo(expensesA?.Count));

        foreach(ProductResponse pr in createdProducts)
        {
            var responseGetProduct = await _client.GetAsync($"api/Products/{pr.Id}");
            Assert.That(responseGetProduct.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expecting 200 Ok() status, but received {responseGetProduct.StatusCode}");

            var responseProduct = await responseGetProduct.Content.ReadFromJsonAsync<ProductResponse>();
            Assert.That(responseProduct, Is.Not.Null);
            Assert.That(responseProduct.StockQuantity, Is.EqualTo(pr.StockQuantity));
            Assert.That(responseProduct.Name, Is.EqualTo(pr.Name));
            Assert.That(responseProduct.Sku, Is.EqualTo(pr.Sku));
            Assert.That(responseProduct.Price, Is.EqualTo(pr.Price));
            Assert.That(responseProduct.LowStockLevel, Is.EqualTo(pr.LowStockLevel));
            Assert.That(responseProduct.Category, Is.EqualTo(pr.Category));
            Assert.That(responseProduct.CreatedAt, Is.EqualTo(pr.CreatedAt));
        }  
    }

    [Test]
    public async Task CreateRestock_EmptyRestockItems_ReturnsBadRequest()
    {
        var responseGetExpensesA = await _client.GetAsync("api/Expenses");
        var expensesA = await responseGetExpensesA.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        var payload = new RestockRequest
        {
            Date = new DateTime(2026, 01, 01),
            Description = "Default restock test description",
            Items = new List<RestockItemRequest> { },
            PaymentNote = "test payment note",
            PaymentMethod = PaymentMethod.Cash
        };

        var response = await _client.PostAsJsonAsync("api/Restock", payload);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Expecting 400 Bad Request() status, but received {response.StatusCode}");

        var responseGetExpensesB = await _client.GetAsync("api/Expenses");
        var expensesB = await responseGetExpensesB.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.That(expensesB?.Count, Is.EqualTo(expensesA?.Count));
    }

    [TearDown]
    public async Task DeleteTestProduct()
    {
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
    public async Task<(HttpResponseMessage Response, ProductResponse Product)> CreateTestProduct(string name = "Test Product", string? sku = null, decimal price = 99.99m, int stockQuantity = 10, int lowStockLevel = 3, string category = "Test")
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

    public async Task<(HttpResponseMessage Response, RestockResponse Restock)> RestockTestProducts (List<RestockItemRequest> items, DateTime? date = null, string description = "Default restock test description", string? paymentNote = null, PaymentMethod paymentMethod = 0)
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
}