using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroserviceCore.Extensions;

namespace MicroserviceCore.Configuration;

public static class APIExtensionConfiguration
{
    public static void AddHostsAPIConfiguration(this IServiceCollection Services, IConfiguration Configuration)
    {
        Services.Configure<ServicesHostSettingsModel>(Configuration);
    }
}
