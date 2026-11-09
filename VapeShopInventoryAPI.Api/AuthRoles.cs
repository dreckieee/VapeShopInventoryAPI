namespace VapeShopInventoryAPI.Api;

public static class AuthRoles
{
    public const string Owner = nameof(UserRole.Owner);
    public const string Cashier = nameof(UserRole.Cashier);
    public const string OwnerOrCashier = Owner + "," + Cashier;
}