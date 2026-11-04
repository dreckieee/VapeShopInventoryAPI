using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
namespace VapeShopInventoryAPI.Api;

public static class UserSeeder
{
    private const int MinOwnerPasswordLength = 12;
    public static async Task SeedAsync(VapeShopInventoryDbContext context, IConfiguration configuration, IPasswordHasher<User> hasher)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }
        string? ownerName = configuration["Auth:OwnerUsername"];
        string? ownerPassword = configuration["Auth:OwnerPassword"];
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new InvalidOperationException("Auth:OwnerUsername is not configured.");
        }
        if (string.IsNullOrWhiteSpace(ownerPassword))
        {
            throw new InvalidOperationException("Auth:OwnerPassword is not configured.");
        }
        if (ownerPassword.Length < MinOwnerPasswordLength)
        {
            throw new InvalidOperationException($"Auth:OwnerPassword must be at least {MinOwnerPasswordLength} characters long.");
        }

        var passwordHash = hasher.HashPassword(null!, ownerPassword); // the default hasher ignores the first argument.
        context.Users.Add(new User(ownerName, passwordHash, UserRole.Owner));
        await context.SaveChangesAsync();
    }
}
