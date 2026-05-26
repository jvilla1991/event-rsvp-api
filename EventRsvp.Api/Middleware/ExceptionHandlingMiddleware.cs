using System.Net;
using System.Text.Json;
using EventRsvp.Domain.Exceptions;
using FluentValidation;

namespace EventRsvp.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Error = "An error occurred while processing your request.",
            StatusCode = (int)HttpStatusCode.InternalServerError
        };

        switch (exception)
        {
            case ValidationException validationEx:
                var validationErrors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                
                errorResponse.Error = "Validation failed.";
                errorResponse.Details = JsonSerializer.Serialize(validationErrors);
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidEventException invalidEventEx:
                errorResponse.Error = invalidEventEx.Message;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidRsvpException invalidRsvpEx:
                errorResponse.Error = invalidRsvpEx.Message;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                errorResponse.Error = unauthorizedEx.Message;
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ArgumentException argEx:
                errorResponse.Error = argEx.Message;
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                errorResponse.Error = "An unexpected error occurred.";
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(json);
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int StatusCode { get; set; }
    }
}
