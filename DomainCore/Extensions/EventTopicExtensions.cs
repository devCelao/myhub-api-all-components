using DomainObjects.Attributes;
using System.Collections.Concurrent;

namespace DomainObjects.Extensions;

public static class EventTopicExtensions
{
    private static readonly ConcurrentDictionary<Type, string?> Cache = new();

    public static string? GetEventTopic<TEvent>() where TEvent : class
        => Cache.GetOrAdd(typeof(TEvent), t => t.GetCustomAttributes(typeof(TopicAttribute), true)
                                                   .FirstOrDefault() is TopicAttribute attr ? attr.Key : string.Empty);
}
