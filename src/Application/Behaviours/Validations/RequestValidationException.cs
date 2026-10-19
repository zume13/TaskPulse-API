using Domain.Shared.ResultPattern;
using FluentValidation.Results;


namespace Application.Behaviours.Validations;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IEnumerable<ValidationFailure> failures)
    {
        Errors = failures
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToList();
    }

    public IReadOnlyList<Error> Errors { get; }
}