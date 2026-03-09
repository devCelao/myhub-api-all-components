using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroserviceCore.Extensions;

namespace MicroserviceCore.Configuration;

public static class ContextConfigurations
{
    public static IServiceCollection AddContextCustomConfiguration<TContext>(this IServiceCollection Services,
                                                         IConfiguration Configuration,
                                                         bool usePool = true,
                                                         Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        var settingsSection = Configuration.GetSection(key: "Connection");

        Services.Configure<ConnectionSettings>(settingsSection);

        var settings = settingsSection.Get<ConnectionSettings>() ?? throw new InvalidOperationException($"Connection string section not found.");

        void Build(DbContextOptionsBuilder options)
        {
            options.UseMySql(settings.ConnectionString, ServerVersion.AutoDetect(settings.ConnectionString), mySql =>
            {
                mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });

            configureOptions?.Invoke(options);
        }

        if (usePool)
            Services.AddDbContextPool<TContext>(Build);
        else
            Services.AddDbContext<TContext>(Build);

        return Services;
    }
}

