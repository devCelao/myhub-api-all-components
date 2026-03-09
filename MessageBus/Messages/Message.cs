using FluentValidation.Results;

namespace MessageBus.Messages;

public abstract class Message
{
    public string MessageType { get; protected set; }
    public Guid AggregatedId { get; protected set; } = Guid.NewGuid();

    protected Message()
    {
        MessageType = GetType().Name;
    }
}

public class ResponseMessage : Message
{
    public ResponseMessage() { }
    public ResponseMessage(ValidationResult validationResult)
    {
        ValidationResult = validationResult;
    }

    public ResponseMessage(string key, string erro)
    {
        ValidationResult = new([new ValidationFailure(key, erro)]);
    }

    public ResponseMessage(string key, List<string> erros)
    {
        ValidationResult = new(
            erros.Select(erro => new ValidationFailure(key, erro)).ToList()
        );
    }
    public ValidationResult ValidationResult { get; set; } = new();
}