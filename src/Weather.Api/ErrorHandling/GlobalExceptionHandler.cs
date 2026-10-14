using FluentValidation;

using global::Weather.Modules.Favorites.Application.Abstractions;
using global::Weather.Modules.Geography.Application.Abstractions;
using global::Weather.Modules.Weather.Application.Abstractions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Weather.Api.ErrorHandling;

/// <summary>Translates exceptions into RFC 9457 Problem Details with a stable code.</summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        Problem problem = Map(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Handled {Code} ({Status}) for {Path}",
                problem.Code,
                problem.Status,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status;

        ProblemDetails problemDetails = new()
        {
            Type = problem.Type,
            Title = problem.Title,
            Status = problem.Status,
            Detail = problem.Detail,
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["code"] = problem.Code;

        if (problem.Errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = problem.Errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }

    private static Problem Map(Exception exception) => exception switch
    {
        ValidationException validation => new Problem(
            StatusCodes.Status422UnprocessableEntity,
            ApiErrorCodes.ValidationFailed,
            ApiProblemTypes.ValidationError,
            "Request validation failed",
            "One or more fields are invalid.",
            validation.Errors
                .Select(failure => new FieldError(failure.PropertyName, failure.ErrorCode, failure.ErrorMessage))
                .ToArray()),

        CountryNotFoundException or WeatherNotFoundException or CityNotFoundException => new Problem(
            StatusCodes.Status404NotFound,
            ApiErrorCodes.ResourceNotFound,
            ApiProblemTypes.ResourceNotFound,
            "Resource not found",
            exception.Message),

        FavoriteAlreadyExistsException => new Problem(
            StatusCodes.Status409Conflict,
            ApiErrorCodes.DuplicateResource,
            ApiProblemTypes.DuplicateResource,
            "Duplicate resource",
            exception.Message),

        WeatherProviderException => new Problem(
            StatusCodes.Status502BadGateway,
            ApiErrorCodes.DependencyUnavailable,
            ApiProblemTypes.DependencyUnavailable,
            "Dependency unavailable",
            "The weather provider could not be reached."),

        _ => new Problem(
            StatusCodes.Status500InternalServerError,
            ApiErrorCodes.InternalError,
            ApiProblemTypes.InternalError,
            "Internal server error",
            "The request could not be completed."),
    };

    private sealed record FieldError(string Field, string Code, string Message);

    private sealed record Problem(
        int Status,
        string Code,
        string Type,
        string Title,
        string Detail,
        FieldError[]? FieldErrors = null)
    {
        public IReadOnlyList<FieldError> Errors { get; } = FieldErrors ?? [];
    }
}
