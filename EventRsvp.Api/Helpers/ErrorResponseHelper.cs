using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventRsvp.Api.Helpers;

public static class ErrorResponseHelper
{
    public static BadRequestObjectResult ValidationErrorResponse(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            error = "Validation failed.",
            details = errors,
            statusCode = 400
        });
    }

    public static BadRequestObjectResult BadRequestResponse(string message)
    {
        return new BadRequestObjectResult(new
        {
            error = message,
            statusCode = 400
        });
    }

    public static NotFoundObjectResult NotFoundResponse(string message)
    {
        return new NotFoundObjectResult(new
        {
            error = message,
            statusCode = 404
        });
    }

    public static UnauthorizedObjectResult UnauthorizedResponse(string message)
    {
        return new UnauthorizedObjectResult(new
        {
            error = message,
            statusCode = 401
        });
    }
}
