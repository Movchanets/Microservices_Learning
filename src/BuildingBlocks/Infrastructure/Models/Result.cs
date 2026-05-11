namespace BuildingBlocks.Infrastructure.Models;

/// <summary>
/// Generic result type for command handlers. Avoids throwing exceptions for expected failures.
/// Rationale: Implements the Result pattern to handle expected domain failures gracefully
/// without the performance overhead of throwing exceptions.
/// </summary>
/// <typeparam name="T">The type of the successful result value.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// Gets the value of the result if successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message if the result is a failure.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the specific error code if the result is a failure.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(string error, string errorCode)
    {
        Error = error;
        ErrorCode = errorCode;
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result with the provided value.
    /// </summary>
    /// <param name="value">The successful output value.</param>
    /// <returns>A successful Result instance.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorCode">The specific error code. Defaults to "ERROR".</param>
    /// <returns>A failed Result instance.</returns>
    public static Result<T> Failure(string error, string errorCode = "ERROR") => new(error, errorCode);
}
