using System.Net;
using System.Text.Json;
using Bakery.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed: {Path}", context.Request.Path);
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            BusinessConflictException => HttpStatusCode.Conflict,
            ValidationException or DomainValidationException or ArgumentException => HttpStatusCode.BadRequest,
            DbUpdateConcurrencyException or DbUpdateException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };
        var details = new ProblemDetails { Status = (int)status, Title = status.ToString(), Instance = context.Request.Path };
        if (environment.IsDevelopment() || status != HttpStatusCode.InternalServerError) details.Detail = exception.Message;
        if (exception is ValidationException validation && validation.Errors.Count > 0) details.Extensions["errors"] = validation.Errors;
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(details));
    }
}
