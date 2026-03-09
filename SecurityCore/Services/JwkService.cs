using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace SecurityCore.Services;

/// <summary>
/// Interface para geração de JsonWebKey
/// </summary>
public interface IJwkService
{
    /// <summary>
    /// Gera uma nova JsonWebKey para ES256
    /// </summary>
    JsonWebKey GenerateKey();
}

/// <summary>
/// Serviço para geração de JsonWebKey (JWK)
/// Versão simplificada que suporta apenas ES256
/// </summary>
public class JwkService : IJwkService
{
    /// <summary>
    /// Gera uma nova chave JsonWebKey para ES256
    /// </summary>
    /// <returns>JsonWebKey com parâmetros públicos e privados</returns>
    public JsonWebKey GenerateKey()
    {
        // Cria chave ECDsa (sempre ES256 na versão simplificada)
        var key = CryptoService.CreateECDsaKey();
        
        // Exporta os parâmetros incluindo a chave privada
        var parameters = key.ECDsa.ExportParameters(includePrivateParameters: true);
        
        // Cria JsonWebKey com todos os parâmetros
        return new JsonWebKey
        {
            // Tipo da chave: EC (Elliptic Curve)
            Kty = "EC",
            
            // Uso: sig (signature)
            Use = "sig",
            
            // Key ID (identificador único)
            Kid = key.KeyId,
            KeyId = key.KeyId,
            
            // Coordenadas do ponto público (X, Y)
            X = Base64UrlEncoder.Encode(parameters.Q.X!),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y!),
            
            // Chave privada (D)
            D = Base64UrlEncoder.Encode(parameters.D!),
            
            // Curva: P-256 (secp256r1)
            Crv = "P-256",
            
            // Algoritmo: ES256 (ECDSA com SHA-256)
            Alg = "ES256"
        };
    }
}

