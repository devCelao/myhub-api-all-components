namespace DataTransferObjects.ManagementDomain;

public class UsuarioDto
{
    public Guid IdUsuario { get; set; }
    public string? Email { get; set; }
    public string? Nome { get; set; }
    public ICollection<PerfilDto> Perfis { get; set; } = [];
}