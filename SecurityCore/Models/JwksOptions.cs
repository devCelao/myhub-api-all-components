namespace SecurityCore.Models;

/// <summary>
/// Configurações para gerenciamento de chaves JWKS
/// </summary>
public class JwksOptions
{
    /// <summary>
    /// Algoritmo de assinatura (sempre ES256 na versão simplificada)
    /// </summary>
    public string Algorithm { get; set; } = "ES256";
    
    /// <summary>
    /// Dias até a chave expirar e ser rotacionada
    /// </summary>
    public int DaysUntilExpire { get; set; } = 90;
    
    /// <summary>
    /// Quantidade de chaves ativas a manter (para rotação suave)
    /// </summary>
    public int KeysToKeep { get; set; } = 2;
    
    /// <summary>
    /// Prefixo para identificar as chaves no KeyId
    /// </summary>
    public string KeyPrefix { get; set; } = "FaciHub_";
}

