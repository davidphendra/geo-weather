using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Weather.BuildingBlocks.Application.Configuration;

/// <summary>Runs every registered FluentValidation validator for a request before its handler.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] applicable = [.. validators];

        if (applicable.Length == 0)
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] results = await Task.WhenAll(
            applicable.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        ValidationFailure[] failures = [.. results.SelectMany(result => result.Errors)];

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
