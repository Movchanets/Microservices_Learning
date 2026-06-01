using System.Runtime.CompilerServices;

// Test projects
[assembly: InternalsVisibleTo("Catalog.UnitTests")]
[assembly: InternalsVisibleTo("Cart.UnitTests")]
[assembly: InternalsVisibleTo("Ordering.UnitTests")]
[assembly: InternalsVisibleTo("Payment.UnitTests")]
[assembly: InternalsVisibleTo("Media.UnitTests")]
[assembly: InternalsVisibleTo("Identity.UnitTests")]
[assembly: InternalsVisibleTo("Inventory.UnitTests")]
[assembly: InternalsVisibleTo("StoreManagement.UnitTests")]
[assembly: InternalsVisibleTo("Notification.UnitTests")]
[assembly: InternalsVisibleTo("Search.UnitTests")]
[assembly: InternalsVisibleTo("ApiGateway.UnitTests")]
[assembly: InternalsVisibleTo("BuildingBlocks.Infrastructure.UnitTests")]
[assembly: InternalsVisibleTo("BuildingBlocks.SharedContracts.UnitTests")]
[assembly: InternalsVisibleTo("Catalog.IntegrationTests")]
[assembly: InternalsVisibleTo("Cart.IntegrationTests")]
[assembly: InternalsVisibleTo("Ordering.IntegrationTests")]
[assembly: InternalsVisibleTo("Inventory.IntegrationTests")]
[assembly: InternalsVisibleTo("Identity.IntegrationTests")]
[assembly: InternalsVisibleTo("ApiGateway.IntegrationTests")]

// Domain projects that need to set Id in factory methods (e.g., saga correlation IDs)
[assembly: InternalsVisibleTo("Ordering.Domain")]
[assembly: InternalsVisibleTo("Identity.Domain")]
[assembly: InternalsVisibleTo("StoreManagement.Domain")]
