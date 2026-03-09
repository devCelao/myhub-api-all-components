using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace MicroserviceCore.Configuration;

public static class SwaggerConfiguration
{
    public static void AddSwaggerConfiguration(this IServiceCollection Services, OpenApiInfo infoApi)
    {
        Services.AddSwaggerGen(c =>
        {
            //c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //{
            //    Description = "Insira o token JWT desta maneira: Bearer {seu token}",
            //    Name = "Authorization",
            //    Scheme = "Bearer",
            //    BearerFormat = "JWT",
            //    In = ParameterLocation.Header,
            //    Type = SecuritySchemeType.ApiKey
            //});

            //c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //    {
            //        {
            //            new OpenApiSecurityScheme
            //            {
            //                Reference = new OpenApiReference
            //                {
            //                    Type = ReferenceType.SecurityScheme,
            //                    Id = "Bearer"
            //                }
            //            },
            //            []
            //        }
            //    });

            c.SwaggerDoc(name: "v1", info: infoApi);
        });
    }

    public static void UseSwaggerConfiguration(this IApplicationBuilder app, bool IsDevelopment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "");
            c.RoutePrefix = "swagger";
        });
        if (IsDevelopment)
        {
           
        }
    }
}
