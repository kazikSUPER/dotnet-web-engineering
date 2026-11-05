using System.Net;
using System.Text.Json;
using Bakery.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FvValidationException = FluentValidation.ValidationException;

namespace Bakery.API.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            FvValidationException or ValidationException or DomainValidationException or ArgumentException => HttpStatusCode.BadRequest,
            DbUpdateConcurrencyException or DbUpdateException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };

        var isValidation = status == HttpStatusCode.BadRequest;
        ProblemDetails details = isValidation
            ? new ValidationProblemDetails(CollectErrors(exception))
            : new ProblemDetails();
        details.Status = (int)status;
        details.Type = isValidation ? "https://tools.ietf.org/html/rfc9110#section-15.5.1" : "about:blank";
        details.Title = isValidation ? "One or more validation errors occurred." : status.ToString();
        details.Instance = context.Request.Path;
        if (environment.IsDevelopment() || status != HttpStatusCode.InternalServerError) details.Detail = exception.Message;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(details, details.GetType(), JsonOptions));
    }

    private static IDictionary<string, string[]> CollectErrors(Exception exception) => exception switch
    {
        FvValidationException validation => validation.Errors
            .GroupBy(x => x.PropertyName, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Select(failure => failure.ErrorMessage).ToArray(), StringComparer.Ordinal),
        ValidationException domain => domain.Errors,
        _ => new Dictionary<string, string[]> { [""] = [exception.Message] }
    };
}
