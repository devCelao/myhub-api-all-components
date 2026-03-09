using MicroserviceCore.Respostas;

namespace MicroserviceCore.Services;

public class BaseContextService
{
    protected readonly RespostaProcessamento result = new();

    protected void AddErroProcessamento(string error) => result.AddError(error);
    protected void AdicionaRetorno(object? retorno) => result.ResultObject = retorno;
    protected RespostaProcessamento RetornaProcessamento() => result;
}
