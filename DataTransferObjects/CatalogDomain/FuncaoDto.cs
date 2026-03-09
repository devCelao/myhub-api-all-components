namespace DataTransferObjects.CatalogDomain;

public class FuncaoDto
{
    public string CodFuncao { get; set; } = default!;
    public string CodServico { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string? Descricao { get; set; }
    public string? Icone { get; set; }
    public int NumOrdem { get; set; }
    public bool IndAtivo { get; set; }
}

