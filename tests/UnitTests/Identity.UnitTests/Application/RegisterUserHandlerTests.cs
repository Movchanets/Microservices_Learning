using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Identity.Application.Commands.Register;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Moq;

namespace Identity.UnitTests.Application;

/// <summary>
/// Unit tests for <see cref="RegisterUserHandler"/>.
/// </summary>
public sealed class RegisterUserHandlerTests
{
    /// <summary>
    /// Tests that the handler returns a failure result when the email is already registered.
    /// Rationale: Validates the domain rule that prevents duplicate email registration.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnDuplicateEmailFailure()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtGenerator = new Mock<IJwtTokenGenerator>();

        var command = new RegisterUserCommand("buyer@example.com", "P@ssw0rd!", "Jane", "Doe");
        userRepository.Setup(x => x.ExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterUserHandler(
            userRepository.Object,
            unitOfWork.Object,
            passwordHasher.Object,
            jwtGenerator.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_EMAIL");
        userRepository.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        passwordHasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        jwtGenerator.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    /// <summary>
    /// Tests the happy path where a valid command correctly registers the user, hashes the password, and issues tokens.
    /// Rationale: Ensures all dependent infrastructure services are coordinated correctly during successful registration.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistUserAndReturnAuthResponse()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtGenerator = new Mock<IJwtTokenGenerator>();

        var command = new RegisterUserCommand("buyer@example.com", "P@ssw0rd!", "Jane", "Doe");
        userRepository.Setup(x => x.ExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        passwordHasher.Setup(x => x.Hash(command.Password)).Returns("hashed-password");
        jwtGenerator.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtGenerator.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        User? persistedUser = null;
        userRepository.Setup(x => x.Add(It.IsAny<User>()))
            .Callback<User>(user => persistedUser = user);

        var handler = new RegisterUserHandler(
            userRepository.Object,
            unitOfWork.Object,
            passwordHasher.Object,
            jwtGenerator.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.Email.Should().Be("buyer@example.com");
        result.Value.Role.Should().Be("Buyer");

        persistedUser.Should().NotBeNull();
        persistedUser!.CurrentRefreshToken.Should().NotBeNull();
        persistedUser.CurrentRefreshToken!.Token.Should().Be("refresh-token");

        userRepository.Verify(x => x.Add(It.IsAny<User>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
