using System.ComponentModel.DataAnnotations;

namespace DomainObjects.Helpers;

public static class ValidationHelpers
{
    public static bool TryValidate<T>(this T obj, out List<ValidationError> errors, IServiceProvider? services = null)
    {
        var context = new ValidationContext(obj!, services, null);
        var results = new List<ValidationResult>();

        var ok = Validator.TryValidateObject(
            obj!, context, results,
            validateAllProperties: true // inclui [Required], [EmailAddress] etc.
        );

        errors = results
            .Select(r => new ValidationError
            {
                Field = r.MemberNames?.FirstOrDefault() ?? string.Empty,
                Message = r.ErrorMessage ?? "Inválido."
            })
            .ToList();

        return ok;
    }
    public static IReadOnlyList<ValidationError> GetErrors<T>(this T obj, IServiceProvider? services = null)
        => obj.TryValidate(out var errors, services) ? Array.Empty<ValidationError>() : errors;
}
public sealed class ValidationError
{
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";
}
