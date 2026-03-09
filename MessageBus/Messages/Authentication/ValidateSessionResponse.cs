namespace MessageBus.Messages.Authentication;

/// <summary>
/// Resposta da validação de sessão
/// </summary>
public class ValidateSessionResponse : ResponseMessage
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }

    public ValidateSessionResponse() { }

    public ValidateSessionResponse(bool isValid, string? reason = null)
    {
        IsValid = isValid;
        Reason = reason;
    }

    public static ValidateSessionResponse Valid() => new(true);
    
    public static ValidateSessionResponse Invalid(string reason) => new(false, reason);
}

