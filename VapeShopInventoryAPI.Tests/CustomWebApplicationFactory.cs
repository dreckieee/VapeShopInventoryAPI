using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Net.Http.Headers;
using VapeShopInventoryAPI.Api;

namespace VapeShopInventoryAPI.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string OwnerUsername = "test-owner-username";
    private const string OwnerPassword = "test-owner-password";
    private const string CashierUsername = "test-cashier-username";
    private const string CashierPassword = "test-cashier-password";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private bool _cashierSeeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Auth:OwnerUsername", OwnerUsername);
        builder.UseSetting("Auth:OwnerPassword", OwnerPassword);

        builder.UseSetting("Jwt:SigningKey", "test-json-web-token-signing-key-setting");

        builder.ConfigureServices(services =>
        {
           
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VapeShopInventoryDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            _connection.Open();

            
            services.AddDbContext<VapeShopInventoryDbContext>(options => {options.UseSqlite(_connection);}); 

            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();
                db.Database.EnsureCreated();
            }
        });
    }
    public HttpClient CreateOwnerClient()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

        var normalizedUsername = User.NormalizeUsername(OwnerUsername);
        var owner = db.Users.Single(u => u.Username == normalizedUsername);

        return CreateClientWithToken(owner);
    }

    public HttpClient CreateCashierClient()
    {
        EnsureCashierSeeded();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

        var normalizedUsername = User.NormalizeUsername(CashierUsername);
        var cashier = db.Users.Single(u => u.Username == normalizedUsername);

        return CreateClientWithToken(cashier);
    }

    private void EnsureCashierSeeded()
    {
        if (_cashierSeeded)
        {
            return;
        }

        var hasher = Services.GetRequiredService<IPasswordHasher<User>>();
        var passwordHash = hasher.HashPassword(null!, CashierPassword);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();

        db.Users.Add(new User(CashierUsername, passwordHash, UserRole.Cashier));
        db.SaveChanges();

        _cashierSeeded = true;
    }

    private HttpClient CreateClientWithToken(User user)
    {
        var tokenService = Services.GetRequiredService<TokenService>();
        var (token, _) = tokenService.CreateToken(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}