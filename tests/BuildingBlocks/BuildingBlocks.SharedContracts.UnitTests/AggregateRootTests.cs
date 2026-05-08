using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Xunit;

namespace BuildingBlocks.SharedContracts.UnitTests;

public class AggregateRootTests
{
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private class TestAggregateRoot : AggregateRoot
    {
        public TestAggregateRoot() : base()
        {
        }

        public void DoSomething()
        {
            AddDomainEvent(new TestDomainEvent());
        }
    }

    [Fact]
    public void Constructor_InitializesEmptyDomainEventsList()
    {
        // Arrange & Act
        var aggregateRoot = new TestAggregateRoot();

        // Assert
        aggregateRoot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_AddsEventToDomainEventsList()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();

        // Act
        aggregateRoot.DoSomething();

        // Assert
        aggregateRoot.DomainEvents.Should().HaveCount(1);
        aggregateRoot.DomainEvents.First().Should().BeOfType<TestDomainEvent>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEventsFromList()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        aggregateRoot.DoSomething();
        aggregateRoot.DoSomething();

        // Ensure events are added
        aggregateRoot.DomainEvents.Should().HaveCount(2);

        // Act
        aggregateRoot.ClearDomainEvents();

        // Assert
        aggregateRoot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_IsReadOnlyAndReflectsChanges()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();

        // Act - Ensure it is an IReadOnlyList that reflects changes
        var readOnlyEvents = aggregateRoot.DomainEvents;
        aggregateRoot.DoSomething();

        // Assert
        readOnlyEvents.Should().HaveCount(1);

        // Cannot cast to mutable list
        var action = () => { var list = (IList<IDomainEvent>)readOnlyEvents; list.Clear(); };
        action.Should().Throw<NotSupportedException>();
    }
}
