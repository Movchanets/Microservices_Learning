using FluentAssertions;
using Identity.Application.Commands.Register;

namespace Identity.UnitTests.Application;

public sealed class RegisterUserValidatorTests
{
    [Fact]
    public void Validate_WhenPasswordIsWeak_ShouldFailValidation()
    {
        var validator = new RegisterUserValidator();
        var command = new RegisterUserCommand(
            "buyer@example.com",
            "weakpass",
            "Jane",
            "Doe");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(x => x.PropertyName).Should().Contain("Password");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPassValidation()
    {
        var validator = new RegisterUserValidator();
        var command = new RegisterUserCommand(
            "buyer@example.com",
            "P@ssw0rd!",
            "Jane",
            "Doe");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
