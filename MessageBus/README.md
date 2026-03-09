# MessageBus - Sistema de Mensageria

## 📋 Visão Geral

O **MessageBus** é um componente central da arquitetura FacimedEnterprise, responsável por gerenciar toda a comunicação assíncrona entre os microserviços através de mensageria RabbitMQ. Este componente fornece uma camada de abstração sobre o EasyNetQ, facilitando a implementação de padrões de mensageria como Publish/Subscribe e Request/Response.

### Arquitetura de Comunicação

```
┌─────────────┐
│  Frontend   │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│  API Gateway    │
└────────┬────────┘
         │
         ▼
    ┌────────────────────────┐
    │   RabbitMQ (MessageBus)│
    └────────────────────────┘
         │
    ┌────┴────┬────────┬────────┐
    ▼         ▼        ▼        ▼
┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│API Int1│ │API Int2│ │API Int3│ │API IntN│
└────────┘ └────────┘ └────────┘ └────────┘
```

**Princípios:**
- ✅ Frontend **apenas** acessa APIs Gateway
- ✅ APIs internas se comunicam **exclusivamente** via MessageBus
- ✅ APIs Gateway intermediam requisições do frontend para as APIs internas

---

## 🏗️ Componentes Principais

### 1. **IBusMessage / BusMessage**
Interface e implementação principal que fornece todas as operações de mensageria:

- **Publish/Subscribe**: Envio de mensagens sem aguardar resposta (fire-and-forget)
- **Request/Respond**: Comunicação RPC com aguardo de resposta

### 2. **Tipos de Mensagens**

#### **IntegrationEvent**
Mensagens para comunicação entre serviços (eventos de integração).

```csharp
public abstract class IntegrationEvent : Event
{
    public DateTime Timestamp { get; private set; }
}
```

#### **ResponseMessage**
Mensagens de resposta para requisições Request/Respond.

```csharp
public class ResponseMessage : Message
{
    public ValidationResult ValidationResult { get; set; }
}
```

#### **Command**
Comandos para operações internas com MediatR (uso local, não para mensageria).

```csharp
public abstract class Command : Message, IRequest<ValidationResult>
{
    public DateTime Timestamp { get; private set; }
    public virtual bool Valido() => throw new NotImplementedException();
}
```

### 3. **Configuração**

#### **MessageBusDependencyInjection**
Extensões para configurar a injeção de dependências:

- `AddMessageBusResponder<TResponder>()`: Para APIs que **respondem** a requisições
- `AddMessageBusRequest()`: Para APIs que apenas **enviam** requisições

---

## 🚀 Instalação e Configuração

### 1. Adicionar Referência ao Projeto

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Componentes\MessageBus\MessageBus.csproj" />
</ItemGroup>
```

### 2. Configurar Connection String

**appsettings.json**
```json
{
  "MessageQueueConnection": {
    "MessageBus": "host=localhost;virtualHost=/;username=guest;password=guest;timeout=30"
  }
}
```

**appsettings.Development.json**
```json
{
  "MessageQueueConnection": {
    "MessageBus": "host=rabbitmq-dev;virtualHost=/;username=admin;password=admin123;timeout=30"
  }
}
```

### 3. Registrar no Program.cs

#### Para APIs que **enviam** requisições (ex: API Gateway):

```csharp
using MessageBus.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Adicionar MessageBus para envio de requisições
builder.Services.AddMessageBusRequest(builder.Configuration);

// ... demais configurações

var app = builder.Build();
app.Run();
```

#### Para APIs que **respondem** a requisições (ex: APIs internas):

```csharp
using MessageBus.Configuration;
using SeuProjeto.Responders;

var builder = WebApplication.CreateBuilder(args);

// Adicionar MessageBus com Responder
builder.Services.AddMessageBusResponder<UsuarioResponder>(builder.Configuration);

// ... demais configurações

var app = builder.Build();
app.Run();
```

---

## 📚 Exemplos de Uso

### 1. Publish/Subscribe (Fire-and-Forget)

**Cenário**: Enviar notificações ou eventos sem aguardar resposta.

#### Definir o Evento de Integração

```csharp
using MessageBus.Messages;

namespace SeuProjeto.IntegrationEvents;

public class UsuarioCriadoEvent : IntegrationEvent
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }

    public UsuarioCriadoEvent()
    {
        AggregatedId = UsuarioId;
    }
}
```

#### Publicar o Evento (Producer)

```csharp
using MessageBus.Interfaces;

public class UsuarioService
{
    private readonly IBusMessage _busMessage;

    public UsuarioService(IBusMessage busMessage)
    {
        _busMessage = busMessage;
    }

    public async Task CriarUsuarioAsync(CreateUsuarioDto dto)
    {
        // Lógica de criação do usuário
        var usuario = new Usuario { ... };
        await _repository.AddAsync(usuario);

        // Publicar evento
        var evento = new UsuarioCriadoEvent
        {
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            DataCriacao = DateTime.Now
        };

        await _busMessage.PublishAsync(evento);
    }
}
```

#### Assinar o Evento (Subscriber)

```csharp
using MessageBus.Interfaces;
using Microsoft.Extensions.Hosting;

public class EmailNotificationSubscriber : BackgroundService
{
    private readonly IBusMessage _busMessage;
    private readonly ILogger<EmailNotificationSubscriber> _logger;

    public EmailNotificationSubscriber(
        IBusMessage busMessage,
        ILogger<EmailNotificationSubscriber> logger)
    {
        _busMessage = busMessage;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _busMessage.SubscribeAsync<UsuarioCriadoEvent>(
            "EmailService.UsuarioCriado",
            async (evento) =>
            {
                _logger.LogInformation(
                    "Enviando email de boas-vindas para {Email}", 
                    evento.Email);

                // Enviar email
                await EnviarEmailBoasVindasAsync(evento.Email, evento.Nome);
            });

        return Task.CompletedTask;
    }

    private async Task EnviarEmailBoasVindasAsync(string email, string nome)
    {
        // Implementação do envio de email
    }
}
```

**Registrar o Subscriber:**

```csharp
builder.Services.AddHostedService<EmailNotificationSubscriber>();
```

---

### 2. Request/Respond (RPC com Resposta)

**Cenário**: API Gateway solicita dados de uma API interna e aguarda resposta.

#### Definir a Requisição (Request)

```csharp
using MessageBus.Messages;

namespace IntegrationHandlers.Requests;

public class ObterUsuarioRequest : IntegrationEvent
{
    public Guid UsuarioId { get; set; }

    public ObterUsuarioRequest()
    {
        AggregatedId = UsuarioId;
    }
}
```

#### Definir a Resposta (Response)

```csharp
using MessageBus.Messages;

namespace IntegrationHandlers.Responses;

public class ObterUsuarioResponse : ResponseMessage
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
```

#### Implementar o Responder (API Interna)

```csharp
using MessageBus.Interfaces;
using IntegrationHandlers.Requests;
using IntegrationHandlers.Responses;
using Microsoft.Extensions.Hosting;

namespace UsuarioAPI.Responders;

public class UsuarioResponder : BackgroundService
{
    private readonly IBusMessage _busMessage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UsuarioResponder> _logger;

    public UsuarioResponder(
        IBusMessage busMessage,
        IServiceProvider serviceProvider,
        ILogger<UsuarioResponder> logger)
    {
        _busMessage = busMessage;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _busMessage.RespondAsync<ObterUsuarioRequest, ObterUsuarioResponse>(
            async (request) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var usuarioRepository = scope.ServiceProvider
                    .GetRequiredService<IUsuarioRepository>();

                _logger.LogInformation(
                    "Recebida requisição para obter usuário {UsuarioId}", 
                    request.UsuarioId);

                var usuario = await usuarioRepository
                    .GetByIdAsync(request.UsuarioId);

                if (usuario == null)
                {
                    return new ObterUsuarioResponse
                    {
                        ValidationResult = new ValidationResult(
                            new[] { new ValidationFailure("", "Usuário não encontrado") }
                        )
                    };
                }

                return new ObterUsuarioResponse
                {
                    UsuarioId = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Cpf = usuario.Cpf,
                    Ativo = usuario.Ativo
                };
            });

        return Task.CompletedTask;
    }
}
```

**Registrar o Responder:**

```csharp
// Program.cs
builder.Services.AddMessageBusResponder<UsuarioResponder>(builder.Configuration);
```

#### Enviar Request e Receber Response (API Gateway)

```csharp
using MessageBus.Interfaces;
using IntegrationHandlers.Requests;
using IntegrationHandlers.Responses;

namespace GatewayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IBusMessage _busMessage;

    public UsuariosController(IBusMessage busMessage)
    {
        _busMessage = busMessage;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterUsuario(Guid id)
    {
        var request = new ObterUsuarioRequest { UsuarioId = id };

        var response = await _busMessage
            .RequestAsync<ObterUsuarioRequest, ObterUsuarioResponse>(request);

        if (!response.ValidationResult.IsValid)
        {
            return NotFound(response.ValidationResult.Errors);
        }

        return Ok(new
        {
            response.UsuarioId,
            response.Nome,
            response.Email,
            response.Cpf,
            response.Ativo
        });
    }
}
```

---

## 🎯 Casos de Uso Práticos

### Caso 1: API Gateway → API de Usuários (Request/Respond)

**Fluxo:**
1. Frontend faz POST para criar usuário no Gateway
2. Gateway envia `CriarUsuarioRequest` via MessageBus
3. API de Usuários processa e retorna `CriarUsuarioResponse`
4. Gateway retorna resposta para o Frontend

```csharp
// Gateway Controller
[HttpPost]
public async Task<IActionResult> CriarUsuario(CreateUsuarioDto dto)
{
    var request = new CriarUsuarioRequest
    {
        Nome = dto.Nome,
        Email = dto.Email,
        Cpf = dto.Cpf
    };

    var response = await _busMessage
        .RequestAsync<CriarUsuarioRequest, CriarUsuarioResponse>(request);

    if (!response.ValidationResult.IsValid)
        return BadRequest(response.ValidationResult.Errors);

    return CreatedAtAction(
        nameof(ObterUsuario), 
        new { id = response.UsuarioId }, 
        response);
}
```

### Caso 2: API Interna Publica Evento (Publish/Subscribe)

**Fluxo:**
1. API de Pagamentos processa um pagamento com sucesso
2. Publica `PagamentoAprovadoEvent`
3. Múltiplas APIs reagem:
   - API de Pedidos atualiza status do pedido
   - API de Notificações envia email/SMS
   - API de Estoque reserva produtos

```csharp
// Publicar
await _busMessage.PublishAsync(new PagamentoAprovadoEvent
{
    PagamentoId = pagamento.Id,
    PedidoId = pagamento.PedidoId,
    Valor = pagamento.Valor,
    DataAprovacao = DateTime.Now
});

// Subscriber 1 - API de Pedidos
_busMessage.SubscribeAsync<PagamentoAprovadoEvent>(
    "PedidoService.PagamentoAprovado",
    async (evento) => await AtualizarStatusPedidoAsync(evento.PedidoId));

// Subscriber 2 - API de Notificações
_busMessage.SubscribeAsync<PagamentoAprovadoEvent>(
    "NotificationService.PagamentoAprovado",
    async (evento) => await EnviarNotificacaoAsync(evento));

// Subscriber 3 - API de Estoque
_busMessage.SubscribeAsync<PagamentoAprovadoEvent>(
    "EstoqueService.PagamentoAprovado",
    async (evento) => await ReservarEstoqueAsync(evento.PedidoId));
```

---

## 🔧 Recursos Avançados

### 1. Resiliência com Polly

O MessageBus já possui políticas de retry configuradas:

```csharp
// Retry 3 vezes com backoff exponencial
private static readonly Policy _retryPolicy = Policy
    .Handle<EasyNetQException>()
    .Or<BrokerUnreachableException>()
    .WaitAndRetry(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Retry infinito em caso de desconexão
private static readonly Policy _foreverPolicy = Policy
    .Handle<EasyNetQException>()
    .Or<BrokerUnreachableException>()
    .RetryForever();
```

### 2. Verificar Status da Conexão

```csharp
if (_busMessage.IsConnected)
{
    // MessageBus está conectado
}
```

### 3. Acesso ao AdvancedBus (para operações avançadas)

```csharp
var advancedBus = _busMessage.AdvancedBus;
// Operações avançadas do EasyNetQ
```

---

## 📖 Boas Práticas

### ✅ Nomenclatura

**Eventos:**
- Usar passado: `UsuarioCriadoEvent`, `PagamentoAprovadoEvent`
- Representa algo que **já aconteceu**

**Comandos (uso local com MediatR):**
- Usar imperativo: `CriarUsuarioCommand`, `AtualizarPedidoCommand`
- Representa uma **intenção de ação**

**Requests:**
- Usar verbo no infinitivo: `CriarUsuarioRequest`, `ObterPedidoRequest`

**Responses:**
- Usar mesmo nome do Request com sufixo Response: `CriarUsuarioResponse`

### ✅ SubscriptionId

Use um padrão consistente para identificar subscribers:

```
{NomeDoServico}.{NomeDoEvento}
```

Exemplos:
- `"EmailService.UsuarioCriado"`
- `"NotificationService.PagamentoAprovado"`
- `"EstoqueService.PedidoCriado"`

### ✅ Validação

Sempre valide as respostas:

```csharp
var response = await _busMessage.RequestAsync<TRequest, TResponse>(request);

if (!response.ValidationResult.IsValid)
{
    // Tratar erros
    return BadRequest(response.ValidationResult.Errors);
}

// Processar resposta
```

### ✅ Tratamento de Erros

```csharp
try
{
    var response = await _busMessage.RequestAsync<TRequest, TResponse>(request);
    // ...
}
catch (Exception ex)
{
    _logger.LogError(ex, "Erro ao enviar requisição");
    return StatusCode(500, "Erro ao processar requisição");
}
```

### ✅ Timeout

Configure timeout apropriado para requisições Request/Respond:

```csharp
// Na connection string
"host=localhost;virtualHost=/;username=guest;password=guest;timeout=30"
```

### ✅ Escopo de Serviços em Responders

Sempre crie um novo escopo ao usar serviços com ciclo de vida Scoped:

```csharp
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    _busMessage.RespondAsync<TRequest, TResponse>(async (request) =>
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IService>();
        
        // Usar service
    });
    
    return Task.CompletedTask;
}
```

---

## 🔍 Troubleshooting

### Problema: MessageBus não conecta

**Solução:**
1. Verificar se o RabbitMQ está rodando
2. Verificar connection string no appsettings.json
3. Verificar logs de erro

```bash
# Docker
docker ps | grep rabbitmq

# Acessar management
http://localhost:15672
```

### Problema: Mensagens não são recebidas

**Solução:**
1. Verificar se o Responder/Subscriber está registrado
2. Verificar se o BackgroundService está sendo executado
3. Verificar logs
4. Verificar filas no RabbitMQ Management

### Problema: Timeout em Request/Respond

**Solução:**
1. Aumentar timeout na connection string
2. Verificar se há Responder registrado
3. Verificar logs do Responder

---

## 📦 Dependências

- **EasyNetQ** 7.8.0 - Cliente RabbitMQ
- **EasyNetQ.Serialization.SystemTextJson** 7.8.0 - Serialização JSON
- **Polly** 8.6.4 - Resiliência e retry
- **FluentValidation** 12.0.0 - Validação
- **MediatR** - Pattern Mediator (para uso local)

---

## 📝 Estrutura do Projeto

```
MessageBus/
├── Configuration/
│   ├── MessageBusDependencyInjection.cs    # Configuração DI
│   └── MessageBusExtensions.cs             # Extension methods
├── Interfaces/
│   └── BusMessage.cs                       # Interface e implementação
├── Mediator/
│   └── MediatorHandler.cs                  # Mediator (uso local)
├── Messages/
│   ├── Message.cs                          # Classe base
│   ├── Command.cs                          # Commands (uso local)
│   ├── Event.cs                            # Events e IntegrationEvents
│   └── ResponseMessage.cs                  # Resposta padrão
└── MessageBus.csproj
```

---

## 🐛 Troubleshooting - Problemas Comuns

### Problema: TaskCanceledException em BackgroundService

**Sintomas:**
- Erro `TaskCanceledException` ao iniciar a API
- BackgroundService com loop infinito incorreto
- Múltiplas inscrições para o mesmo evento

**Causa:**
```csharp
// ❌ INCORRETO - Loop infinito desnecessário
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        busMessage.Subscribe<Evento>("SubscriptionId", async (evento) => { ... });
    }
    return Task.CompletedTask;
}
```

**Solução:**
```csharp
// ✅ CORRETO - Registrar subscriber uma única vez
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Registrar subscriber uma única vez
    busMessage.SubscribeAsync<InterfaceAcessoCriadaEvent>("TokenCadastroService",
        async (evento) =>
        {
            logger.LogInformation("Processando evento: {Email}", evento.Email);
            await ProcessarEventoAsync(evento);
        });

    // Manter o serviço vivo
    return Task.Delay(Timeout.Infinite, stoppingToken);
}

private async Task ProcessarEventoAsync(InterfaceAcessoCriadaEvent evento)
{
    try
    {
        logger.LogInformation("Enviando email para {Email}", evento.Email);
        await EnviarEmailBoasVindasAsync(evento.Email, evento.Nome);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao processar evento para {Email}", evento.Email);
    }
}
```

### Problema: Subscribe Síncrono com Action Assíncrona

**Sintomas:**
- Deadlocks ou exceções de cancelamento
- Métodos async em Subscribe síncrono

**Solução:**
```csharp
// ❌ INCORRETO
busMessage.Subscribe<Evento>("Id", async (evento) => { ... });

// ✅ CORRETO - Use SubscribeAsync para actions async
busMessage.SubscribeAsync<Evento>("Id", async (evento) => { ... });
```

### Problema: Connection String Não Encontrada

**Sintomas:**
- `ArgumentNullException` ao iniciar
- "The connection string cannot be null or empty"

**Solução:**
```json
// appsettings.json
{
  "MessageQueueConnection": {
    "MessageBus": "host=localhost;virtualHost=/;username=guest;password=guest;timeout=30"
  }
}
```

### Problema: API Inicia mas Não Emite Integração

**Sintomas:**
- API inicia sem erros
- BackgroundService não processa eventos
- Conexão com RabbitMQ aparentemente OK

**Possíveis Causas e Soluções:**

#### 1. **Problema de Hostname na Connection String**

**❌ Problema:**
```json
// Connection string usando hostname do container
"MessageBus": "host=facimed-message:5672;username=facimed;password=facimed;virtualHost=/;publisherConfirms=true;requestedHeartbeat=60;timeout=10"
```

**✅ Solução:**
```json
// Para desenvolvimento local (API rodando fora do Docker)
"MessageBus": "host=localhost;port=5672;username=facimed;password=facimed;virtualHost=/;publisherConfirms=true;requestedHeartbeat=60;timeout=10"

// Para API rodando dentro do Docker
"MessageBus": "host=facimed-message;port=5672;username=facimed;password=facimed;virtualHost=/;publisherConfirms=true;requestedHeartbeat=60;timeout=10"
```

#### 2. **Verificar se RabbitMQ está Acessível**

```bash
# Testar conectividade
docker exec -it facimed-rabbitmq rabbitmq-diagnostics ping

# Verificar status
docker exec -it facimed-rabbitmq rabbitmq-diagnostics status

# Verificar conexões ativas
docker exec -it facimed-rabbitmq rabbitmq-diagnostics listeners
```

#### 3. **Verificar Logs do RabbitMQ**

```bash
# Ver logs do RabbitMQ
docker logs facimed-rabbitmq

# Ver logs em tempo real
docker logs -f facimed-rabbitmq
```

#### 4. **Verificar Configuração do BackgroundService**

**❌ Problema Comum:**
```csharp
// BackgroundService não está aguardando o RabbitMQ estar pronto
protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Executa imediatamente, antes do RabbitMQ estar pronto
    busMessage.SubscribeAsync<Evento>("Id", handler);
    return Task.CompletedTask;
}
```

**✅ Solução:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Aguardar RabbitMQ estar pronto
    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    
    // Verificar se está conectado
    if (!busMessage.IsConnected)
    {
        logger.LogWarning("MessageBus não conectado, aguardando...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    }
    
    logger.LogInformation("Registrando subscriber...");
    busMessage.SubscribeAsync<InterfaceAcessoCriadaEvent>("TokenCadastroService",
        async (evento) =>
        {
            logger.LogInformation("Evento recebido: {Email}", evento.Email);
            await ProcessarEventoAsync(evento);
        });

    // Manter serviço vivo
    await Task.Delay(Timeout.Infinite, stoppingToken);
}
```

#### 5. **Verificar Dependências no Docker Compose**

**❌ Problema:**
```yaml
# API não aguarda RabbitMQ estar pronto
services:
  api:
    depends_on:
      - facimed-message  # Apenas inicia após, não aguarda estar saudável
```

**✅ Solução:**
```yaml
services:
  api:
    depends_on:
      facimed-message:
        condition: service_healthy  # Aguarda health check
```

#### 6. **Adicionar Health Check ao RabbitMQ**

```yaml
facimed-message:
  image: rabbitmq:3.13-management
  container_name: facimed-rabbitmq
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s  # Aguarda 30s antes de começar health checks
```

#### 7. **Verificar se o Evento está sendo Publicado**

```csharp
// Adicionar logs para debug
public async Task PublicarEventoAsync(InterfaceAcessoCriadaEvent evento)
{
    try
    {
        logger.LogInformation("Publicando evento: {Email}", evento.Email);
        await _busMessage.PublishAsync(evento);
        logger.LogInformation("Evento publicado com sucesso: {Email}", evento.Email);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao publicar evento: {Email}", evento.Email);
        throw;
    }
}
```

#### 8. **Verificar Configuração de Timeout**

```json
// Aumentar timeout se necessário
"MessageBus": "host=localhost;port=5672;username=facimed;password=facimed;virtualHost=/;publisherConfirms=true;requestedHeartbeat=60;timeout=30"
```

### Problema: TaskCanceledException em EasyNetQ

**Sintomas:**
- `TaskCanceledException` com source "EasyNetQ"
- Operações de canal canceladas

**Solução:**
```csharp
// Adicionar retry policy mais robusta
private static readonly Policy _retryPolicy = Policy
    .Handle<EasyNetQException>()
    .Or<BrokerUnreachableException>()
    .Or<TaskCanceledException>()  // Adicionar TaskCanceledException
    .WaitAndRetry(5, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Verificar conexão antes de usar
public void Publish<T>(T message) where T : IntegrationEvent
{
    if (!IsConnected)
    {
        logger.LogWarning("MessageBus não conectado, tentando reconectar...");
        TryConnect();
    }
    
    _retryPolicy.Execute(() =>
    {
        _bus.PubSub.Publish(message);
    });
}
```

### Problema: RabbitMQ Management UI Não Acessível

**Sintomas:**
- Não consegue acessar `http://localhost:15672`
- Erro de conexão recusada
- Página não carrega

**Diagnóstico e Soluções:**

#### 1. **Verificar se o Container está Rodando**

```bash
# Verificar se RabbitMQ está rodando
docker ps | grep rabbitmq

# Deve mostrar algo como:
# facimed-rabbitmq   rabbitmq:3.13-management   ...   0.0.0.0:15672->15672/tcp
```

#### 2. **Verificar Portas Expostas**

```bash
# Verificar se a porta 15672 está sendo exposta
docker port facimed-rabbitmq

# Deve mostrar:
# 15672/tcp -> 0.0.0.0:15672
# 5672/tcp -> 0.0.0.0:5672
```

#### 3. **Verificar Logs do RabbitMQ**

```bash
# Ver logs do RabbitMQ
docker logs facimed-rabbitmq

# Procurar por:
# - "Management plugin started"
# - "Server startup complete"
# - Erros de configuração
```

#### 4. **Verificar Variáveis de Ambiente**

```bash
# Verificar variáveis do container
docker exec -it facimed-rabbitmq env | grep RABBITMQ

# Deve mostrar:
# RABBITMQ_DEFAULT_USER=facimed
# RABBITMQ_DEFAULT_PASS=facimed
# RABBITMQ_DEFAULT_VHOST=/
```

#### 5. **Verificar se Management Plugin está Habilitado**

```bash
# Verificar plugins habilitados
docker exec -it facimed-rabbitmq rabbitmq-plugins list

# Deve mostrar:
# [E*] rabbitmq_management
```

#### 6. **Soluções Comuns**

**Problema: Porta não está sendo exposta**

```yaml
# Verificar se a porta está correta no docker-compose.yml
facimed-message:
  ports:
    - "${RABBITMQ_UI_PORT:-15672:15672}"  # Deve ser 15672:15672
```

**Problema: Variáveis de ambiente não definidas**

```bash
# Verificar arquivo .env
cat .env | grep RABBITMQ

# Deve ter:
# RABBITMQ_USER=facimed
# RABBITMQ_PASS=facimed
# RABBITMQ_UI_PORT=15672
```

**Problema: Container não está saudável**

```bash
# Verificar health check
docker inspect facimed-rabbitmq | grep -A 10 Health

# Reiniciar se necessário
docker restart facimed-rabbitmq
```

#### 7. **Teste de Conectividade**

```bash
# Testar se a porta está acessível
telnet localhost 15672

# Ou usar curl
curl -I http://localhost:15672

# Deve retornar HTTP 200
```

#### 8. **Solução Completa - Reiniciar com Debug**

```bash
# 1. Parar todos os containers
docker-compose down

# 2. Remover volumes (cuidado: apaga dados)
docker volume prune

# 3. Subir novamente com logs
docker-compose up -d facimed-message

# 4. Verificar logs em tempo real
docker logs -f facimed-rabbitmq

# 5. Aguardar aparecer: "Management plugin started"
```

#### 9. **Verificar Configuração do .env**

```bash
# Arquivo .env deve ter:
RABBITMQ_USER=facimed
RABBITMQ_PASS=facimed
RABBITMQ_VHOST=/
RABBITMQ_UI_PORT=15672
RABBITMQ_AMQP_PORT=5672
```

#### 10. **Acesso Manual ao Management**

```bash
# Se ainda não funcionar, acesse diretamente o container
docker exec -it facimed-rabbitmq bash

# Dentro do container, verificar se management está rodando
rabbitmq-diagnostics listeners

# Deve mostrar:
# Interface: [::], port: 15672, protocol: http, purpose: management
```

#### 11. **URLs de Acesso**

```
# URL principal
http://localhost:15672

# URLs alternativas (se houver problemas de DNS)
http://127.0.0.1:15672
http://0.0.0.0:15672
```

#### 12. **Credenciais de Acesso**

```
Usuário: facimed
Senha: facimed
```

#### 13. **Verificar Firewall/Antivírus**

```bash
# Windows - Verificar se porta está bloqueada
netstat -an | findstr 15672

# Deve mostrar:
# TCP    0.0.0.0:15672          0.0.0.0:0              LISTENING
```

#### 14. **Solução de Emergência - Acesso Direto**

```bash
# Se nada funcionar, acesse via container
docker exec -it facimed-rabbitmq bash

# Dentro do container, instalar ferramentas
apt update && apt install -y curl

# Testar localmente
curl http://localhost:15672

# Se funcionar localmente, problema é de rede/porta
```

---

## 🤝 Integração com IntegrationHandlers

O projeto **IntegrationHandlers** deve conter todas as definições de Request/Response compartilhadas entre as APIs:

```
IntegrationHandlers/
├── Requests/
│   ├── CriarUsuarioRequest.cs
│   ├── ObterUsuarioRequest.cs
│   └── AtualizarUsuarioRequest.cs
├── Responses/
│   ├── CriarUsuarioResponse.cs
│   ├── ObterUsuarioResponse.cs
│   └── AtualizarUsuarioResponse.cs
└── Events/
    ├── UsuarioCriadoEvent.cs
    └── PagamentoAprovadoEvent.cs
```

**Todas as APIs** devem referenciar IntegrationHandlers para manter o contrato padronizado.

---

## 📚 Exemplos Completos

Para exemplos completos de implementação, consulte:

- **IntegrationHandlers/Responders/** - Implementações de responders
- **ManagementAPI/** - Exemplo de API Gateway
- **Services/\*/API/** - Exemplos de APIs internas

---

## 📄 Licença

Este componente faz parte do sistema FacimedEnterprise e é de uso interno.

---

## 👥 Suporte

Para dúvidas ou problemas, entre em contato com a equipe de arquitetura.

