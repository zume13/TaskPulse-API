using Microsoft.AspNetCore.Mvc;
using Shared.ResultPattern;

namespace Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToProblemResult(this Result result)
    {
        var error = result.Error;

        var statusCode = error.errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.code,
            Detail = error.message
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }
}