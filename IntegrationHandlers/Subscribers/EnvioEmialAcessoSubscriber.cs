using DomainObjects.Enums;
using IntegrationHandlers.Events.EmailsWorker;
using IntegrationHandlers.Events.ManagementAPI;
using MessageBus.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationHandlers.Subscribers;

public class EnvioEmialAcessoSubscriber(IBusMessage message, ILogger<EnvioEmialAcessoSubscriber> log) : BackgroundService
{
    private readonly IBusMessage busMessage = message;
    private readonly ILogger<EnvioEmialAcessoSubscriber> logger = log;
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ✅ Registrar subscriber uma única vez
        busMessage.SubscribeAsync<InterfaceAcessoCriadaEvent>(SubscriptioId.TokenCadastroService,
            async (evento) =>
            {
                logger.LogInformation("Processando evento para {Email}", evento.Email);
                await EnviarEmailBoasVindasAsync(evento);
            });

        // ✅ Manter o serviço vivo sem loop infinito
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
    private async Task EnviarEmailBoasVindasAsync(InterfaceAcessoCriadaEvent evento)
    {
        try
        {

            logger.LogInformation("Enviando email para {Email}", evento.Email);
            var emailEvent = new EmailBoasVindasEvent("Bem-vindo!", true)
            {
                Email = evento.Email,
                Nome = evento.Nome,
                TokenAtivacao = evento.IdInterface.ToString(),
                UrlAtivacao = ""
            };

            await busMessage.PublishAsync(message: emailEvent, topic: emailEvent.Topic);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar evento para {Email}", evento.Email);
        }
        finally
        {
            await Task.CompletedTask;
        }
    }
}
