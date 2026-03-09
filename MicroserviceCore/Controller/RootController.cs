using FluentValidation.Results;
using MicroserviceCore.Respostas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MicroserviceCore.Controller;
[ApiController]
public class RootController : ControllerBase
{
    protected IActionResult OkResponse<T>(T data, object? meta = null)
    {
        var payload = ApiResponse<T>.FromData(data, meta);

        return Ok(payload);
    }

    protected IActionResult ErrorResponse(IEnumerable<ApiError> errors,
                                          int statusCode = StatusCodes.Status400BadRequest)
    {
        var payload = ApiResponse<object?>.FromErros(errors);
        return StatusCode(statusCode, payload);
    }

    protected IActionResult FromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(kvp => kvp.Value?.Errors?.Any() == true)
            .SelectMany(kvp =>
                kvp.Value!.Errors.Select(e =>
                    new ApiError("validation_error", e.ErrorMessage, kvp.Key)));

        return ErrorResponse(errors, StatusCodes.Status400BadRequest);
    }

    protected IActionResult FromValidationResult(ValidationResult validation)
    {
        var errors = validation.Errors.Select(e =>
            new ApiError("validation_error", e.ErrorMessage, e.PropertyName));
        return ErrorResponse(errors);
    }

    protected IActionResult CustomResponde(RespostaProcessamento? result)
    {
        if (result is null)
            return ErrorResponse([new("process_error", "Tente novamente mais tarde!")], 503);

        if (result.PossuiErros)
            return ErrorResponse(result.Errors.Select(err => new ApiError("domain_error", err)), 400);

        return OkResponse(result.ResultObject);
    }

    protected IActionResult ProblemFromException(Exception ex, int statusCode = StatusCodes.Status500InternalServerError)
    {
        var pd = new ProblemDetails
        {
            Title = "An error occurred while processing your request.",
            Detail = ex.Message,
            Status = statusCode
        };

        return StatusCode(statusCode, pd);
    }
}
