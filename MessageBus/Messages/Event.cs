using DomainObjects.Attributes;
using MediatR;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MessageBus.Messages;
public class Event : Message, INotification
{
    public DateTime Timestamp { get; private set; } = DateTime.Now;
}
public abstract class IntegrationEvent : Event
{
    private static readonly ConcurrentDictionary<Type, string?> _topicCache = new();
    [JsonIgnore] // evita ir para o JSON
    public virtual string? Topic => GetTopicFor(GetType());

    protected static string? GetTopicFor(Type t) =>
        _topicCache.GetOrAdd(t, static tt =>
            tt.GetCustomAttribute<TopicAttribute>()?.Key
        );
}