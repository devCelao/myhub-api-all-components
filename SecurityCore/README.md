# SecurityCore 🔐

Componente responsável pelo gerenciamento de chaves JWKS (JSON Web Key Set) para assinatura e validação de tokens JWT no FacimedEnterprise.

## 📦 O que é?

O **SecurityCore** é um componente simplificado que implementa o padrão JWKS para:

- ✅ Gerar chaves criptográficas ES256 (Elliptic Curve)
- ✅ Gerenciar ciclo de vida das chaves (criação, rotação, expiração)
- ✅ Armazenar chaves no banco de dados
- ✅ Expor chaves públicas para validação de tokens

## 🏗️ Estrutura

```
SecurityCore/
├── Enums/
│   └── KeyType.cs                    // Tipos de chave (RSA, ECDsa, HMAC)
├── Models/
│   ├── JwtOptions.cs                 // Configurações JWT
│   ├── JwksOptions.cs                // Configurações JWKS
│   └── SecurityKeyWithPrivate.cs     // Entidade de chave
├── Services/
│   ├── CryptoService.cs              // Geração de chaves
│   ├── JwkService.cs                 // Criação de JsonWebKey
│   └── JwksService.cs                // Gerenciamento de ciclo de vida
└── Store/
    └── IDatabaseJwksStore.cs         // Interface de armazenamento
```

## 🚀 Como Usar

### 1. Adicionar Referência

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\Componentes\SecurityCore\SecurityCore.csproj" />
</ItemGroup>
```

### 2. Configurar no Program.cs

```csharp
using SecurityCore.Models;
using SecurityCore.Services;
using SecurityCore.Store;

// Configurações
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<JwksOptions>(
    builder.Configuration.GetSection("Jwks"));

// Services
builder.Services.AddScoped<IJwkService, JwkService>();
builder.Services.AddScoped<IJwksService, JwksService>();
builder.Services.AddScoped<IDatabaseJwksStore, SuaImplementacaoDoStore>();
```

### 3. Usar no TokenService

```csharp
public class TokenService : ITokenService
{
    private readonly IJwksService _jwksService;
    
    public TokenService(IJwksService jwksService)
    {
        _jwksService = jwksService;
    }
    
    public async Task<string> GerarAccessToken(Usuario usuario)
    {
        // Obtém credenciais de assinatura (cria nova chave se necessário)
        var signingCredentials = _jwksService.GetCurrent();
        
        // Cria token JWT
        var token = _tokenHandler.CreateJwtSecurityToken(
            issuer: "FacimedEnterprise.Authentication",
            audience: "FacimedEnterprise.API",
            subject: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: signingCredentials // ← JWKS aqui!
        );
        
        return _tokenHandler.WriteToken(token);
    }
}
```

### 4. Expor Endpoint JWKS

```csharp
[ApiController]
[Route(".well-known")]
public class JwksController : ControllerBase
{
    private readonly IJwksService _jwksService;
    
    [HttpGet("jwks.json")]
    public IActionResult GetJwks()
    {
        var keys = _jwksService.GetPublicKeys(5);
        return Ok(new { keys });
    }
}
```

## ⚙️ Configurações

### appsettings.json

```json
{
  "Jwt": {
    "Issuer": "FacimedEnterprise.Authentication",
    "Audience": "FacimedEnterprise.API",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "Jwks": {
    "Algorithm": "ES256",
    "DaysUntilExpire": 90,
    "KeysToKeep": 2,
    "KeyPrefix": "Facimed_"
  }
}
```

### Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Algorithm` | Algoritmo de assinatura | ES256 |
| `DaysUntilExpire` | Dias até rotacionar chave | 90 |
| `KeysToKeep` | Chaves com privada a manter | 2 |
| `KeyPrefix` | Prefixo do KeyId | Facimed_ |

## 🔄 Rotação Automática

O **JwksService** gerencia automaticamente a rotação de chaves:

1. **Dia 0**: Cria primeira chave
2. **Dia 90**: Cria nova chave, remove privada da anterior
3. **Dia 180**: Cria nova chave, remove privada da anterior
4. **Contínuo**: Mantém sempre 2 chaves com privada + N chaves públicas

## 🔐 Segurança

- ✅ **ES256**: Algoritmo Elliptic Curve (mais seguro que HS256)
- ✅ **Rotação**: Chaves rotacionadas automaticamente
- ✅ **Privacidade**: Chaves privadas removidas após rotação
- ✅ **Múltiplas Chaves**: Suporta validação de tokens com chaves antigas

## 📦 Dependências

```xml
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.2.1" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
```

## 🧪 Exemplo de Uso Completo

```csharp
// 1. Configurar serviços
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));
builder.Services.Configure<JwksOptions>(config.GetSection("Jwks"));
builder.Services.AddScoped<IJwkService, JwkService>();
builder.Services.AddScoped<IJwksService, JwksService>();
builder.Services.AddScoped<IDatabaseJwksStore, DatabaseJwksStore>();

// 2. Gerar token
var jwksService = serviceProvider.GetRequiredService<IJwksService>();
var signingCredentials = jwksService.GetCurrent();
var token = CreateJwtToken(signingCredentials);

// 3. Expor chaves públicas
var publicKeys = jwksService.GetPublicKeys(5);
// Retorna: [{ kty: "EC", kid: "Facimed_abc", x: "...", y: "...", ... }]

// 4. Validar token em outras APIs
var keys = await HttpClient.GetAsync("/.well-known/jwks.json");
var isValid = ValidateToken(token, keys);
```

## 📚 Documentação

- [COMO_FUNCIONA_JWKS.md](../../Services/Authentication/COMO_FUNCIONA_JWKS.md) - Explicação detalhada
- [PLANO_IMPLEMENTACAO_JWKS.md](../../Services/Authentication/PLANO_IMPLEMENTACAO_JWKS.md) - Plano de implementação

## 🤝 Contribuindo

Este é um componente interno do FacimedEnterprise. Para modificações:

1. Mantenha a simplicidade (apenas ES256)
2. Adicione testes para novas funcionalidades
3. Atualize a documentação
4. Verifique compatibilidade com RFC 7517

## 📄 Licença

Propriedade do FacimedEnterprise - Uso interno apenas.

---

**Versão:** 1.0  
**Última atualização:** 28/10/2025

