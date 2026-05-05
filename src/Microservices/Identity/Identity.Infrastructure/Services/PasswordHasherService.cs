using System.Security.Cryptography;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Identity.Domain.Aggregates;

namespace Identity.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(null!, password);

    public bool Verify(string password, string hashedPassword) =>
        _hasher.VerifyHashedPassword(null!, hashedPassword, password)
            != PasswordVerificationResult.Failed;
}
