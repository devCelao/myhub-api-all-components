namespace SecurityCore.Enums;

/// <summary>
/// Tipo de chave criptográfica para JWKS
/// </summary>
public enum KeyType
{
    /// <summary>
    /// RSA - Rivest-Shamir-Adleman (chave assimétrica)
    /// </summary>
    RSA = 0,
    
    /// <summary>
    /// ECDsa - Elliptic Curve Digital Signature Algorithm (chave assimétrica)
    /// Usado para ES256, ES384, ES512
    /// </summary>
    ECDsa = 1,
    
    /// <summary>
    /// HMAC - Hash-based Message Authentication Code (chave simétrica)
    /// Usado para HS256, HS384, HS512
    /// </summary>
    HMAC = 2
}

