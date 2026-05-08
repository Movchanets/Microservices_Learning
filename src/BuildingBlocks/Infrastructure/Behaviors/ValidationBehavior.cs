using FluentValidation;
using MediatR;

namespace BuildingBlocks.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation before the handler.
/// Throws ValidationException if any rules fail.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
	IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : notnull
{
	/// <summary>
	/// Intercepts the MediatR request to perform validation before the handler logic executes.
	/// Rationale: Adheres to the Fail-Fast principle. By executing all registered FluentValidation rules
	/// in the pipeline, we guarantee that handlers only receive valid data, avoiding
	/// redundant validation checks within the core business logic.
	/// </summary>
	/// <param name="request">The incoming request to validate.</param>
	/// <param name="next">The delegate to call the next behavior or the handler itself.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The response from the next behavior or handler.</returns>
	/// <exception cref="ValidationException">Thrown if any validation failures occur.</exception>
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		if (!validators.Any())
			return await next();

		var context = new ValidationContext<TRequest>(request);

		var failures = (await Task.WhenAll(
				validators.Select(v => v.ValidateAsync(context, cancellationToken))))
			.SelectMany(r => r.Errors)
			.Where(f => f is not null)
			.ToList();

		if (failures.Count != 0)
			throw new ValidationException(failures);

		return await next();
	}
}
