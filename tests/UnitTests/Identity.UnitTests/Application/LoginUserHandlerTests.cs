using BuildingBlocks.SharedContracts.Abstractions;
using FluentAssertions;
using Identity.Application.Commands.Login;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Moq;

namespace Identity.UnitTests.Application;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnInvalidCredentialsFailure()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtGenerator = new Mock<IJwtTokenGenerator>();

        userRepository.Setup(x => x.GetByEmailAsync("buyer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new LoginUserHandler(
            userRepository.Object,
            unitOfWork.Object,
            passwordHasher.Object,
            jwtGenerator.Object);

        var result = await handler.Handle(
            new LoginUserCommand("buyer@example.com", "P@ssw0rd!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalid_ShouldReturnInvalidCredentialsFailure()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtGenerator = new Mock<IJwtTokenGenerator>();

        var user = User.Create("buyer@example.com", "stored-hash", "Jane", "Doe");
        userRepository.Setup(x => x.GetByEmailAsync("buyer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(x => x.Verify("wrong-password", user.PasswordHash.Hash))
            .Returns(false);

        var handler = new LoginUserHandler(
            userRepository.Object,
            unitOfWork.Object,
            passwordHasher.Object,
            jwtGenerator.Object);

        var result = await handler.Handle(
            new LoginUserCommand("buyer@example.com", "wrong-password"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnAuthResponseAndPersistRefreshToken()
    {
        var userRepository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtGenerator = new Mock<IJwtTokenGenerator>();

        var user = User.Create("buyer@example.com", "stored-hash", "Jane", "Doe");
        userRepository.Setup(x => x.GetByEmailAsync("buyer@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(x => x.Verify("P@ssw0rd!", user.PasswordHash.Hash))
            .Returns(true);
        jwtGenerator.Setup(x => x.GenerateAccessToken(user)).Returns("access-token");
        jwtGenerator.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new LoginUserHandler(
            userRepository.Object,
            unitOfWork.Object,
            passwordHasher.Object,
            jwtGenerator.Object);

        var result = await handler.Handle(
            new LoginUserCommand("buyer@example.com", "P@ssw0rd!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        user.CurrentRefreshToken.Should().NotBeNull();
        user.CurrentRefreshToken!.Token.Should().Be("refresh-token");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
