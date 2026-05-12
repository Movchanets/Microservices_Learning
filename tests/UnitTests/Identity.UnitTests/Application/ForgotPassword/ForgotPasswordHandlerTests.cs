using FluentAssertions;
using Identity.Application.Commands.ForgotPassword;
using Identity.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using Moq;

namespace Identity.UnitTests.Application.ForgotPassword;

public sealed class ForgotPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<ForgotPasswordHandler>> _loggerMock;
    private readonly ForgotPasswordHandler _handler;

    public ForgotPasswordHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<ForgotPasswordHandler>>();
        _handler = new ForgotPasswordHandler(_userRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var user = User.Create(email, "hash", "First", "Last");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ForgotPasswordCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnSuccessForSecurity()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ForgotPasswordCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
