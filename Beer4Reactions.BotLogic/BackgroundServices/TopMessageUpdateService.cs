using Microsoft.Extensions.Options;
using Beer4Reactions.BotLogic.Configuration;
using Beer4Reactions.BotLogic.Services;

namespace Beer4Reactions.BotLogic.BackgroundServices;

public class TopMessageUpdateService(
    IServiceProvider serviceProvider,
    IOptions<TelegramBotSettings> botSettings,
    IOptions<BotSettings> settings,
    ILogger<TopMessageUpdateService> logger)
    : BackgroundService
{
    private readonly TelegramBotSettings _botSettings = botSettings.Value;
    private readonly BotSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TopMessage Update Service started. Update interval: {Interval} minutes", 
            _settings.TopMessageUpdateIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateAllTopMessagesAsync();
                
                var delay = TimeSpan.FromMinutes(_settings.TopMessageUpdateIntervalMinutes);
                logger.LogInformation("Next TopMessage update in {Minutes} minutes", delay.TotalMinutes);
                
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("TopMessage update service cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TopMessage update cycle");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task UpdateAllTopMessagesAsync()
    {
        logger.LogDebug("TOP MESSAGE UPDATE | CYCLE STARTED | Chats[{ChatCount}]", 
            _botSettings.AllowedChatIds.Count);
        
        var updateTasks = _botSettings.AllowedChatIds.Select(async chatId =>
        {
            using var scope = serviceProvider.CreateScope();
            var topMessageService = scope.ServiceProvider.GetRequiredService<TopMessageService>();
            
            try
            {
                await topMessageService.UpdateTopMessageAsync(chatId);
                logger.LogDebug("CHAT[{ChatId}] | TOP MESSAGE UPDATED", chatId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CHAT[{ChatId}] | TOP MESSAGE UPDATE FAILED", chatId);
            }
        });

        await Task.WhenAll(updateTasks);

        // Периодически очищаем старые записи (раз в день)
        var utcNow = DateTime.UtcNow;
        if (utcNow.Hour == 0 && utcNow.Minute < _settings.TopMessageUpdateIntervalMinutes)
        {
            using var scope = serviceProvider.CreateScope();
        }
        
        logger.LogDebug("TOP MESSAGE UPDATE | CYCLE COMPLETED");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping TopMessage Update Service...");
        await base.StopAsync(cancellationToken);
    }
}
