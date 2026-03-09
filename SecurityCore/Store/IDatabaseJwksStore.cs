using SecurityCore.Models;

namespace SecurityCore.Store;

/// <summary>
/// Interface para armazenamento de chaves JWKS no banco de dados
/// </summary>
public interface IDatabaseJwksStore
{
    /// <summary>
    /// Salva uma nova chave no banco
    /// </summary>
    void Save(SecurityKeyWithPrivate key);
    
    /// <summary>
    /// Atualiza uma chave existente (usado para remover chave privada)
    /// </summary>
    void Update(SecurityKeyWithPrivate key);
    
    /// <summary>
    /// Obtém a chave atual (mais recente e não expirada)
    /// </summary>
    SecurityKeyWithPrivate GetCurrentKey();
    
    /// <summary>
    /// Obtém as N chaves mais recentes
    /// </summary>
    /// <param name="quantity">Quantidade de chaves a retornar</param>
    IReadOnlyCollection<SecurityKeyWithPrivate> GetKeys(int quantity);
    
    /// <summary>
    /// Verifica se precisa criar uma nova chave
    /// </summary>
    /// <param name="daysUntilExpire">Dias antes da expiração para considerar necessidade de nova chave</param>
    bool NeedsUpdate(int daysUntilExpire);
}

