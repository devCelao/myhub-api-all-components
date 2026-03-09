# MyHub API - All Components

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 📋 Descrição

Biblioteca de componentes compartilhados para a arquitetura de microserviços do MyHub. Este repositório contém componentes reutilizáveis que fornecem funcionalidades essenciais para todos os microserviços da plataforma, incluindo segurança, comunicação, validação e infraestrutura base.

## 🏗️ Arquitetura

Este projeto segue os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**, fornecendo componentes modulares e desacoplados que podem ser utilizados por diferentes microserviços.

## 📦 Componentes

### 1. **MicroserviceCore**
Núcleo de infraestrutura para microserviços com funcionalidades comuns:

- **Controllers Base**: `BaseController`, `RootController` com padronização de respostas
- **Middleware**: `TokenRefreshMiddleware` para renovação automática de tokens
- **Configurações**: Extensões para JWT, Swagger, API e contextos de banco de dados
- **Services Base**: `BaseClientServices`, `BaseContextService` para operações CRUD
- **Respostas Padronizadas**: `ServiceResult`, `ResponseResult`, `ApiResponse`
- **Gerenciamento de Cookies**: `AuthCookieManager` para autenticação
- **Suporte a Bancos**: SQL Server e MySQL (via Pomelo)

**Principais Dependências:**
- Entity Framework Core 9.0
- FluentValidation
- JWT Bearer Authentication
- Polly (Resiliência)
- Swashbuckle (Swagger/OpenAPI)

### 2. **SecurityCore**
Componente de segurança com gerenciamento de tokens JWT e JWKS:

- **Serviços de Criptografia**: `CryptoService` para operações criptográficas
- **Gerenciamento JWT**: `JwkService`, `JwksService` para criação e validação de tokens
- **Issuer Service**: Gerenciamento de emissores de tokens
- **JWKS Store**: Interface para armazenamento de chaves públicas
- **Modelos**: `JwtOptions`, `JwksOptions`, `SecurityKeyWithPrivate`
- **Enums**: `KeyType` para tipos de chaves criptográficas

**Principais Dependências:**
- System.IdentityModel.Tokens.Jwt
- Microsoft.IdentityModel.Tokens

### 3. **MessageBus**
Sistema de mensageria baseado em MediatR para comunicação entre componentes:

- **Abstrações**: `Message`, `Command`, `Event`, `BusMessage`
- **Mediator Handler**: `MediatorHandler` para orquestração de mensagens
- **Mensagens de Autenticação**:
  - `RefreshTokenRequest/Response`
  - `ValidateSessionRequest/Response`
  - `GetJwksRequest/Response`
- **Extensões**: `BusMessageSubscriptions`, `MessageBusExtensions`
- **Dependency Injection**: Configuração automática do MediatR

### 4. **DomainCore** (DomainObjects)
Objetos de domínio e entidades base para DDD:

- Entidades base
- Value Objects
- Agregados
- Especificações de domínio

### 5. **DataTransferObjects**
DTOs para transferência de dados entre camadas e serviços:

- Contratos de requisição/resposta
- Modelos de visualização
- Objetos de transferência padronizados

### 6. **IntegrationHandlers**
Handlers para integrações e comunicação entre microserviços:

- Handlers de comandos
- Handlers de eventos
- Processamento de mensagens de integração

## 🚀 Como Usar

### Instalação

Adicione as referências dos projetos necessários ao seu microserviço:

```xml
<ItemGroup>
  <ProjectReference Include="..\myhub-api-all-components\MicroserviceCore\MicroserviceCore.csproj" />
  <ProjectReference Include="..\myhub-api-all-components\SecurityCore\SecurityCore.csproj" />
  <ProjectReference Include="..\myhub-api-all-components\MessageBus\MessageBus.csproj" />
</ItemGroup>
```

### Configuração Básica

#### 1. Configurar MicroserviceCore no `Program.cs`:

```csharp
using MicroserviceCore.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configurar serviços base do microserviço
builder.Services.AddMicroserviceCore(builder.Configuration);

// Configurar JWT
builder.Services.AddJwtConfiguration(builder.Configuration);

// Configurar Swagger
builder.Services.AddSwaggerConfiguration("Meu Microserviço API", "v1");

var app = builder.Build();

// Configurar pipeline
app.UseMicroserviceCore();
app.UseTokenRefreshMiddleware();

app.Run();
```

#### 2. Usar BaseController:

```csharp
using MicroserviceCore.Controller;

[ApiController]
[Route("api/[controller]")]
public class MeuController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await _service.ObterDados();
        return CustomResponse(resultado);
    }
}
```

#### 3. Configurar MessageBus:

```csharp
using MessageBus.Configuration;

// No Program.cs
builder.Services.AddMessageBus();
```

#### 4. Criar um Command Handler:

```csharp
using MessageBus.Messages;
using MediatR;

public class MeuCommand : Command
{
    public string Dados { get; set; }
}

public class MeuCommandHandler : IRequestHandler<MeuCommand, bool>
{
    public async Task<bool> Handle(MeuCommand request, CancellationToken cancellationToken)
    {
        // Lógica do handler
        return true;
    }
}
```

## ⚙️ Configuração

### appsettings.json

```json
{
  "JwtOptions": {
    "Issuer": "https://seu-issuer.com",
    "Audience": "sua-audience",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyHub;User Id=sa;Password=senha;"
  },
  "ServicesHostSettings": {
    "AuthenticationService": "https://auth-service.com"
  }
}
```

## 🧪 Testes

```bash
dotnet test
```

## 📝 Convenções

### Padrões de Código
- **Nomenclatura**: PascalCase para classes e métodos, camelCase para variáveis
- **Async/Await**: Todos os métodos assíncronos devem ter sufixo `Async`
- **Validação**: Usar FluentValidation para validação de entrada
- **Respostas**: Sempre usar `ServiceResult` ou `ResponseResult` para padronizar retornos

### Estrutura de Pastas
```
ComponentName/
├── Configuration/      # Configurações e extensões DI
├── Controllers/        # Controllers base
├── Services/          # Serviços e lógica de negócio
├── Models/            # Modelos e DTOs
├── Middleware/        # Middlewares customizados
└── Extensions/        # Métodos de extensão
```

## 🔒 Segurança

- Autenticação via **JWT Bearer Token**
- Suporte a **JWKS** (JSON Web Key Set)
- Renovação automática de tokens via middleware
- Gerenciamento seguro de cookies de autenticação
- Criptografia de dados sensíveis

## 🛠️ Tecnologias

- **.NET 9.0**
- **Entity Framework Core 9.0**
- **MediatR** (MessageBus)
- **FluentValidation**
- **Polly** (Resiliência)
- **JWT Bearer Authentication**
- **Swagger/OpenAPI**
- **SQL Server / MySQL**

## 📚 Dependências Principais

| Pacote | Versão | Descrição |
|--------|--------|-----------|
| Microsoft.EntityFrameworkCore | 9.0.9 | ORM para acesso a dados |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.9 | Autenticação JWT |
| FluentValidation | 12.0.0 | Validação de objetos |
| Polly | 8.6.4 | Resiliência e retry policies |
| Swashbuckle.AspNetCore | 9.0.6 | Documentação OpenAPI |
| System.IdentityModel.Tokens.Jwt | 8.2.1 | Manipulação de JWT |

## 🤝 Contribuindo

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

## 👥 Autores

- **MyHub Team** - *Desenvolvimento inicial*

## 📞 Suporte

Para questões e suporte, abra uma issue no repositório ou entre em contato com a equipe de desenvolvimento.

---

**Nota**: Este é um projeto interno do MyHub. Certifique-se de seguir as políticas de segurança e privacidade da organização ao utilizar estes componentes.