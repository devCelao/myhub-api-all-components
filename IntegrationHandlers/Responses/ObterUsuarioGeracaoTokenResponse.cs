using MessageBus.Messages;

namespace IntegrationHandlers.Responses;

public class ObterUsuarioGeracaoTokenResponse : ResponseMessage
{
    public Guid IdUsuarioWorkspace { get; set; }
    public string Email { get; set; } = default!;
    public string NomeUsuario { get; set; } = default!;
    public List<Guid> Workspaces { get; set; } = [];
    public List<string>? Permissoes { get; set; } = [];
}
