using MessageBus.Interfaces;
using MessageBus.Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MessageBus.Configuration;

public static class MessageBusDependencyInjection
{
    public static void AddMessageBusResponder<TResponder>(this IServiceCollection Services,
                                                          IConfiguration Configuration) where TResponder : class, IHostedService
    {
        string? connection = Configuration.GetMessageQueueConnection("MessageBus");

        if (string.IsNullOrEmpty(connection))
            throw new ArgumentNullException(nameof(Configuration), "The connection string cannot be null or empty.");
        
        Services.AddSingleton<IBusMessage>(new BusMessage(connection));
        Services.AddHostedService<TResponder>();
        Services.AddScoped<IMediatorHandler, MediatorHandler>();
    } 

    public static void AddMessageBusRequest(this IServiceCollection Services,
                                                 IConfiguration Configuration)
    {
        string? connection = Configuration.GetMessageQueueConnection("MessageBus");

        if (string.IsNullOrEmpty(connection))
            throw new ArgumentNullException(nameof(Configuration), "The connection string cannot be null or empty.");

        Services.AddSingleton<IBusMessage>(new BusMessage(connection));
    } 
}
