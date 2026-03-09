using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
namespace MicroserviceCore.Configuration;

public static class ProgramConfiguration
{
    public static void AddJsonFile(this WebApplicationBuilder builder)
        =>
        builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
                                 .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: true)
                                 .AddJsonFile(path: $"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                                 .AddEnvironmentVariables();
}
