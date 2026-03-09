using Microsoft.AspNetCore.Http;

namespace SecurityCore.Services;

/// <summary>
/// Interface para obtenção do Issuer dinâmico baseado no contexto HTTP
/// </summary>
public interface IIssuerService
{
    /// <summary>
    /// Obtém o Issuer baseado na URL da requisição atual
    /// Exemplo: https://auth.facimed.com → retorna "https://auth.facimed.com"
    /// </summary>
    string GetCurrentIssuer();
}

/// <summary>
/// Serviço para geração de Issuer dinâmico baseado no subdomínio da API
/// </summary>
public class IssuerService(IHttpContextAccessor httpContextAccessor) : IIssuerService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// Obtém o Issuer baseado na URL da requisição HTTP atual
    /// Formato: {scheme}://{host}
    /// Exemplo: https://auth.facimed.com
    /// </summary>
    public string GetCurrentIssuer()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext não disponível");

        var request = httpContext.Request;
        var scheme = request.Scheme; // http ou https
        var host = request.Host.Value; // auth.facimed.com:5001 ou auth.facimed.com

        return $"{scheme}://{host}";
    }
}

