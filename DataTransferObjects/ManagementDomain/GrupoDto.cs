namespace DataTransferObjects.ManagementDomain;

public class GrupoDto
{
    public Guid IdGrupo { get; set; }
    public ICollection<UsuarioDto> Usuarios { get; set; } = [];
}
