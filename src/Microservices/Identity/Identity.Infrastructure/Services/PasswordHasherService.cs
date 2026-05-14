using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Provides password hashing and verification services.
/// </summary>
public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A securely hashed password string.</returns>
    public string Hash(string password) =>
        // Rationale: We pass null for the user parameter because the default PBKDF2 implementation in Identity v3
        // does not actually use the user instance for salting. It generates a random salt per hash.
        _hasher.HashPassword(null!, password);

    /// <summary>
    /// Verifies that a plaintext password matches a previously hashed password.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public bool Verify(string password, string hashedPassword) =>
        _hasher.VerifyHashedPassword(null!, hashedPassword, password) != PasswordVerificationResult.Failed;
}
