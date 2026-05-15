# P2-01 — Integration Tests for Phase 6 Services

**Goal**: Add integration tests for StoreManagement and Media APIs.

**Fixes**: MISSING.md #8.1

**Depends on**: P0-01 (auth), P0-04 (cart auth)

---

## StoreManagement Integration Tests

### Project setup
```
tests/IntegrationTests/StoreManagement.IntegrationTests/
├── StoreManagement.IntegrationTests.csproj
├── StoreEndpointsTests.cs
└── Fixtures/
    └── StoreManagementFixture.cs
```

### Test fixture
Use Testcontainers for PostgreSQL:
```csharp
public class StoreManagementFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = null!;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Postgres = new PostgreSqlBuilder().Build();
        await Postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override connection string
                });
            });
    }
}
```

### Test cases
- `CreateStore_WithValidData_ReturnsCreated`
- `CreateStore_DuplicateSeller_ReturnsConflict`
- `VerifyStore_AsAdmin_ChangesStatus`
- `VerifyStore_AsNonAdmin_ReturnsForbidden`
- `GetStores_FilterByStatus_ReturnsFiltered`

## Media Integration Tests

### Test cases
- `UploadImage_ValidFile_ReturnsCreated`
- `UploadImage_GeneratesThumbnail`
- `GetMedia_ExistingBlob_ReturnsFile`
- `DeleteMedia_ExistingBlob_ReturnsNoContent`
- `UploadImage_OversizedFile_ReturnsBadRequest`

## Done When
- [ ] StoreManagement.IntegrationTests project created
- [ ] Media.IntegrationTests project created
- [ ] All test cases passing
- [ ] Tests use Testcontainers for PostgreSQL
