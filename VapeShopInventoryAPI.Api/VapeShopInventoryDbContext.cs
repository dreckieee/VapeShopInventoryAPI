using Microsoft.EntityFrameworkCore;

public class VapeShopInventoryDbContext : DbContext
{
    public DbSet<Product> Products {get; private set;}
    public VapeShopInventoryDbContext (DbContextOptions<VapeShopInventoryDbContext> options) : base (options){}
}