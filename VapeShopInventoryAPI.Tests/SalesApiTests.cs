using System.Net;
using System.Net.Http.Json;

namespace VapeShopInventoryAPI.Tests;

public class SalesApiTests
{   
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private int? _createdSaleId;
    private int testInvalidId = -1;
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
        var response = await _client.GetAsync($"/api/Sales/{testInvalidId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Expected 404 NotFound() status, but received {response.StatusCode}");
    }

    [Test]
    public async Task CreateSale_ValidSaleRequest_ReturnsCreated()
    {
        var saleDate = DateTime.Now;
        var (responseCreate, sale) = await CreateTestSaleAsync(saleDate);
        Assert.That(responseCreate.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Expected 201 Created() status, but received {responseCreate.StatusCode}");

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

        var response = await _client.GetAsync($"/api/Sales/{sale.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected 200 Ok() status, but received {response.StatusCode}");
    }

    [TearDown]
    public async Task DeleteTestSale()
    {
        if (_createdSaleId != null)
        {
            try
            {
                var response = await _client.PutAsync($"/api/Sales/{_createdSaleId}/cancel", null);
                if(response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new Exception($"Expected 204 NoContent() status, but received {response.StatusCode}");
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
    }

    private async Task <(HttpResponseMessage Response, SaleResponse? Sale)> CreateTestSaleAsync(DateTime saleDate)
    {
        var payload = new { saleDate };
        var response = await _client.PostAsJsonAsync("/api/Sales", payload);
        var sale = await response.Content.ReadFromJsonAsync<SaleResponse>();
        return (response, sale);
    }
}
