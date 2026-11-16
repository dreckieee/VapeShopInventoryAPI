using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace VapeShopInventoryAPI.Tests;

public class FallbackPolicyApiTests
{
    private CustomWebApplicationFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _factory = new CustomWebApplicationFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task AnyEndpointWithoutAuthAttribute_AnonymousClient_ReturnsUnauthorized()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(UnmarkedTestController).Assembly)));

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/test-only/unmarked");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"Expected 401 status, but received {response.StatusCode} instead.");
    }
}
