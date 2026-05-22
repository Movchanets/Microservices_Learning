using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class UserSeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public UserSeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureUserExistsAsync(UserModel user, CancellationToken ct)
    {
        // Try to login first (Idempotency check)
        var response = await _client.PostAsJsonAsync("/api/identity/auth/login", new { user.Email, user.Password }, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("User already exists: {Email}", user.Email);
            return;
        }
        else if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Unexpected response checking user existence {Email}: {StatusCode} - {Error}", user.Email, response.StatusCode, err);
        }

        // Register if login failed (will default to Buyer)
        response = await _client.PostAsJsonAsync("/api/identity/auth/register", user, ct);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Created user: {Email}", user.Email);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to create user {Email}: {StatusCode} - {Error}", user.Email, response.StatusCode, error);
        }
    }

    public async Task PromoteSellersAsync(List<UserModel> users, string adminToken, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        // Fetch all users to get their IDs
        var response = await _client.GetAsync("/api/identity/users", ct);
        if (!response.IsSuccessStatusCode)
        {
             _logger.LogWarning("Failed to list users to promote sellers.");
             return;
        }

        var allUsers = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: ct) ?? new List<UserDto>();

        foreach (var user in users.Where(u => u.Role == "Seller"))
        {
            var existingUser = allUsers.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser != null && existingUser.Role != "Seller")
            {
                var roleResponse = await _client.PutAsJsonAsync($"/api/identity/users/{existingUser.Id}/role", new { Role = "Seller" }, ct);
                if (roleResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Promoted user {Email} to Seller.", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to promote user {Email}.", user.Email);
                }
            }
        }
    }

    public async Task<string> LoginAsync(string email, string password, CancellationToken ct)
    {
        var response = await _client.PostAsJsonAsync("/api/identity/auth/login", new { Email = email, Password = password }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Login failed for {Email}: {StatusCode} - {Error}", email, response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        return result!.AccessToken;
    }

    public async Task<Guid?> GetUserIdAsync(string email, string adminToken, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync("/api/identity/users", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to fetch users: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var allUsers = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: ct);
        return allUsers?.FirstOrDefault(u => u.Email == email)?.Id;
    }
}