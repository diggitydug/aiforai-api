namespace AiForAi.Api.Models.Responses;

public static class ErrorResults
{
    public static IResult BadRequest(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status400BadRequest);

    public static IResult Unauthorized(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status401Unauthorized);

    public static IResult Forbidden(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status403Forbidden);

    public static IResult NotFound(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status404NotFound);

    public static IResult Conflict(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status409Conflict);

    public static IResult TooManyRequests(string errorCode, string message) =>
        Results.Json(new { error_code = errorCode, message }, statusCode: StatusCodes.Status429TooManyRequests);
}
