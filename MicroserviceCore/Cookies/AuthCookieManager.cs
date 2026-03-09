using Microsoft.AspNetCore.Http;

namespace MicroserviceCore.Cookies;

/// <summary>
/// Gerenciador centralizado de cookies de autenticação
/// Garante consistência nas configurações de cookies em toda a aplicação
/// </summary>
public static class AuthCookieManager
{
    /// <summary>
    /// Nome do cookie que armazena o Access Token
    /// </summary>
    public const string AccessTokenCookieName = "accessToken";
    
    /// <summary>
    /// Nome do cookie que armazena o Refresh Token
    /// </summary>
    public const string RefreshTokenCookieName = "refreshToken";

    /// <summary>
    /// Define os cookies de autenticação na resposta HTTP
    /// </summary>
    /// <param name="response">HttpResponse onde os cookies serão definidos</param>
    /// <param name="accessToken">Token de acesso JWT</param>
    /// <param name="refreshToken">Token de refresh</param>
    /// <param name="accessTokenExpirationSeconds">Segundos de expiração do cookie de access token</param>
    /// <param name="refreshTokenExpirationSeconds">Segundos de expiração do cookie de refresh token</param>
    public static void SetTokenCookies(
        HttpResponse response, 
        string accessToken, 
        string refreshToken,
        int accessTokenExpirationSeconds = 900, // 15 minutos
        int refreshTokenExpirationSeconds = 3600) // 1 hora
    {
        var accessTokenOptions = CreateCookieOptions(accessTokenExpirationSeconds);
        var refreshTokenOptions = CreateCookieOptions(refreshTokenExpirationSeconds);

        response.Cookies.Append(AccessTokenCookieName, accessToken, accessTokenOptions);
        response.Cookies.Append(RefreshTokenCookieName, refreshToken, refreshTokenOptions);
    }

    /// <summary>
    /// Limpa os cookies de autenticação (logout)
    /// </summary>
    /// <param name="response">HttpResponse onde os cookies serão removidos</param>
    public static void ClearTokenCookies(HttpResponse response)
    {
        var expiredOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        response.Cookies.Append(AccessTokenCookieName, "", expiredOptions);
        response.Cookies.Append(RefreshTokenCookieName, "", expiredOptions);
    }

    /// <summary>
    /// Obtém o access token da requisição
    /// </summary>
    /// <param name="request">HttpRequest de onde obter o cookie</param>
    /// <returns>Access token ou null se não existir</returns>
    public static string? GetAccessToken(HttpRequest request)
    {
        return request.Cookies[AccessTokenCookieName];
    }

    /// <summary>
    /// Obtém o refresh token da requisição
    /// </summary>
    /// <param name="request">HttpRequest de onde obter o cookie</param>
    /// <returns>Refresh token ou null se não existir</returns>
    public static string? GetRefreshToken(HttpRequest request)
    {
        return request.Cookies[RefreshTokenCookieName];
    }

    /// <summary>
    /// Atualiza os cookies na requisição atual para que middlewares subsequentes usem os novos tokens
    /// </summary>
    /// <param name="request">HttpRequest onde atualizar os cookies</param>
    /// <param name="accessToken">Novo access token</param>
    /// <param name="refreshToken">Novo refresh token</param>
    public static void UpdateRequestCookies(HttpRequest request, string accessToken, string refreshToken)
    {
        // Reconstrói o header Cookie com os novos valores
        var cookies = request.Cookies
            .Where(c => c.Key != AccessTokenCookieName && c.Key != RefreshTokenCookieName)
            .Select(c => $"{c.Key}={c.Value}")
            .ToList();

        cookies.Add($"{AccessTokenCookieName}={accessToken}");
        cookies.Add($"{RefreshTokenCookieName}={refreshToken}");

        request.Headers.Cookie = string.Join("; ", cookies);
    }

    /// <summary>
    /// Cria opções padrão para cookies de autenticação
    /// </summary>
    /// <param name="expirationSeconds">Tempo de expiração em segundos</param>
    private static CookieOptions CreateCookieOptions(int expirationSeconds)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // Necessário para frontend separado (CORS)
            Expires = DateTime.UtcNow.AddSeconds(expirationSeconds)
        };
    }
}

