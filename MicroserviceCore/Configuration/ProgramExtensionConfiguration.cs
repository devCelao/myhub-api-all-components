using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace MicroserviceCore.Configuration;

public static class ProgramExtensionConfiguration
{
    public static void AddExtensionConfiguration(this IServiceCollection Services)
    {
        Services.AddControllers()
       .AddJsonOptions(o =>
       {
           o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
           //o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
       });
        
        Services.AddEndpointsApiExplorer();
    }

    public static void AddCustomCors(this IServiceCollection Services)
        =>
         Services.AddCors(options =>
         {
             options.AddPolicy("Total",
                 builder =>
                     builder
                         .SetIsOriginAllowed(origin =>
                         {
                             var uri = new Uri(origin);
                             return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                         })
                         .AllowCredentials()
                         .AllowAnyMethod()
                         .AllowAnyHeader());
         });

    public static void UseCustomCors(this IApplicationBuilder app)
        =>
        app.UseCors("Total");

    public static void UseCustomError(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

                var (status, code, message) = ex switch
                {
                    System.Text.Json.JsonException => (StatusCodes.Status400BadRequest, "invalid_payload", "Corpo JSON inválido."),
                    NotSupportedException => (StatusCodes.Status400BadRequest, "invalid_payload", "Conteúdo não suportado ou discriminador ausente."),
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "unauthorized", "Autenticação necessária."),
                    InvalidOperationException => (StatusCodes.Status409Conflict, "conflict", ex?.Message ?? "Operação inválida."),
                    _ => (StatusCodes.Status500InternalServerError, "internal_error", "Erro interno do servidor.")
                };

                var payload = new
                {
                    error = code,
                    message,
                    traceId
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(payload);
            });
        });
    }
}