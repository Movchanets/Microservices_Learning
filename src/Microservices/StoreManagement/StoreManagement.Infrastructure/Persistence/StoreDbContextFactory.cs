using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StoreManagement.Infrastructure.Persistence;

public class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StoreDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=store;Username=postgres;Password=postgres");

        return new StoreDbContext(optionsBuilder.Options);
    }
}
