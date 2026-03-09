using Microsoft.AspNetCore.Builder;

namespace MicroserviceCore.Middleware;

public static class TokenRefreshMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o middleware de refresh automático de token
    /// IMPORTANTE: Deve ser chamado ANTES de UseAuthentication()
    /// </summary>
    /// <example>
    /// app.UseTokenRefresh();      // Primeiro
    /// app.UseAuthentication();    // Depois
    /// app.UseAuthorization();
    /// </example>
    public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenRefreshMiddleware>();
    }

    /// <summary>
    /// Configura autenticação com refresh automático de token via cookie
    /// Substitui as chamadas separadas de UseTokenRefresh(), UseAuthentication() e UseAuthorization()
    /// </summary>
    /// <example>
    /// // Ao invés de:
    /// // app.UseTokenRefresh();
    /// // app.UseAuthentication();
    /// // app.UseAuthorization();
    /// 
    /// // Use apenas:
    /// app.UseAuthenticationWithTokenRefresh();
    /// </example>
    public static IApplicationBuilder UseAuthenticationWithTokenRefresh(this IApplicationBuilder app)
    {
        app.UseMiddleware<TokenRefreshMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

