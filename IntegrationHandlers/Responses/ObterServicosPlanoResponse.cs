using DataTransferObjects.CatalogDomain;
using MessageBus.Messages;

namespace IntegrationHandlers.Responses;

public class ObterServicosPlanoResponse : ResponseMessage
{
    public ObterServicosPlanoResponse() { }
    public ObterServicosPlanoResponse(string key, string erro) : base(key, erro) { }

    public ObterServicosPlanoResponse(string key, List<string> erros) : base (key, erros) { }

    public PlanoDto Plano { get; set; } = new();
}
