using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// Makes the Identity PostgreSQL fixture available to all tests in the collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IdentityIntegrationCollection : ICollectionFixture<IdentityPostgresFixture>
{
    public const string Name = "Identity integration tests";
}
