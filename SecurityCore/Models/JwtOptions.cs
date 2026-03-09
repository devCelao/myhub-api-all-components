namespace SecurityCore.Models;

/// <summary>
/// Configurações básicas para geração de JWT
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Para quem o token é destinado (aud claim)
    /// Exemplo: facihub-api, facihub-dashboard
    /// </summary>
    public string Audience { get; set; } = "";
}

