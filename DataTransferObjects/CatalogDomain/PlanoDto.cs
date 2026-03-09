namespace DataTransferObjects.CatalogDomain;

public class PlanoDto
{
    public string CodPlano { get; set; } = default!;
    public string NomePlano { get; set; } = default!;
    public bool IndAtivo { get; set; }
    public bool IndGeraCobranca { get; set; }
    public decimal ValorBase { get; set; }
    public List<ServicoDto> Servicos { get; set; } = [];
}
