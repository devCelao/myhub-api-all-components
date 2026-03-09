using FluentValidation.Results;
using MediatR;
using MessageBus.Messages;

namespace MessageBus.Mediator;

public interface IMediatorHandler
{
    Task PublicarEvento<T>(T evento) where T : Event;
    Task<ValidationResult> EnviarComando<T>(T comando) where T : Command;
}
public class MediatorHandler(IMediator mediator) : IMediatorHandler
{
    private readonly IMediator _mediator = mediator;

    public async Task<ValidationResult> EnviarComando<T>(T comando) where T : Command => await _mediator.Send(comando);

    public async Task PublicarEvento<T>(T evento) where T : Event => await _mediator.Publish(evento);
}