
using MicroserviceCore.Comunication;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MicroserviceCore.Controller;
[ApiController]
public class BaseController : ControllerBase
{
    protected ICollection<string> Erros = [];
    protected bool OperacaoValida() => Erros.Count == 0;
    protected void AdicionarErrosProcessamento(string erro) => Erros.Add(erro);
    protected void AdicionarListaErros(List<string> erros) => Erros = erros;
    protected void LimpaErrosProcessamento() => Erros.Clear();
    protected bool ResponsePossuiErros(ResponseResult resposta)
    {
        if (resposta == null || resposta.Errors.Mensagens.Count == 0) return false;

        foreach (var mensagem in resposta.Errors.Mensagens)
        {
            AdicionarErrosProcessamento(mensagem);
        }

        return true;
    }

    protected ActionResult CustomResponse(object? result = null)
    {
        if (OperacaoValida()) return Ok(result);

        return BadRequest(new Dictionary<string, string[]> { { "Mensagens", Erros.ToArray() } });
    }

    protected ActionResult CustomResponse(ModelStateDictionary modelState)
    {
        var erros = modelState.Values.SelectMany(e => e.Errors);
        foreach (var erro in erros)
        {
            AdicionarErrosProcessamento(erro.ErrorMessage);
        }

        return CustomResponse();
    }

    protected ActionResult CustomResponse(ValidationResult resultValidation)
    {
        foreach (var erro in resultValidation.Errors)
        {
            AdicionarErrosProcessamento(erro.ErrorMessage);
        }

        return CustomResponse();
    }
    protected ActionResult CustomResponse(ResponseResult resposta)
    {
        ResponsePossuiErros(resposta);

        return CustomResponse();
    }
}