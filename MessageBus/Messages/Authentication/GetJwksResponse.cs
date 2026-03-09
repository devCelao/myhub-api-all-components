namespace MessageBus.Messages.Authentication;

/// <summary>
/// Resposta com as chaves públicas JWKS em formato JSON
/// </summary>
public class GetJwksResponse : ResponseMessage
{
    /// <summary>
    /// JSON string contendo o JWKS (JSON Web Key Set)
    /// </summary>
    public string JwksJson { get; set; } = string.Empty;

    public GetJwksResponse() { }

    public GetJwksResponse(string jwksJson)
    {
        JwksJson = jwksJson;
    }
}

