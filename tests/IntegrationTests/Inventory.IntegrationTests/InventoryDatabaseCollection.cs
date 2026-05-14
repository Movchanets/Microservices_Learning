using Xunit;

namespace Inventory.IntegrationTests;

[CollectionDefinition("Database collection")]
public class InventoryDatabaseCollection : ICollectionFixture<InventoryDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
