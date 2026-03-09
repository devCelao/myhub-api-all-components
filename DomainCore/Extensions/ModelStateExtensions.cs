using DomainObjects.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DomainObjects.Extensions;

public static class ModelStateExtensions
{
    public static List<ValidationError> ToErrorList(this ModelStateDictionary modelState)
    {
        return [.. modelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .SelectMany(kvp => kvp.Value!.Errors.Select(err =>
                new ValidationError
                {
                    Field= NormalizeKey(kvp.Key),
                    Message= string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Valor inválido." : err.ErrorMessage
                }))];
    }
    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return key;
        var dot = key.LastIndexOf('.');
        return dot >= 0 ? key[(dot + 1)..] : key;
    }
}

public static class ModelStateGroupedExtensions
{
    public static Dictionary<string, string[]> ToGroupedErrors(this ModelStateDictionary modelState)
    {
        return modelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors
                           .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                           .Distinct()
                           .ToArray()
            );
    }
}