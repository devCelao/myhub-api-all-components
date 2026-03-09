namespace MessageBus.Messages.Authentication;

/// <summary>
/// Resposta do refresh token com novos tokens
/// </summary>
public class RefreshTokenResponse : ResponseMessage
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public int RefreshExpiresIn { get; set; }

    public RefreshTokenResponse() { }

    public RefreshTokenResponse(bool success, string? accessToken = null, string? refreshToken = null, int expiresIn = 900, int refreshExpiresIn = 2592000)
    {
        Success = success;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresIn = expiresIn;
        RefreshExpiresIn = refreshExpiresIn;
    }

    public static RefreshTokenResponse Failed() => new(false);
    
    public static RefreshTokenResponse Succeeded(string accessToken, string refreshToken, int expiresIn = 900, int refreshExpiresIn = 2592000) 
        => new(true, accessToken, refreshToken, expiresIn, refreshExpiresIn);
}

