namespace MicroserviceCore.Respostas;

public class RespostaProcessamento
{
    public object? ResultObject { get; set; }
    public List<string> Errors { get; set; } = [];

    public bool PossuiErros => Errors.Count > 0;
    public void AddError(string error) => Errors.Add(error);
}
