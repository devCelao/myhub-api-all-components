using MessageBus.Messages;

namespace IntegrationHandlers.Requests;

public class ObterUsuarioGeracaoTokenRequest : IntegrationEvent
{
    public Guid IdUsuario { get; set; }
    public Guid? IdWorkspace { get; set; }
}

