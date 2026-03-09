using DomainObjects.Attributes;
using DomainObjects.Enums;
using MessageBus.Messages;

namespace IntegrationHandlers.Events.EmailsWorker;
public abstract class EnvioEmailEvent : IntegrationEvent
{
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public abstract TipoEmailEvent TipoEmail { get; }
    public abstract string Assunto { get; }
    public abstract string Corpo { get; }
    public abstract bool IsHtml { get; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public DateTime? AgendadoPara { get; set; }
    public List<AnexoEvent> Anexos { get; set; } = [];
}
[Topic("email.recuperacaosenha")]
public class EmailRecuperacaoSenhaEvent(string assunto, bool isHtml) : EnvioEmailEvent
{
   
    public string TokenRecuperacao { get; set; } = string.Empty;
    public DateTime ExpiracaoToken { get; set; }
    public override string Assunto => assunto;
    public override string Corpo => GerarCorpoEmailRecuperacaoSenha();
    public override bool IsHtml => isHtml;
    public override TipoEmailEvent TipoEmail => TipoEmailEvent.RecuperacaoSenha;

    private string GerarCorpoEmailRecuperacaoSenha()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 30px; }}
        .codigo-box {{ 
            background: white; 
            border: 2px solid #007bff; 
            border-radius: 10px; 
            padding: 30px; 
            text-align: center; 
            margin: 30px 0; 
        }}
        .codigo {{ 
            font-size: 48px; 
            font-weight: bold; 
            color: #007bff; 
            letter-spacing: 10px; 
            font-family: 'Courier New', monospace;
        }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='content'>
            <h2>Olá,</h2>
            <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
            <p>Use o código de verificação abaixo para continuar:</p>
            
            <div class='codigo-box'>
                <p style='margin: 0; font-size: 14px; color: #666;'>Seu código de verificação:</p>
                <div class='codigo'>{TokenRecuperacao}</div>
                <p style='margin: 0; font-size: 12px; color: #999;'>Digite este código na tela de redefinição de senha</p>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Importante:</strong>
                <ul>
                    <li>Este código é válido por apenas <strong>30 minutos</strong></li>
                    <li>Por segurança, ele pode ser usado apenas <strong>uma vez</strong></li>
                    <li>Você tem no máximo <strong>5 tentativas</strong> para digitar o código correto</li>
                    <li>Se você não solicitou esta redefinição, ignore este email</li>
                    <li>Nunca compartilhe este código com ninguém</li>
                </ul>
            </div>
            <p>Se você não solicitou a redefinição de senha, sua conta permanece segura e você pode ignorar este email.</p>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; {DateTime.UtcNow.Year}. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }
}
[Topic("email.confirmacaoSenha")]
public class EmailConfirmacaoAlteracaoSenhaEvent(string assunto, bool isHtml) : EnvioEmailEvent
{
    public override string Assunto => assunto;
    public override bool IsHtml => isHtml;
    public override string Corpo => GerarHtmlConfirmacaoSenha();
    public override TipoEmailEvent TipoEmail => TipoEmailEvent.ConfirmacaoAlteracaoSenha;

    private static string GerarHtmlConfirmacaoSenha()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 30px; }}
        .success {{ background: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Senha Alterada</h1>
        </div>
        <div class='content'>
            <h2>Olá,</h2>
            <div class='success'>
                <p><strong>Sua senha foi alterada com sucesso!</strong></p>
                <p>Data/Hora: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</p>
            </div>
            <p>Você já pode fazer login com sua nova senha.</p>
            <p><strong>Se você não realizou esta alteração:</strong></p>
            <ul>
                <li>Entre em contato com o suporte imediatamente</li>
                <li>Sua conta pode ter sido comprometida</li>
            </ul>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; {DateTime.UtcNow.Year}. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }

}
[Topic("email.boasvindas")]
public class EmailBoasVindasEvent(string assunto, bool isHtml) : EnvioEmailEvent
{
    public string? TokenAtivacao { get; set; }
    public string? UrlAtivacao { get; set; }

    public override string Assunto => assunto;
    public override string Corpo => GerarCorpoEmailBoasVindas();
    public override bool IsHtml => isHtml;
    public override TipoEmailEvent TipoEmail => TipoEmailEvent.BoasVindas;
    private string GerarCorpoEmailBoasVindas()
    {
        var corpo = $@"
                <html>
                <body>
                    <h2>Bem-vindo ao Facimed, {Nome}!</h2>
                    <p>Seu cadastro foi realizado com sucesso.</p>";

        if (!string.IsNullOrEmpty(TokenAtivacao) && !string.IsNullOrEmpty(UrlAtivacao))
        {
            corpo += $@"
                    <p>Para ativar sua conta, clique no link abaixo:</p>
                    <p><a href='{UrlAtivacao}?token={TokenAtivacao}'>Ativar Conta</a></p>";
        }

        corpo += @"
                    <p>Obrigado por escolher o Facimed!</p>
                </body>
                </html>";

        return corpo;
    }
}
[Topic("email.generico")]
public class EmailEnviadoEvent(string assunto, string body, bool isHtml) : EnvioEmailEvent
{
    public override string Assunto => assunto;
    public override string Corpo => body;
    public override bool IsHtml => isHtml;
    public override TipoEmailEvent TipoEmail => TipoEmailEvent.EmailGenerico;
}
public class AnexoEvent
{
    public string NomeArquivo { get; set; } = string.Empty;
    public string TipoConteudo { get; set; } = string.Empty;
    public byte[] Conteudo { get; set; } = [];
}