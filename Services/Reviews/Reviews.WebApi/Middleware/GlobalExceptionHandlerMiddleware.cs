using System.Net;
using System.Text.Json;
using Bakery.Reviews.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Bakery.Reviews.WebApi.Middleware;

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Запит завершився помилкою: {Path}", context.Request.Path);

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var status = MapStatusCode(exception);

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = MapTitle(exception, status),
            Type = $"https://httpstatuses.com/{(int)status}",
            Instance = context.Request.Path,
        };

        if (environment.IsDevelopment() || status != HttpStatusCode.InternalServerError)
        {
            problem.Detail = exception.Message;
        }

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        }

        if (exception is MongoWriteException writeException && writeException.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            problem.Extensions["duplicateKey"] = writeException.WriteError.Message;
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }

    private static HttpStatusCode MapStatusCode(Exception exception) => exception switch
    {
        NotFoundException => HttpStatusCode.NotFound,
        ConflictException => HttpStatusCode.Conflict,
        ValidationException or FormatException => HttpStatusCode.BadRequest,
        DomainException => HttpStatusCode.BadRequest,
        MongoConnectionException or MongoExecutionTimeoutException => HttpStatusCode.ServiceUnavailable,
        TimeoutException => HttpStatusCode.ServiceUnavailable,
        MongoWriteException write when write.WriteError?.Category == ServerErrorCategory.DuplicateKey => HttpStatusCode.Conflict,
        MongoBulkWriteException bulk when bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey) => HttpStatusCode.Conflict,
        MongoCommandException => HttpStatusCode.UnprocessableEntity,
        _ => HttpStatusCode.InternalServerError,
    };

    private static string MapTitle(Exception exception, HttpStatusCode status) => exception switch
    {
        MongoConnectionException or MongoExecutionTimeoutException or TimeoutException => "MongoDB недоступна",
        MongoCommandException => "Документ не відповідає схемі колекції",
        MongoWriteException or MongoBulkWriteException => "Порушення унікальності в MongoDB",
        ValidationException => "Невалідні дані запиту",
        ConflictException => "Конфлікт стану ресурсу",
        NotFoundException => "Ресурс не знайдено",
        _ => status.ToString(),
    };
}
