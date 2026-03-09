using MessageBus.Messages;

namespace IntegrationHandlers.Requests;

public class ObterServicosPlanoRequest : IntegrationEvent
{
    public string CodPlano { get; set; } = string.Empty;
}