namespace MessageBus.Messages.Authentication;

/// <summary>
/// Requisição para validar se uma sessão (refresh token) ainda é válida
/// </summary>
public class ValidateSessionRequest : IntegrationEvent
{
    public string RefreshToken { get; set; } = string.Empty;

    public ValidateSessionRequest() { }

    public ValidateSessionRequest(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}

