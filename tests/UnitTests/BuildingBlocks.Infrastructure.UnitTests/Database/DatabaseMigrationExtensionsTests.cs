using BuildingBlocks.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BuildingBlocks.Infrastructure.UnitTests.Database;

public class DatabaseMigrationExtensionsTests
{
    /// <summary>
    /// Verifies that ApplyWithRetry retries on transient failures and eventually succeeds.
    /// This prevents Identity API from hanging on startup when PostgreSQL isn't ready.
    /// </summary>
    [Fact]
    public void ApplyWithRetry_RetriesOnTransientFailure_AndEventuallySucceeds()
    {
        // Arrange — simulate 2 failures then success
        var callCount = 0;
        void Migrate()
        {
            callCount++;
            if (callCount <= 2)
                throw new InvalidOperationException("Connection string has not been initialized.");
        }

        // Act
        var act = () => DatabaseMigrationExtensions.ApplyWithRetry(
            Migrate, "TestService", Mock.Of<ILogger>(),
            maxRetries: 3, delay: TimeSpan.Zero);

        // Assert — should not throw, callCount should be 3 (2 failures + 1 success)
        act.Should().NotThrow();
        callCount.Should().Be(3);
    }

    /// <summary>
    /// Verifies that ApplyWithRetry throws after exhausting all retries.
    /// </summary>
    [Fact]
    public void ApplyWithRetry_ThrowsAfterMaxRetries()
    {
        // Arrange — always fail
        var callCount = 0;
        void Migrate()
        {
            callCount++;
            throw new InvalidOperationException("Connection refused");
        }

        // Act
        var act = () => DatabaseMigrationExtensions.ApplyWithRetry(
            Migrate, "TestService", Mock.Of<ILogger>(),
            maxRetries: 3, delay: TimeSpan.Zero);

        // Assert — should throw after 3 attempts
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Connection refused");
        callCount.Should().Be(3);
    }

    /// <summary>
    /// Verifies that ApplyWithRetry succeeds on first try without retrying.
    /// </summary>
    [Fact]
    public void ApplyWithRetry_SucceedsOnFirstTry_NoRetry()
    {
        // Arrange
        var callCount = 0;
        void Migrate() => callCount++;

        // Act
        var act = () => DatabaseMigrationExtensions.ApplyWithRetry(
            Migrate, "TestService", Mock.Of<ILogger>(),
            maxRetries: 3, delay: TimeSpan.Zero);

        // Assert
        act.Should().NotThrow();
        callCount.Should().Be(1);
    }
}
