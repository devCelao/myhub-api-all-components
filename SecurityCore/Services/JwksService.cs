using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurityCore.Models;
using SecurityCore.Store;

namespace SecurityCore.Services;

/// <summary>
/// Interface para gerenciamento de chaves JWKS
/// </summary>
public interface IJwksService
{
    /// <summary>
    /// Obtém as credenciais de assinatura atuais (cria nova chave se necessário)
    /// </summary>
    SigningCredentials GetCurrent();
    
    /// <summary>
    /// Obtém as chaves públicas para exposição no endpoint JWKS
    /// </summary>
    /// <param name="quantity">Quantidade de chaves a retornar</param>
    IReadOnlyCollection<JsonWebKey> GetPublicKeys(int quantity = 5);
}

/// <summary>
/// Serviço principal para gerenciamento do ciclo de vida das chaves JWKS
/// Responsável por:
/// - Criar novas chaves quando necessário
/// - Rotacionar chaves antigas
/// - Remover chaves privadas antigas
/// - Fornecer chaves públicas para validação
/// </summary>
public class JwksService(
    IDatabaseJwksStore store,
    IJwkService jwkService,
    IOptions<JwksOptions> options) : IJwksService
{
    private readonly IDatabaseJwksStore _store = store;
    private readonly IJwkService _jwkService = jwkService;
    private readonly JwksOptions _options = options.Value;

    /// <summary>
    /// Obtém as credenciais de assinatura atuais
    /// Se não houver chave válida ou estiver próxima da expiração, gera uma nova
    /// </summary>
    public SigningCredentials GetCurrent()
    {
        // 1. Verifica se precisa gerar nova chave
        if (_store.NeedsUpdate(_options.DaysUntilExpire))
        {
            // Remove chaves privadas antigas (mantém apenas públicas para validação)
            RemoveOldPrivateKeys();
            
            // Gera nova chave
            return GenerateNewKey();
        }
        
        // 2. Retorna chave atual
        var currentKey = _store.GetCurrentKey();
        return currentKey.GetSigningCredentials();
    }
    
    /// <summary>
    /// Obtém as chaves públicas para exposição no endpoint JWKS
    /// Usado por outros serviços para validar tokens
    /// </summary>
    public IReadOnlyCollection<JsonWebKey> GetPublicKeys(int quantity = 5)
    {
        var keys = _store.GetKeys(quantity);
        return keys.Select(k => k.GetPublicKey()).ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Gera uma nova chave e salva no banco
    /// </summary>
    private SigningCredentials GenerateNewKey()
    {
        // Gera JsonWebKey com parâmetros públicos e privados
        var jsonWebKey = _jwkService.GenerateKey();
        
        // Cria entidade para armazenar no banco
        var securityKey = new SecurityKeyWithPrivate();
        securityKey.SetPrivateKey(jsonWebKey, _options.Algorithm);
        
        // Adiciona prefixo ao KeyId
        securityKey.KeyId = $"{_options.KeyPrefix}{securityKey.KeyId}";
        jsonWebKey.KeyId = securityKey.KeyId;
        jsonWebKey.Kid = securityKey.KeyId;
        
        // Define data de expiração baseada nas opções
        securityKey.ExpirationDate = securityKey.CreationDate.AddDays(_options.DaysUntilExpire);
        
        // Salva no banco
        _store.Save(securityKey);
        
        // Retorna credenciais para uso imediato
        return new SigningCredentials(jsonWebKey, _options.Algorithm);
    }
    
    /// <summary>
    /// Remove as chaves privadas antigas, mantendo apenas as públicas
    /// Isso economiza espaço e aumenta a segurança
    /// </summary>
    private void RemoveOldPrivateKeys()
    {
        // Obtém chaves antigas (além das que queremos manter)
        var allKeys = _store.GetKeys(_options.KeysToKeep + 10); // Pega mais para processar
        
        // Pula as N chaves mais recentes (KeysToKeep)
        var oldKeys = allKeys.Skip(_options.KeysToKeep);
        
        foreach (var key in oldKeys)
        {
            // Verifica se ainda tem chave privada
            if (!string.IsNullOrEmpty(key.ParametersJson) && 
                (key.ParametersJson.Contains("\"d\":") || key.ParametersJson.Contains("\"D\":")))
            {
                key.RemovePrivateKey();
                _store.Update(key);
            }
        }
    }
}

