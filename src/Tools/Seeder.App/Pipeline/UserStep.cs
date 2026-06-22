using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 1: Create users and obtain auth tokens.
/// Populates the SellerRegistry with tokens for all sellers.
/// Returns the admin token.
/// </summary>
public class UserStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;
    private readonly SellerRegistry _sellers;

    public UserStep(HttpClient client, ILogger logger, string dataDirectory, SellerRegistry sellers)
    {
        _client = client;
        _logger = logger;
        _dataDirectory = dataDirectory;
        _sellers = sellers;
    }

    public async Task<string> ExecuteAsync(CancellationToken ct)
    {
        var userSeeder = new UserSeeder(_client, _logger);
        var users = await SeedDataLoader.LoadJsonAsync<List<UserModel>>(_dataDirectory, "users.json");
        var adminUser = users.First(u => u.Role == "Admin");

        // Login admin
        var adminToken = await userSeeder.LoginAsync(adminUser.Email, adminUser.Password, ct);

        // Register all sellers
        var sellers = users.Where(u => u.Role == "Seller").ToList();
        _logger.LogInformation("Setting up {Count} sellers...", sellers.Count);

        foreach (var seller in sellers)
            await userSeeder.EnsureUserExistsAsync(seller, ct);

        await userSeeder.PromoteSellersAsync(users, adminToken, ct);

        // Login each seller and register in SellerRegistry
        foreach (var seller in sellers)
        {
            var token = await userSeeder.LoginAsync(seller.Email, seller.Password, ct);
            _sellers.Register(seller.Email, token);
        }

        _logger.LogInformation("Waiting for identity events to settle...");
        await Task.Delay(2000, ct);

        return adminToken;
    }
}
