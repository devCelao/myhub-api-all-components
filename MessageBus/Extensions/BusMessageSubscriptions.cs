using DomainObjects.Attributes;
using DomainObjects.Enums;
using DomainObjects.Extensions;
using EasyNetQ;
using MessageBus.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace MessageBus.Extensions;

public static class BusMessageSubscriptions
{
    public static void SubscribeAllDerivedFrom<TBase>(
        this IBusMessage bus,
        SubscriptioId subscriptionId,
        Func<TBase, CancellationToken, Task> handler,
        ushort prefetch = 10,
        bool durable = true,
        string? defaultTopic = null)
        where TBase : class
    {
        var subscribeMethod = typeof(IBusMessage)
            .GetMethods()
            .First(m =>
            {
                if (m.Name != nameof(IBusMessage.SubscribeAsync) || !m.IsGenericMethodDefinition)
                    return false;
                var p = m.GetParameters();
                return p.Length == 3
                    && p[0].ParameterType == typeof(SubscriptioId)
                    && p[1].ParameterType.IsGenericType
                    && p[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,,>)
                    && p[2].ParameterType == typeof(Action<ISubscriptionConfiguration>);
            });




        var eventTypes = typeof(TBase).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(TBase).IsAssignableFrom(t))
            .ToArray();

        foreach (var t in eventTypes)
        {
            var topic = !string.IsNullOrWhiteSpace(defaultTopic)
                ? defaultTopic!                                       
                : (t.GetCustomAttribute<TopicAttribute>()?.Key ?? "#");

            var closed = subscribeMethod.MakeGenericMethod(t);
            var adapted = CreateTypedHandler(handler, t);

            var queueName = $"{subscriptionId.GetDescription()}.{t.Name}.{Sanitize(topic)}";

            Action<ISubscriptionConfiguration> cfg = c =>
            {
                c.WithTopic(topic);
                c.WithPrefetchCount(prefetch);
                if (durable) c.WithDurable(true);
                c.WithQueueName(queueName);
            };

            closed.Invoke(bus, [subscriptionId, adapted, cfg]);
        }
    }
    private static object CreateTypedHandler<TBase>(
        Func<TBase, CancellationToken, Task> handler,
        Type concreteType) where TBase : class
    {
        var msg = Expression.Parameter(concreteType, "msg");
        var ct = Expression.Parameter(typeof(CancellationToken), "ct");

        var cast = Expression.Convert(msg, typeof(TBase));
        var invoke = Expression.Call(Expression.Constant(handler),
                                     handler.GetType().GetMethod("Invoke")!,
                                     cast, ct);

        var funcType = typeof(Func<,,>).MakeGenericType(concreteType, typeof(CancellationToken), typeof(Task));
        return Expression.Lambda(funcType, invoke, msg, ct).Compile();
    }

    static string Sanitize(string s) 
        => s.Replace('*', 'x').Replace('#', 'h').Replace('.', '_');
}
