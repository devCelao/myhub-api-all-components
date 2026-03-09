namespace MessageBus.Messages.Authentication;

/// <summary>
/// Requisição para obter as chaves públicas JWKS
/// </summary>
public class GetJwksRequest : IntegrationEvent
{
    public GetJwksRequest() { }
}

