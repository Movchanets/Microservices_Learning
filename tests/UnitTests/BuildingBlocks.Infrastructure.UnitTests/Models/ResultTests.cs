using BuildingBlocks.Infrastructure.Models;

namespace BuildingBlocks.Infrastructure.UnitTests.Models;

public class ResultTests
{
    [Fact]
    public void Success_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var value = "Test Value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_WithErrorCode_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var error = "Test Error";
        var errorCode = "TEST_CODE";

        // Act
        var result = Result<string>.Failure(error, errorCode);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Failure_WithoutErrorCode_ShouldSetDefaultErrorCode()
    {
        // Arrange
        var error = "Test Error";

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal("ERROR", result.ErrorCode);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Success_WithIntValue_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_WithIntValue_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var error = "Int Error";

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal("ERROR", result.ErrorCode);
        Assert.Equal(0, result.Value);
    }
}
