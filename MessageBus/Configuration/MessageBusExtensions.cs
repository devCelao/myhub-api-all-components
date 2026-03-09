using Microsoft.Extensions.Configuration;

namespace MessageBus.Configuration;

public static class MessageBusExtensions
{
    public static string? GetMessageQueueConnection(this IConfiguration configuration, string key) 
        => configuration?.GetSection("MessageQueueConnection")?[key: key];
}
