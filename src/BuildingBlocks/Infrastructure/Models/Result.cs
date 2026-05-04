namespace BuildingBlocks.Infrastructure.Models;

/// <summary>
/// Generic result type for command handlers. Avoids throwing exceptions for expected failures.
/// </summary>
public sealed class Result<T>
{
	public T? Value { get; }
	public string? Error { get; }
	public string? ErrorCode { get; }
	public bool IsSuccess { get; }

	private Result(T value) { Value = value; IsSuccess = true; }
	private Result(string error, string errorCode) { Error = error; ErrorCode = errorCode; IsSuccess = false; }

	public static Result<T> Success(T value) => new(value);
	public static Result<T> Failure(string error, string errorCode = "ERROR") => new(error, errorCode);
}
