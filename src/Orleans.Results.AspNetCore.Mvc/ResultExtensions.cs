using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;

namespace Orleans.Results.AspNetCore.Mvc;

public static class ResultExtensions
{
    public static ActionResult OkOrBadRequest<TErrorCode>(this Result<TErrorCode> result) where TErrorCode : Enum
     => result.IsSuccess ? result.Ok() : result.BadRequest();

    public static ActionResult<T> OkOrBadRequest<TErrorCode, T>(this Result<TErrorCode, T> result) where TErrorCode : Enum
     => result.IsSuccess ? result.Ok() : result.BadRequest();

    public static ActionResult Ok<TErrorCode>(this Result<TErrorCode> _) where TErrorCode : Enum
     => new OkResult();

    public static ActionResult<T> Ok<TErrorCode, T>(this Result<TErrorCode, T> result) where TErrorCode : Enum
     => new OkObjectResult(result.Value);

    public static ActionResult<T> CreatedAtAction<TErrorCode, T>(this Result<TErrorCode, T> result, [CallerMemberName] string? actionName = null, string? controllerName = null, object? routeValues = null) where TErrorCode : Enum
     => new CreatedAtActionResult(actionName, controllerName, routeValues, result.Value);

    public static ActionResult NotFound<TErrorCode>(this Result<TErrorCode> result) where TErrorCode : Enum
     => new NotFoundObjectResult(result.ErrorsText);

    public static ActionResult BadRequest<TErrorCode>(this Result<TErrorCode> result) where TErrorCode : Enum
     => new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]>(result.Errors
            .GroupBy(error => error.Code, error => error.Message)
            .Select(group => new KeyValuePair<string, string[]>($"Error {group.Key}", group.ToArray()))
        )));

    public static NotImplementedException UnhandledErrorException<TErrorCode>(this Result<TErrorCode> result) where TErrorCode : Enum
        => new("Unhandled error(s): " + result.ErrorsText);
}
