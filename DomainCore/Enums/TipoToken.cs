namespace DomainObjects.Enums;

public enum TipoToken
{
    Nenhum = 0,
    Ativacao = 1,
    RedefinicaoSenha = 2,
    AcessoExterno = 3,        // Para integrações
    ConfirmacaoEmail = 4,     // Reenvio de confirmação
    AlteracaoEmail = 5,       // Confirmar novo email
    DoisFatores = 6          // Token 2FA temporário
}
