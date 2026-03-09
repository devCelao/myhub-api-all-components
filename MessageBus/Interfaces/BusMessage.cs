using DomainObjects.Enums;
using DomainObjects.Extensions;
using EasyNetQ;
using EasyNetQ.DI;
using EasyNetQ.Internals;
using EasyNetQ.Serialization.SystemTextJson;
using MessageBus.Messages;
using Polly;
using RabbitMQ.Client.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MessageBus.Interfaces;


public interface IBusMessage : IDisposable
{
    bool IsConnected { get; }
    IAdvancedBus AdvancedBus { get; }

    bool Publish<T>(T message) where T : IntegrationEvent;

    Task<bool> PublishAsync<T>(T message, string? topic = null) where T : IntegrationEvent;

    bool Subscribe<T>(SubscriptioId subscriptionId, Action<T> onMessage) where T : class;

    bool SubscribeAsync<T>(SubscriptioId subscriptionId, Func<T, Task> onMessage) where T : class;
    bool SubscribeAsync<T>(SubscriptioId subscriptionId, Func<T, CancellationToken, Task> onMessage, Action<ISubscriptionConfiguration> config) where T : class;

    TResponse Request<TRequest, TResponse>(TRequest request)
        where TRequest : IntegrationEvent
        where TResponse : ResponseMessage;

    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IntegrationEvent
        where TResponse : ResponseMessage;

    IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        where TRequest : IntegrationEvent
        where TResponse : ResponseMessage;

    AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        where TRequest : IntegrationEvent
        where TResponse : ResponseMessage;
}
public class BusMessage : IBusMessage
{
    private IBus _bus = default!;
    private IAdvancedBus _advancedBus = default!;
    private readonly string _connectionString;
    public bool IsConnected => _bus?.Advanced?.IsConnected ?? false;
    public IAdvancedBus AdvancedBus => _bus.Advanced;


    private static readonly Policy _retryPolicy = Policy.Handle<EasyNetQException>()
                                                        .Or<BrokerUnreachableException>()
                                                        .WaitAndRetry(3, retryAttempt =>
                                                            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static readonly Policy _foreverPolicy = Policy.Handle<EasyNetQException>()
                                                          .Or<BrokerUnreachableException>()
                                                          .RetryForever();

    private void OnDisconnect(object? s, EventArgs e) => _foreverPolicy.Execute(TryConnect);
    public void Dispose() => _bus.Dispose();
    public BusMessage(string connectionString)
    {
        _connectionString = connectionString;
        TryConnect();
    }

    private void TryConnect()
    {
        if (IsConnected) return;

        _retryPolicy.Execute(() =>
        {
            _bus = RabbitHutch.CreateBus(_connectionString, reg =>
            {
                var opts = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                reg.Register<ISerializer>(_ => new SystemTextJsonSerializer(opts));
            });
            _advancedBus = _bus.Advanced;
            _advancedBus.Disconnected += OnDisconnect;
        });
    }

    public bool Publish<T>(T message) where T : IntegrationEvent
    {
        try
        {
            TryConnect();
            _bus.PubSub.Publish(message);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> PublishAsync<T>(T message, string? topic = null) where T : IntegrationEvent
    {
        try
        {
            TryConnect();
            if (string.IsNullOrEmpty(topic))
                await _bus.PubSub.PublishAsync(message);
            else
                await _bus.PubSub.PublishAsync(message, topic);

            return true;
        }
        catch
        {
            return false;
        }

    }
    public bool Subscribe<T>(SubscriptioId subscriptionId, Action<T> onMessage) where T : class
    {
        try
        {
            TryConnect();
            _bus.PubSub.Subscribe(subscriptionId.GetDescription(), onMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public bool SubscribeAsync<T>(SubscriptioId subscriptionId, Func<T, Task> onMessage) where T : class
    {
        try
        {
            TryConnect();
            _bus.PubSub.SubscribeAsync(subscriptionId.GetDescription(), onMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public bool SubscribeAsync<T>(SubscriptioId subscriptionId, Func<T, CancellationToken, Task> onMessage, Action<ISubscriptionConfiguration> config) where T : class
    {
        try
        {
            TryConnect();

            _bus.PubSub.SubscribeAsync<T>(
                    subscriptionId: subscriptionId.GetDescription(),
                    onMessage: onMessage,
                    configure: config
                );

            return true;
        }
        catch
        {
            return false;
        }
       
    }
    public TResponse Request<TRequest, TResponse>(TRequest request) where TRequest : IntegrationEvent
            where TResponse : ResponseMessage
    {
        TryConnect();
        return _bus.Rpc.Request<TRequest, TResponse>(request);
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IntegrationEvent where TResponse : ResponseMessage
    {
        TryConnect();
        return await _bus.Rpc.RequestAsync<TRequest, TResponse>(request);
    }

    public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        where TRequest : IntegrationEvent where TResponse : ResponseMessage
    {
        TryConnect();
        return _bus.Rpc.Respond(responder);
    }

    public AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        where TRequest : IntegrationEvent where TResponse : ResponseMessage
    {
        TryConnect();
        return _bus.Rpc.RespondAsync(responder);
    }
}
