using MicroserviceCore.Controller;

namespace MicroserviceCore.Respostas;

public class ServiceResult<T>
{
    public T? Value { get; private init; }
    public List<ApiError> Errors { get; private init; } = [];
    public bool HasErros => Errors.Count > 0;

    public static ServiceResult<T> Ok(T value) => 
        new() { Value = value };

    public static ServiceResult<T> Fail(params ApiError[] errors) =>
        new() { Errors = [.. errors] };
}
