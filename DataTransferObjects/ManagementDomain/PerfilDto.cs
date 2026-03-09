namespace DataTransferObjects.ManagementDomain;

public class PerfilDto
{
    public Guid IdPerfil { get; set; }
    public Guid IdGrupo { get; set; }
    public string TituloPerfil { get; set; } = string.Empty;
    public List<string> Servicos { get; set; } = [];
}
