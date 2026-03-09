using MessageBus.Interfaces;
using MessageBus.Messages.Authentication;
using MicroserviceCore.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecurityCore.Models;
using SecurityCore.Services;
using System.Text.Json;

namespace MicroserviceCore.Configuration;

/// <summary>
/// Extensões para configuração de autenticação JWT com suporte a subdomínios
/// </summary>
public static class JwtExtensionConfiguration
{
    /// <summary>
    /// Configura JWT para API de AUTENTICAÇÃO (que gera tokens)
    /// Inclui JWKS Services para geração de chaves e assinatura de tokens
    /// O Issuer é gerado DINAMICAMENTE baseado no subdomínio da requisição
    /// IMPORTANTE: Você deve registrar IDatabaseJwksStore manualmente no Program.cs
    /// </summary>
    public static IServiceCollection AddJwtAuthenticationProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurações JWT e JWKS
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<JwksOptions>(configuration.GetSection("Jwks"));

        // JWKS Services (para geração de tokens)
        services.AddScoped<IJwkService, JwkService>();
        services.AddScoped<IJwksService, JwksService>();
        // Nota: IDatabaseJwksStore deve ser registrado na API específica

        // Configuração JWT (com issuer dinâmico)
        AddJwtAuthenticationWithDynamicIssuer(services, configuration);

        return services;
    }

    /// <summary>
    /// Configura JWT para APIs CONSUMIDORAS (que apenas validam tokens)
    /// Valida tokens usando chaves públicas do endpoint JWKS da API de Autenticação
    /// O Issuer é validado DINAMICAMENTE baseado no subdomínio da requisição original
    /// NÃO gera tokens, NÃO faz refresh - apenas VALIDA
    /// </summary>
    public static IServiceCollection AddJwtAuthenticationConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração JWT (sem JWKS Services, sem refresh)
        AddJwtAuthenticationConsumerWithDynamicIssuer(services, configuration);

        return services;
    }

    /// <summary>
    /// Configuração de autenticação JWT para API Provider (gera e valida tokens)
    /// Issuer dinâmico baseado no subdomínio da requisição
    /// </summary>
    private static void AddJwtAuthenticationWithDynamicIssuer(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Configuração JWT não encontrada no appsettings.json");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    // Issuer será validado dinamicamente no evento OnTokenValidated
                    IssuerValidator = (issuer, securityToken, validationParameters) =>
                    {
                        // Aceita qualquer issuer que seja uma URL válida (será o subdomínio da API)
                        if (Uri.TryCreate(issuer, UriKind.Absolute, out _))
                        {
                            return issuer;
                        }
                        throw new SecurityTokenInvalidIssuerException($"Issuer inválido: {issuer}");
                    },

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true,
                    
                    // Chaves obtidas do banco (via IJwksService)
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        Console.WriteLine("🔑 [JWT Provider] Resolvendo chaves de assinatura do banco");
                        Console.WriteLine($"   🆔 Key ID (kid): {kid ?? "N/A"}");
                        
                        using var scope = services.BuildServiceProvider().CreateScope();
                        var jwksService = scope.ServiceProvider.GetRequiredService<IJwksService>();
                        var keys = jwksService.GetPublicKeys();
                        
                        Console.WriteLine($"   ✅ Chaves obtidas do banco: {keys.Count()}");
                        
                        return keys;
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Tenta ler do cookie primeiro usando o gerenciador centralizado
                        var token = AuthCookieManager.GetAccessToken(context.Request);
                        
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                            Console.WriteLine("🔍 [JWT Provider] Token obtido do cookie");
                        }
                        else
                        {
                            Console.WriteLine("🔍 [JWT Provider] Token não encontrado no cookie, usando header");
                        }
                        
                        Console.WriteLine($"   📍 Path: {context.Request.Path}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("✅ [JWT Provider] Token validado com sucesso!");
                        Console.WriteLine($"   👤 User: {context.Principal?.Identity?.Name ?? "N/A"}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("❌ [JWT Provider] Falha na autenticação");
                        Console.WriteLine($"   🚫 Erro: {context.Exception.GetType().Name}");
                        Console.WriteLine($"   💬 Mensagem: {context.Exception.Message}");
                        
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            Console.WriteLine("   ⏰ Token expirado!");
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    /// <summary>
    /// Configuração de autenticação JWT para APIs Consumidoras (apenas validam tokens)
    /// Busca chaves públicas via MessageBus da API de Autenticação
    /// Issuer dinâmico - aceita qualquer URL válida como issuer
    /// </summary>
    private static void AddJwtAuthenticationConsumerWithDynamicIssuer(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Configuração JWT não encontrada no appsettings.json");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    // Issuer dinâmico - aceita qualquer URL válida
                    IssuerValidator = (issuer, securityToken, validationParameters) =>
                    {
                        if (Uri.TryCreate(issuer, UriKind.Absolute, out _))
                        {
                            return issuer;
                        }
                        throw new SecurityTokenInvalidIssuerException($"Issuer inválido: {issuer}");
                    },

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true,
                    
                    // Chaves obtidas via MessageBus
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        Console.WriteLine("🔑 [JWT Consumer] Resolvendo chaves de assinatura via MessageBus");
                        Console.WriteLine($"   🆔 Key ID (kid): {kid ?? "N/A"}");
                        
                        var keys = FetchJwksFromMessageBus(services);
                        Console.WriteLine($"   ✅ Chaves obtidas: {keys.Count()}");
                        
                        return keys;
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Tenta ler do cookie primeiro usando o gerenciador centralizado
                        var token = AuthCookieManager.GetAccessToken(context.Request);
                        
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                            Console.WriteLine("🔍 [JWT Consumer] Token obtido do cookie");
                            Console.WriteLine($"   📝 Token Preview: {token.Substring(0, Math.Min(50, token.Length))}...");
                        }
                        else
                        {
                            Console.WriteLine("🔍 [JWT Consumer] Token não encontrado no cookie, usando header");
                        }
                        
                        Console.WriteLine($"   📍 Path: {context.Request.Path}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("✅ [JWT Consumer] Token validado com sucesso!");
                        Console.WriteLine($"   👤 User: {context.Principal?.Identity?.Name ?? "N/A"}");
                        Console.WriteLine($"   📋 Claims: {context.Principal?.Claims.Count() ?? 0}");
                        foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                        {
                            Console.WriteLine($"      - {claim.Type}: {claim.Value}");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("❌ [JWT Consumer] Falha na autenticação");
                        Console.WriteLine($"   🚫 Erro: {context.Exception.GetType().Name}");
                        Console.WriteLine($"   💬 Mensagem: {context.Exception.Message}");
                        
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            Console.WriteLine("   ⏰ Token expirado!");
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine("⚠️ [JWT Consumer] OnChallenge - Desafio de autenticação");
                        Console.WriteLine($"   📍 Path: {context.Request.Path}");
                        Console.WriteLine($"   🔍 Error: {context.Error ?? "N/A"}");
                        Console.WriteLine($"   📝 ErrorDescription: {context.ErrorDescription ?? "N/A"}");
                        
                        if (context.AuthenticateFailure != null)
                        {
                            Console.WriteLine($"   ❌ AuthenticateFailure: {context.AuthenticateFailure.GetType().Name}");
                            Console.WriteLine($"   💬 Message: {context.AuthenticateFailure.Message}");
                        }
                        
                        // Se token expirado, adiciona header informativo
                        if (context.AuthenticateFailure?.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    // Cache simples para JWKS (evita chamadas excessivas ao MessageBus)
    private static IEnumerable<SecurityKey>? _cachedKeys;
    private static DateTime _cacheExpiration = DateTime.MinValue;
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Busca chaves públicas JWKS via MessageBus
    /// Usa cache local para evitar chamadas excessivas
    /// </summary>
    private static IEnumerable<SecurityKey> FetchJwksFromMessageBus(IServiceCollection services)
    {
        lock (_cacheLock)
        {
            // Verifica se cache ainda é válido (5 minutos)
            if (_cachedKeys != null && DateTime.UtcNow < _cacheExpiration)
            {
                Console.WriteLine("📦 [JWT Consumer] Usando JWKS do cache");
                return _cachedKeys;
            }

            try
            {
                Console.WriteLine("📡 [JWT Consumer] Buscando JWKS via MessageBus...");
                
                using var scope = services.BuildServiceProvider().CreateScope();
                var busMessage = scope.ServiceProvider.GetRequiredService<IBusMessage>();
                
                var request = new GetJwksRequest();
                var response = busMessage.RequestAsync<GetJwksRequest, GetJwksResponse>(request)
                    .GetAwaiter().GetResult();
                
                if (string.IsNullOrEmpty(response.JwksJson))
                {
                    Console.WriteLine("⚠️ [JWT Consumer] JWKS vazio recebido");
                    return Enumerable.Empty<SecurityKey>();
                }
                
                Console.WriteLine($"   📦 JWKS recebido: {response.JwksJson.Length} caracteres");
                
                var jwks = JsonSerializer.Deserialize<JsonWebKeySet>(response.JwksJson);
                var keys = jwks?.Keys ?? Enumerable.Empty<SecurityKey>();
                
                // Atualiza cache
                _cachedKeys = keys.ToList();
                _cacheExpiration = DateTime.UtcNow.AddMinutes(5);
                
                Console.WriteLine($"   ✅ Total de chaves: {keys.Count()}");
                return _cachedKeys;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [JWT Consumer] ERRO ao buscar JWKS via MessageBus:");
                Console.WriteLine($"   🚫 Tipo: {ex.GetType().Name}");
                Console.WriteLine($"   💬 Mensagem: {ex.Message}");
                
                // Se tiver cache antigo, usa mesmo expirado
                if (_cachedKeys != null)
                {
                    Console.WriteLine("⚠️ [JWT Consumer] Usando cache expirado como fallback");
                    return _cachedKeys;
                }
                
                throw new InvalidOperationException("Erro ao buscar chaves JWKS via MessageBus", ex);
            }
        }
    }
}
