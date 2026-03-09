using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace SecurityCore.Services;

/// <summary>
/// Serviço para operações criptográficas e geração de chaves
/// </summary>
public static class CryptoService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
    
    /// <summary>
    /// Cria um identificador único seguro usando Base64Url
    /// </summary>
    /// <param name="length">Tamanho em bytes (padrão: 16 bytes = 128 bits)</param>
    /// <returns>String Base64Url única</returns>
    public static string CreateUniqueId(int length = 16)
    {
        var bytes = new byte[length];
        Rng.GetBytes(bytes);
        return Base64UrlEncoder.Encode(bytes);
    }
    
    /// <summary>
    /// Cria uma chave ECDsa (Elliptic Curve) para ES256
    /// Usa a curva P-256 (nistP256) que é a mais comum e segura
    /// </summary>
    /// <returns>Chave ECDsa com KeyId único</returns>
    public static ECDsaSecurityKey CreateECDsaKey()
    {
        // P-256 (secp256r1) é a curva recomendada para ES256
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        
        return new ECDsaSecurityKey(ecdsa)
        {
            KeyId = CreateUniqueId()
        };
    }
}

