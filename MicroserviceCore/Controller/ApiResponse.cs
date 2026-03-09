namespace MicroserviceCore.Controller;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public List<ApiError> Errors { get; init; } = [];
    public object? Meta { get; init; }

    public static ApiResponse<T> FromData(T data, object? meta) =>
        new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse<T> FromErros(IEnumerable<ApiError> errors) =>
        new() { Success = false, Errors = [.. errors] };
}

public sealed record ApiError(string Code, string Message, string? Field = null);