using DomainObjects.Enums;
using MessageBus.Messages;

namespace IntegrationHandlers.Events.ManagementAPI;

public class InterfaceAcessoCriadaEvent : IntegrationEvent
{
    public Guid IdInterface { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TipoToken TipoToken { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
