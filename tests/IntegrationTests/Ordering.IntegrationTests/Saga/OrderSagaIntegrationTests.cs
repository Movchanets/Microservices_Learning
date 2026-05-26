using System.Text.Json;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Cart;
using BuildingBlocks.SharedContracts.Events.Inventory;
using BuildingBlocks.SharedContracts.Events.Ordering;
using BuildingBlocks.SharedContracts.Events.Payment;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Ordering.IntegrationTests.Saga;

/// <summary>
/// Saga integration tests verifying OrderStateMachine state transitions
/// persist correctly to Redis. Uses real InMemory bus + real Redis.
///
/// MassTransit's RedisRepository stores saga state as JSON strings keyed
/// by CorrelationId (GUID with dashes format).
/// </summary>
[Collection("Ordering collection")]
public class OrderSagaIntegrationTests
{
    private readonly OrderingDatabaseFixture _fixture;

    public OrderSagaIntegrationTests(OrderingDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OrderSubmitted_PersistsSagaStateInRedis()
    {
        var correlationId = Guid.NewGuid();

        await _fixture.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-1",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Test Product", 2, 25.00m, Guid.NewGuid())],
            DateTime.UtcNow));

        var state = await PollForState(correlationId, "ReservingInventory", TimeSpan.FromSeconds(15));
        state.Should().NotBeNull("saga state should exist in Redis after OrderSubmitted");
        state!.Value.GetProperty("currentState").GetString().Should().Be("ReservingInventory");
        state.Value.GetProperty("buyerId").GetString().Should().Be("buyer-1");
    }

    [Fact]
    public async Task FullFlow_PersistsAllStateTransitions()
    {
        var correlationId = Guid.NewGuid();

        // Step 1: OrderSubmitted → ReservingInventory
        await _fixture.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-2",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Test Product", 1, 50.00m, Guid.NewGuid()),
             new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Test Product", 3, 15.00m, Guid.NewGuid())],
            DateTime.UtcNow));

        var state1 = await PollForState(correlationId, "ReservingInventory", TimeSpan.FromSeconds(15));
        state1.Should().NotBeNull("step 1: should be in ReservingInventory");
        state1!.Value.GetProperty("currentState").GetString().Should().Be("ReservingInventory");

        // Step 2: InventoryReserved → ProcessingPayment
        await _fixture.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));

        var state2 = await PollForState(correlationId, "ProcessingPayment", TimeSpan.FromSeconds(15));
        state2.Should().NotBeNull("step 2: should be in ProcessingPayment");
        state2!.Value.GetProperty("currentState").GetString().Should().Be("ProcessingPayment");

        // Step 3: PaymentCompleted → Completed
        await _fixture.Bus.Publish(new PaymentCompletedEvent(
            correlationId, correlationId, "TX-001"));

        var state3 = await PollForState(correlationId, "Completed", TimeSpan.FromSeconds(15));
        state3.Should().NotBeNull("step 3: should be in Completed");
        state3!.Value.GetProperty("currentState").GetString().Should().Be("Completed");
    }

    [Fact]
    public async Task PaymentFailed_PersistsCancelledState()
    {
        var correlationId = Guid.NewGuid();

        // Step 1: OrderSubmitted → ReservingInventory
        await _fixture.Bus.Publish(new OrderSubmittedEvent(
            correlationId, "buyer-3",
            [new OrderItemContract(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU", "Test Product", 5, 10.00m, Guid.NewGuid())],
            DateTime.UtcNow));

        var state1 = await PollForState(correlationId, "ReservingInventory", TimeSpan.FromSeconds(15));
        state1.Should().NotBeNull("step 1: should be in ReservingInventory");

        // Step 2: InventoryReserved → ProcessingPayment
        await _fixture.Bus.Publish(new InventoryReservedEvent(correlationId, correlationId));

        var state2 = await PollForState(correlationId, "ProcessingPayment", TimeSpan.FromSeconds(15));
        state2.Should().NotBeNull("step 2: should be in ProcessingPayment");

        // Step 3: PaymentFailed → Cancelled
        await _fixture.Bus.Publish(new PaymentFailedEvent(
            correlationId, correlationId, "Insufficient funds"));

        var state3 = await PollForState(correlationId, "Cancelled", TimeSpan.FromSeconds(15));
        state3.Should().NotBeNull("step 3: should be in Cancelled");
        state3!.Value.GetProperty("currentState").GetString().Should().Be("Cancelled");
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Polls Redis until the saga reaches the expected state.
    /// MassTransit RedisRepository stores saga state as a JSON string
    /// keyed by CorrelationId in "D" format (with dashes).
    /// </summary>
    private async Task<JsonElement?> PollForState(
        Guid correlationId, string expectedState, TimeSpan timeout)
    {
        var redis = _fixture.ServiceProvider.GetRequiredService<ConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var key = correlationId.ToString("D");
            var val = await db.StringGetAsync(key);

            if (val.HasValue)
            {
                var state = JsonSerializer.Deserialize<JsonElement>(val.ToString());
                var currentState = state.GetProperty("currentState").GetString();
                if (currentState == expectedState)
                    return state;
            }

            await Task.Delay(250);
        }

        return null;
    }
}
