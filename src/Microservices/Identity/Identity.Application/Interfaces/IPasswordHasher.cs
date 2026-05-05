namespace Identity.Application.Interfaces;

/// <summary>
/// Abstraction for password hashing. Implemented in Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}
