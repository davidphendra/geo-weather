using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace Weather.Modules.Notes.Application.Configuration;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] applicableValidators = validators.ToArray();

        if (applicableValidators.Length > 0)
        {
            ValidationContext<TRequest> context = new(request);

            ValidationResult[] results = await Task.WhenAll(
                applicableValidators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            ValidationFailure[] failures = results.SelectMany(result => result.Errors).ToArray();

            if (failures.Length > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next(cancellationToken);
    }
}
