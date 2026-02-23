namespace AiForAi.Api.Models.Responses;

public sealed class ApiError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public IResult ToIResult() => Results.Json(
        new { error_code = ErrorCode, message = Message },
        statusCode: StatusCode);
}

public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public ApiError? Error { get; init; }

    public static ServiceResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static ServiceResult<T> Failure(string errorCode, string message, int statusCode) =>
        new()
        {
            IsSuccess = false,
            Error = new ApiError
            {
                ErrorCode = errorCode,
                Message = message,
                StatusCode = statusCode
            }
        };
}

public sealed class ServiceResult
{
    public bool IsSuccess { get; init; }
    public ApiError? Error { get; init; }

    public static ServiceResult Success() => new() { IsSuccess = true };

    public static ServiceResult Failure(string errorCode, string message, int statusCode) =>
        new()
        {
            IsSuccess = false,
            Error = new ApiError
            {
                ErrorCode = errorCode,
                Message = message,
                StatusCode = statusCode
            }
        };
}
