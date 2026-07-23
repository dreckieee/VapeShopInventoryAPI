using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Net;
using System.Text.Json;

namespace VapeShopInventoryAPI.Tests;

public class SalesApiTests : PlaywrightTest
{   
    private int? _createdSaleId;
    private int testInvalidId = -1;
    private const string BaseUrl = "http://localhost:5208";
    private IAPIRequestContext _apiContext = null!;

    [SetUp]
    public async Task SetupApi()
    {
        _apiContext = await Playwright.APIRequest.NewContextAsync(new ()
        {
            BaseURL = BaseUrl
        }); 
    }

    [Test]
    public async Task GetSale_NonExistentId_ReturnsNotFound()
    {
        var response = await _apiContext.GetAsync($"/api/Sales/{testInvalidId}");
        Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.Status}");
    }

    [Test]
    public async Task CreateSale_ValidSaleRequest_ReturnsCreated()
    {
        var saleDate = DateTime.Now;
        var (responseCreate, sale) = await CreateTestSaleAsync(saleDate);
        Assert.That(responseCreate.Status, Is.EqualTo((int)HttpStatusCode.Created), $"Expected 201 Created() status, but received {responseCreate.Status}");

        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;

        Assert.That(saleDate, Is.EqualTo(sale.SaleDate));
    }

    [Test]
    public async Task GetSale_ExistingId_ReturnsOk()
    {
        var saleDate = DateTime.Now;
        var (responseCreate, sale) = await CreateTestSaleAsync(saleDate);

        Assert.That(sale, Is.Not.Null);
        _createdSaleId = sale.Id;

        var response = await _apiContext.GetAsync($"/api/Sales/{sale.Id}");
        Assert.That(response.Status, Is.EqualTo((int)HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.Status}");
    }

    [TearDown]
    public async Task Dispose()
    {
        if (_createdSaleId != null)
        {
            try
            {
                var response = await _apiContext.PutAsync($"/api/Sales/{_createdSaleId}/cancel");
                if(response.Status != (int)HttpStatusCode.NoContent)
                {
                    throw new Exception($"Expected 204 NoContent() status, but received {response.Status}");
                }
                
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Warning: Failure in cancelling a sale with an Id of {_createdSaleId}: {ex.Message}");
            }
            finally
            {
                _createdSaleId = null;
            }
        }
        await _apiContext.DisposeAsync();
    }

    private async Task <(IAPIResponse Response, SaleResponse? Sale)> CreateTestSaleAsync(DateTime saleDate)
    {
        var payLoad = new { saleDate };
        var response = await _apiContext.PostAsync("/api/Sales", new APIRequestContextOptions
        {
            DataObject = payLoad
        });
        var responseBody = await response.TextAsync();
        var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
        var sale = JsonSerializer.Deserialize<SaleResponse>(responseBody, options);

        return (response, sale);
    }
}
