using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace SecurityCore.Models;

/// <summary>
/// Representa uma chave de segurança JWKS armazenada no banco de dados
/// </summary>
public class SecurityKeyWithPrivate
{
    /// <summary>
    /// Identificador único da chave no banco
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID da chave (kid - Key ID) usado no JWT
    /// </summary>
    public string KeyId { get; set; } = default!;
    
    /// <summary>
    /// Tipo da chave (kty): EC, RSA, oct
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// Algoritmo de assinatura: ES256, RS256, HS256, etc
    /// </summary>
    public string Algorithm { get; set; } = default!;
    
    /// <summary>
    /// Parâmetros da chave em formato JSON (inclui chave privada inicialmente)
    /// </summary>
    public string? ParametersJson { get; set; }
    
    /// <summary>
    /// Data de criação da chave
    /// </summary>
    public DateTime CreationDate { get; set; }
    
    /// <summary>
    /// Data de expiração da chave (após essa data, não deve mais assinar novos tokens)
    /// </summary>
    public DateTime ExpirationDate { get; set; }
    
    /// <summary>
    /// Indica se a chave foi revogada manualmente
    /// </summary>
    public bool IsRevoked { get; set; }
    
    // ========== Métodos ==========
    
    /// <summary>
    /// Obtém a chave pública (JsonWebKey) a partir dos parâmetros armazenados
    /// </summary>
    public JsonWebKey GetPublicKey()
    {
        if (string.IsNullOrEmpty(ParametersJson))
            throw new InvalidOperationException("Chave não possui parâmetros");
            
        return JsonSerializer.Deserialize<JsonWebKey>(ParametersJson)!;
    }
    
    /// <summary>
    /// Obtém as credenciais de assinatura para usar na geração de JWT
    /// </summary>
    public SigningCredentials GetSigningCredentials()
    {
        var key = GetPublicKey();
        return new SigningCredentials(key, Algorithm);
    }
    
    /// <summary>
    /// Define a chave privada (usada ao criar uma nova chave)
    /// </summary>
    public void SetPrivateKey(JsonWebKey key, string algorithm)
    {
        KeyId = key.KeyId;
        Type = key.Kty;
        Algorithm = algorithm;
        ParametersJson = JsonSerializer.Serialize(key);
        CreationDate = DateTime.UtcNow;
        ExpirationDate = CreationDate.AddDays(90); // Será sobrescrito pelas opções
    }
    
    /// <summary>
    /// Remove a chave privada dos parâmetros (mantém apenas a pública)
    /// Usado após período de rotação para economizar espaço e aumentar segurança
    /// </summary>
    public void RemovePrivateKey()
    {
        var publicKey = GetPublicKey();
        
        // Remove parâmetros privados de chaves EC
        publicKey.D = null;
        
        // Remove parâmetros privados de chaves RSA
        publicKey.DP = null;
        publicKey.DQ = null;
        publicKey.QI = null;
        publicKey.P = null;
        publicKey.Q = null;
        
        // Remove parâmetro privado de chaves HMAC
        publicKey.K = null;
        
        ParametersJson = JsonSerializer.Serialize(publicKey);
    }
}

