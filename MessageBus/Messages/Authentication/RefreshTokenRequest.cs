namespace MessageBus.Messages.Authentication;

/// <summary>
/// Requisição para renovar access token usando refresh token
/// </summary>
public class RefreshTokenRequest : IntegrationEvent
{
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Issuer original do token (para manter consistência na geração do novo token)
    /// Exemplo: https://localhost:5010 ou https://auth.facimed.com
    /// </summary>
    public string? Issuer { get; set; }

    public RefreshTokenRequest() { }

    public RefreshTokenRequest(string refreshToken, string? issuer = null)
    {
        RefreshToken = refreshToken;
        Issuer = issuer;
    }
}
