using Marketplace.IntegrationTests.Shared;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.API.Saga;
using Ordering.Infrastructure.Persistence;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Ordering.IntegrationTests;

public sealed class OrderingDatabaseFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = RedisContainerFactory.Create();

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public string RedisConnectionString { get; private set; } = null!;
    public IBus Bus { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        RedisConnectionString = _redisContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        services.AddMassTransit(cfg =>
        {
            cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .RedisRepository(r => r.DatabaseConfiguration(RedisConnectionString));

            cfg.UsingInMemory((context, busCfg) =>
            {
                busCfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton(ConnectionMultiplexer.Connect(RedisConnectionString));

        ServiceProvider = services.BuildServiceProvider(true);

        // Start the bus so saga consumers are active
        var busControl = ServiceProvider.GetRequiredService<IBusControl>();
        await busControl.StartAsync(TimeSpan.FromSeconds(30));
        Bus = busControl;
    }

    public async Task DisposeAsync()
    {
        var busControl = ServiceProvider.GetRequiredService<IBusControl>();
        await busControl.StopAsync(TimeSpan.FromSeconds(10));

        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        await _redisContainer.DisposeAsync();
    }

    public IServiceScope CreateScope() => ServiceProvider.CreateScope();
}

[CollectionDefinition("Ordering collection")]
public class OrderingCollection : ICollectionFixture<OrderingDatabaseFixture>
{
}
