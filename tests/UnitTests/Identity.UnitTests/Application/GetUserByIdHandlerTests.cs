using FluentAssertions;
using Identity.Application.Queries;
using Identity.Domain.Aggregates;
using Moq;

namespace Identity.UnitTests.Application;

/// <summary>
/// Unit tests for <see cref="GetUserByIdHandler"/>.
/// </summary>
public sealed class GetUserByIdHandlerTests
{
    /// <summary>
    /// Tests that the handler returns null when the user does not exist.
    /// Rationale: Validates correct missing resource handling.
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var userRepository = new Mock<IUserRepository>();
        var userId = Guid.NewGuid();
        userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new GetUserByIdHandler(userRepository.Object);

        var result = await handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the handler returns a correctly mapped DTO when the user exists.
    /// Rationale: Validates that domain aggregate fields are successfully mapped to the application query result.
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnMappedDto()
    {
        var userRepository = new Mock<IUserRepository>();
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");
        userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetUserByIdHandler(userRepository.Object);

        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("buyer@example.com");
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.Role.Should().Be("Buyer");
        result.IsActive.Should().BeTrue();
    }
}
