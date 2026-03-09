using FluentValidation.Results;
using MediatR;

namespace MessageBus.Messages;

public abstract class Command : Message, IRequest<ValidationResult>
{
    public DateTime Timestamp { get; private set; } = DateTime.Now;
    public ValidationResult? ValidationResult { get; set; } 

    public virtual bool Valido() => throw new NotImplementedException();
}

public abstract class CommandHandler
{
    protected ValidationResult ValidationResult = new();

    protected void AdicionarErro(string mensagem)
        => ValidationResult.Errors.Add(new ValidationFailure(propertyName: string.Empty, errorMessage: mensagem));

    protected void AdicionaErros(IEnumerable<string> mensagens)
    {
        foreach (var mensagem in mensagens) AdicionarErro(mensagem);
    }
    

    protected ValidationResult PersistirDados(bool sucesso)
    {
        if (!sucesso) AdicionarErro("Houve um erro ao persistir os dados");

        return ValidationResult;
    }
}