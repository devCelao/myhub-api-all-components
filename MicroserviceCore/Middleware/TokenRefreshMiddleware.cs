using MessageBus.Interfaces;
using MessageBus.Messages.Authentication;
using MicroserviceCore.Cookies;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace MicroserviceCore.Middleware;

/// <summary>
/// Middleware que intercepta requisições com token expirado e faz refresh automático via MessageBus
/// Também valida se a sessão (refresh token) ainda é válida
/// Deve ser registrado ANTES do UseAuthentication() no pipeline
/// </summary>
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IBusMessage busMessage)
    {
        var accessToken = AuthCookieManager.GetAccessToken(context.Request);
        var refreshToken = AuthCookieManager.GetRefreshToken(context.Request);

        // Se não tem refresh token, não há como renovar - segue normalmente
        // (a autenticação JWT vai falhar naturalmente se o accessToken também não existir ou for inválido)
        if (string.IsNullOrEmpty(refreshToken))
        {
            await _next(context);
            return;
        }

        // Verifica se precisa fazer refresh:
        // 1. Access token não existe (cookie expirou ou nunca foi setado)
        // 2. Access token existe mas o JWT interno está expirado
        var needsRefresh = string.IsNullOrEmpty(accessToken) || IsTokenExpired(accessToken);

        if (needsRefresh)
        {
            Console.WriteLine("🔄 [TokenRefreshMiddleware] Access token ausente ou expirado, tentando refresh...");
            Console.WriteLine($"   📋 Access token presente: {!string.IsNullOrEmpty(accessToken)}");
            Console.WriteLine($"   📋 Refresh token presente: {!string.IsNullOrEmpty(refreshToken)}");

            // Primeiro: Validar se a sessão ainda é válida (não foi revogada)
            var sessionValid = await ValidateSessionAsync(busMessage, refreshToken);
            
            if (!sessionValid)
            {
                Console.WriteLine("❌ [TokenRefreshMiddleware] Sessão inválida/revogada. Limpando cookies...");
                AuthCookieManager.ClearTokenCookies(context.Response);
                
                // Retorna 401 imediatamente
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("Session-Invalid", "true");
                await context.Response.WriteAsync("Sessão inválida ou revogada. Faça login novamente.");
                return;
            }

            // Extrai o issuer do token expirado (se disponível) ou usa o da requisição atual
            var issuer = ExtractIssuerFromToken(accessToken) ?? GetCurrentIssuer(context);
            Console.WriteLine($"   🏷️ Issuer para refresh: {issuer}");

            // Tenta renovar o token passando o issuer
            var refreshResult = await TryRefreshTokenAsync(busMessage, refreshToken, issuer);
            
            if (refreshResult.Success && refreshResult.AccessToken != null && refreshResult.RefreshToken != null)
            {
                Console.WriteLine("✅ [TokenRefreshMiddleware] Refresh bem-sucedido! Atualizando cookies...");
                
                // Define os novos cookies na resposta
                AuthCookieManager.SetTokenCookies(context.Response, refreshResult.AccessToken, refreshResult.RefreshToken);
                
                // IMPORTANTE: Atualiza o cookie na requisição atual para que a autenticação use o novo token
                AuthCookieManager.UpdateRequestCookies(context.Request, refreshResult.AccessToken, refreshResult.RefreshToken);
            }
            else
            {
                Console.WriteLine("❌ [TokenRefreshMiddleware] Refresh falhou. Usuário precisa fazer login novamente.");
                // Limpa os cookies inválidos
                AuthCookieManager.ClearTokenCookies(context.Response);
                
                // Retorna 401 imediatamente
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("Token-Refresh-Failed", "true");
                await context.Response.WriteAsync("Não foi possível renovar o token. Faça login novamente.");
                return;
            }
        }
        else
        {
            // Access token existe e não está expirado, mas vamos validar a sessão
            var sessionValid = await ValidateSessionAsync(busMessage, refreshToken);
            
            if (!sessionValid)
            {
                Console.WriteLine("❌ [TokenRefreshMiddleware] Sessão inválida/revogada. Limpando cookies...");
                AuthCookieManager.ClearTokenCookies(context.Response);
                
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("Session-Invalid", "true");
                await context.Response.WriteAsync("Sessão inválida ou revogada. Faça login novamente.");
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Valida se a sessão (refresh token) ainda é válida via MessageBus
    /// </summary>
    private static async Task<bool> ValidateSessionAsync(IBusMessage busMessage, string refreshToken)
    {
        try
        {
            Console.WriteLine("🔐 [TokenRefreshMiddleware] Validando sessão via MessageBus...");
            
            var request = new ValidateSessionRequest(refreshToken);
            var response = await busMessage.RequestAsync<ValidateSessionRequest, ValidateSessionResponse>(request);
            
            if (response.IsValid)
            {
                Console.WriteLine("✅ [TokenRefreshMiddleware] Sessão válida");
                return true;
            }
            
            Console.WriteLine($"❌ [TokenRefreshMiddleware] Sessão inválida: {response.Reason}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [TokenRefreshMiddleware] Erro ao validar sessão: {ex.Message}");
            // Em caso de erro de comunicação, permite continuar (fail-open)
            // Pode ser alterado para fail-close se preferir mais segurança
            return true;
        }
    }

    /// <summary>
    /// Tenta renovar o access token usando o refresh token via MessageBus
    /// </summary>
    private static async Task<RefreshResult> TryRefreshTokenAsync(IBusMessage busMessage, string refreshToken, string? issuer)
    {
        try
        {
            Console.WriteLine("🔄 [TokenRefreshMiddleware] Renovando token via MessageBus...");
            
            var request = new RefreshTokenRequest(refreshToken, issuer);
            var response = await busMessage.RequestAsync<RefreshTokenRequest, RefreshTokenResponse>(request);
            
            if (response.Success && response.AccessToken != null && response.RefreshToken != null)
            {
                Console.WriteLine("✅ [TokenRefreshMiddleware] Token renovado com sucesso");
                return new RefreshResult(true, response.AccessToken, response.RefreshToken);
            }
            
            Console.WriteLine("❌ [TokenRefreshMiddleware] Falha ao renovar token");
            return new RefreshResult(false, null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [TokenRefreshMiddleware] Erro ao renovar token: {ex.Message}");
            return new RefreshResult(false, null, null);
        }
    }

    /// <summary>
    /// Extrai o issuer de um token JWT (sem validar assinatura)
    /// </summary>
    private static string? ExtractIssuerFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(token))
                return null;

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Issuer;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtém o issuer baseado na requisição HTTP atual
    /// </summary>
    private static string GetCurrentIssuer(HttpContext context)
    {
        var request = context.Request;
        var scheme = request.Scheme;
        var host = request.Host.Value;
        return $"{scheme}://{host}";
    }

    /// <summary>
    /// Verifica se o token JWT está expirado (sem validar assinatura)
    /// </summary>
    private static bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(token))
                return true;

            var jwtToken = handler.ReadJwtToken(token);
            
            // Adiciona margem de 30 segundos para evitar race conditions
            var expirationTime = jwtToken.ValidTo.AddSeconds(-30);
            var isExpired = expirationTime < DateTime.UtcNow;
            
            if (isExpired)
            {
                Console.WriteLine($"   ⏰ Token expirou em: {jwtToken.ValidTo:yyyy-MM-dd HH:mm:ss} UTC");
            }
            
            return isExpired;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ Erro ao verificar expiração do token: {ex.Message}");
            return true; // Se não conseguir ler, considera expirado
        }
    }
}

// DTO interno
internal record RefreshResult(bool Success, string? AccessToken, string? RefreshToken);
