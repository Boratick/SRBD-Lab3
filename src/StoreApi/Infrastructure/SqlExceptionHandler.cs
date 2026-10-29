using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace StoreApi.Infrastructure;

/// <summary>
/// Перетворює виключення, згенеровані на боці SQL Server, у відповідь HTTP (RFC 7807 Problem Details).
/// Користувацькі помилки (THROW з номером >= 50000) повертаються як 400 з текстом із бази даних.
/// </summary>
public class SqlExceptionHandler(IProblemDetailsService problemDetails, ILogger<SqlExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not SqlException sql) return false;

        var isUserError = sql.Number >= 50000;
        if (!isUserError) logger.LogError(sql, "SQL Server error {Number}", sql.Number);

        context.Response.StatusCode = isUserError ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = isUserError ? "Помилка бізнес-логіки бази даних" : "Помилка сервера бази даних",
            Detail = sql.Message,
        };
        problem.Extensions["sqlErrorNumber"] = sql.Number;
        problem.Extensions["procedure"] = sql.Procedure;

        return await problemDetails.TryWriteAsync(new() { HttpContext = context, ProblemDetails = problem, Exception = sql });
    }
}
