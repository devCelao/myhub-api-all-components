namespace DataTransferObjects.CatalogDomain;

public class ServicoDto
{
    public string CodServico { get; set; } = default!;
    public string NomeServico { get; set; } = default!;
    public string? Descricao { get; set; }
}
